# Plano de Correção: Erros de Transaction ID e Deserialização

**Data:** 2025-12-15  
**Autor:** Análise automatizada de logs  
**Status:** Pendente de implementação

---

## 📋 Resumo Executivo

Os logs revelam dois problemas críticos nos serviços de transação (Card, Money, Pix):

1. **Erro de Deserialização do Heartbeat**: Mensagens do `heartbeat-publisher` não contêm o header `rbs2-content-type` necessário para o Rebus
2. **Erro de Transaction ID Vazio**: Mensagens periódicas (~1 por minuto) chegam aos serviços com `TransactionId` vazio (`Guid.Empty`)

---

## 🔍 Análise Detalhada

### Problema 1: Erro de Deserialização (KeyNotFoundException)

**Erro:**
```
KeyNotFoundException: Could not find the key 'rbs2-content-type'
```

**Onde ocorre:**
- `services-heartbeats` (01:24:11)
- `services-card` (01:24:11)
- `services-money` (01:24:11)
- `services-pix` (01:24:11)

**Causa raiz:**
O `HeartbeatPublisher` (Services.Heartbeats) está publicando mensagens através do `RebusMessagePublisher`, que cria um `MessageEnvelope` e o publica no bus. No entanto, o Rebus espera que mensagens contenham o header `rbs2-content-type` para deserialização, mas esse header não está sendo adicionado.

**Análise de código:**
- `HeartbeatPublisher.cs:59` → `_publisher.PublishAsync(...)`
- `RebusMessagePublisher.cs:23` → Cria `MessageEnvelope` e chama `_messagingBus.PublishAsync(envelope, ...)`
- O `MessageEnvelope` é um record simples: `record MessageEnvelope(string MessageType, string Payload, string RoutingKey)`
- **FALTA**: Headers customizados do Rebus não estão sendo propagados

**Impacto:**
- 4 tentativas de retry por mensagem
- Mensagens de heartbeat não são processadas pelos serviços de transação
- Logs poluídos com stack traces

---

### Problema 2: Transaction ID Vazio (transaction_id_empty)

**Erro:**
```
InvalidOperationException: transaction_id_empty
```

**Onde ocorre:**
- Todos os três serviços (Card, Money, Pix)
- Aproximadamente 1 vez por minuto
- Mesmo `message_id` aparece em todos os serviços simultaneamente

**Padrão temporal:**
```
01:24:27 - message_id: 81456218-f8b6-437f-a28e-433fd1eb816b
01:25:25 - message_id: 26ca8550-236a-45d4-abed-be825c05d2b1
01:26:20 - message_id: 6df55e39-c77c-4f89-ba61-15ebcb1eb0ac
01:27:15 - message_id: 47e8c7d2-a9e4-4a8d-9a1f-c0098a63860e
```

**Causa raiz:**
Com base na análise:

1. **HeartbeatPublisher está enviando HeartbeatDto, não TransactionDto**
   - `HeartbeatDto` → `record HeartbeatDto(string ServiceName, string Status, DateTime TimestampUtc)`
   - **NÃO possui campo `TransactionId`**

2. **Roteamento incorreto:**
   - O `HeartbeatPublisher` está publicando na `routingKey` configurada (`_rabbitOptions.RoutingKey`)
   - Se essa routing key for `transactions` (ou similar), as mensagens vão para a fila errada
   - Os serviços Card/Money/Pix **tentam deserializar `HeartbeatDto` como `CardTransactionDto/MoneyTransactionDto/PixTransactionDto`**

3. **Deserialização parcial:**
   - `JsonSerializer.Deserialize` não falha porque `HeartbeatDto` é JSON válido
   - Mas o DTO resultante tem `TransactionId = Guid.Empty` (valor padrão)
   - Falha na validação: `TransactionId.TryCreate(Guid.Empty)` → `"transaction_id_empty"`

**Evidência:**
- `CardTransactionReceiver.cs:58` → Deserializa para `CardTransactionDto`
- `CardTransactionDto` → `record CardTransactionDto(Guid TransactionId, ...)`
- Quando recebe JSON de `HeartbeatDto`, `TransactionId` fica vazio
- `TransactionDtoMapper.cs:64` → `TransactionId.TryCreate(transactionId)` falha
- `TransactionId.cs:9-11` → Retorna erro `"transaction_id_empty"`

**Impacto:**
- Processamento interrompido de mensagens válidas
- Retry infinito (1 tentativa)
- Mensagens de heartbeat não são descartadas silenciosamente

---

## 🎯 Soluções Propostas

### Solução 1: Corrigir Roteamento do Heartbeat

**Abordagem:** Separar completamente o heartbeat do fluxo de transações

**Mudanças necessárias:**

1. **`appsettings.json` do HeartbeatPublisher:**
   ```json
   "RabbitOptions": {
     "RoutingKey": "heartbeats"  // Era: "transactions"
   }
   ```

2. **`appsettings.json` do HeartbeatConsumer:**
   ```json
   "RabbitOptions": {
     "RoutingKey": "heartbeats"
   }
   ```

3. **Verificar configuração de exchanges:**
   - Garantir que existe um binding de `heartbeats` para a exchange correta
   - Evitar que heartbeats sejam roteados para `queue.transactions`

**Prós:**
- ✅ Correção simples e direta
- ✅ Sem mudanças de código
- ✅ Mantém arquitetura atual

**Contras:**
- ❌ Não resolve o problema do header `rbs2-content-type`

---

### Solução 2: Adicionar Validação de MessageType nos Receivers

**Abordagem:** Fazer os receivers validarem o `MessageType` antes de deserializar

**Mudanças necessárias:**

1. **`CardTransactionReceiver.cs` (e similares):**
   ```csharp
   public async Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
   {
       ArgumentNullException.ThrowIfNull(envelope);

       if (!CanHandle(envelope))
       {
           IgnoredRoutingKey(_logger, envelope.RoutingKey, _expectedRoutingKey, null);
           return;
       }

       // NOVO: Validar MessageType
       if (!envelope.MessageType.StartsWith("mock.", StringComparison.OrdinalIgnoreCase))
       {
           _logger.LogDebug("Ignoring non-transaction message type {MessageType}", envelope.MessageType);
           return;
       }

       var message = JsonSerializer.Deserialize(envelope.Payload, CardTransactionJsonContext.Default.CardTransactionDto);
       // ... resto do código
   }
   ```

**Prós:**
- ✅ Validação defensiva
- ✅ Descarta mensagens inválidas silenciosamente
- ✅ Fácil de implementar

**Contras:**
- ❌ Tratamento de sintoma, não da causa
- ❌ Código duplicado em 3 receivers

---

### Solução 3: Corrigir Headers do Rebus (RECOMENDADA)

**Abordagem:** Fazer o `RebusMessagePublisher` adicionar headers necessários

**Mudanças necessárias:**

1. **`RebusMessagePublisher.cs`:**
   ```csharp
   using Rebus.Messages;
   using System.Collections.Generic;

   public sealed class RebusMessagePublisher : IMessagePublisher
   {
       private readonly IMessagingBus _messagingBus;

       public RebusMessagePublisher(IMessagingBus messagingBus)
       {
           ArgumentNullException.ThrowIfNull(messagingBus);
           _messagingBus = messagingBus;
       }

       public async Task PublishAsync(string messageType, string payload, string routingKey, CancellationToken cancellationToken = default)
       {
           ArgumentException.ThrowIfNullOrWhiteSpace(messageType);
           ArgumentException.ThrowIfNullOrWhiteSpace(payload);
           ArgumentException.ThrowIfNullOrWhiteSpace(routingKey);

           var envelope = new MessageEnvelope(messageType, payload, routingKey);
           
           // Adicionar headers do Rebus
           var headers = new Dictionary<string, string>
           {
               { Headers.ContentType, "application/json" },
               { Headers.Type, typeof(MessageEnvelope).GetSimpleAssemblyQualifiedName() },
               { "routing-key", routingKey }
           };

           await _messagingBus.PublishAsync(envelope, headers, cancellationToken).ConfigureAwait(false);
       }
   }
   ```

2. **Atualizar interface `IMessagingBus`:**
   ```csharp
   public interface IMessagingBus
   {
       Task PublishAsync<T>(T message, CancellationToken cancellationToken = default);
       Task PublishAsync<T>(T message, Dictionary<string, string> headers, CancellationToken cancellationToken = default);
   }
   ```

3. **Implementação na classe concreta do bus:**
   ```csharp
   public async Task PublishAsync<T>(T message, Dictionary<string, string> headers, CancellationToken cancellationToken = default)
   {
       await _bus.Advanced.Topics.Publish(topic, message, headers).ConfigureAwait(false);
   }
   ```

**Prós:**
- ✅ Resolve o problema do header faltando
- ✅ Mantém compatibilidade com Rebus
- ✅ Solução centralizada

**Contras:**
- ❌ Requer mudanças em interface e implementação
- ❌ Mais complexo

---

### Solução 4: Combinar Soluções 1 + 2 (PRAGMÁTICA)

**Abordagem:** Correção rápida com validação defensiva

1. **Curto prazo:**
   - Implementar Solução 1 (corrigir routing key do heartbeat)
   - Implementar Solução 2 (validação de MessageType nos receivers)

2. **Médio prazo:**
   - Implementar Solução 3 (headers do Rebus) se problema persistir

**Prós:**
- ✅ Correção imediata sem refactoring complexo
- ✅ Validação adicional previne futuros bugs
- ✅ Baixo risco

**Contras:**
- ❌ Não é a solução "mais correta"
- ❌ Deixa debt técnica para depois

---

## 📝 Plano de Implementação (RECOMENDADO)

### Fase 1: Correção Imediata (Solução 1 + 2)

**Passo 1: Corrigir configuração do Heartbeat**
```bash
# Arquivos a modificar:
- src/Services.Heartbeats/appsettings.json
- Configuração de RabbitMQ (se necessário)
```

**Passo 2: Adicionar validação nos receivers**
```bash
# Arquivos a modificar:
- src/Services.Card/CardTransactionReceiver.cs
- src/Services.Money/MoneyTransactionReceiver.cs
- src/Services.Pix/PixTransactionReceiver.cs
```

**Passo 3: Testar**
```bash
# Reiniciar containers
podman-compose down
podman-compose up -d

# Monitorar logs
podman logs -f services-card
podman logs -f mock-transactions
```

**Tempo estimado:** 30 minutos

---

### Fase 2: Correção Estrutural (Solução 3)

**Passo 1: Atualizar IMessagingBus**
```bash
- src/Domain/Interfaces/IMessagingBus.cs
```

**Passo 2: Atualizar RebusMessagePublisher**
```bash
- src/Infra.Message/RebusMessagePublisher.cs
```

**Passo 3: Atualizar implementação concreta**
```bash
- Encontrar implementação de IMessagingBus
- Adicionar sobrecarga com headers
```

**Passo 4: Testar extensivamente**

**Tempo estimado:** 2 horas

---

## 🧪 Testes de Validação

### Cenário 1: Heartbeat não interfere em transações
```bash
# Verificar logs após correção
podman logs services-card 2>&1 | grep -i "heartbeat"
# Esperado: Nenhum log de erro relacionado a heartbeat

podman logs services-card 2>&1 | grep -i "transaction_id_empty"
# Esperado: Nenhuma ocorrência
```

### Cenário 2: Transações são processadas corretamente
```bash
# Verificar processamento
podman logs mock-transactions | tail -20
podman logs services-card | tail -20
# Esperado: Logs de transações publicadas e processadas
```

### Cenário 3: Headers do Rebus presentes
```bash
# Inspecionar mensagem no RabbitMQ
podman exec rabbitmq rabbitmqctl list_queues name messages
# Verificar headers das mensagens
```

---

## 📊 Métricas de Sucesso

- ✅ Zero ocorrências de `KeyNotFoundException` nos logs
- ✅ Zero ocorrências de `transaction_id_empty` nos logs
- ✅ Heartbeats recebidos e processados corretamente pelo HeartbeatConsumer
- ✅ Transações mock processadas sem erros
- ✅ Retry count = 0 para mensagens válidas

---

## 🚨 Riscos e Mitigações

| Risco | Probabilidade | Impacto | Mitigação |
|-------|---------------|---------|-----------|
| Configuração incorreta de routing key | Média | Alto | Validar configuração antes de deploy |
| Mensagens perdidas durante transição | Baixa | Médio | Fazer em ambiente de dev primeiro |
| Headers incompatíveis com Rebus | Baixa | Alto | Testar com versão específica do Rebus |
| Impacto em outros consumers | Baixa | Alto | Revisar todos os consumers registrados |

---

## 📚 Documentação Adicional

### Arquivos Relevantes
- `src/Services.Card/CardTransactionReceiver.cs:69` → Onde o erro é lançado
- `src/Services.Heartbeats/HeartbeatPublisher.cs:59` → Publicação do heartbeat
- `src/Infra.Message/RebusMessagePublisher.cs:23` → Publicação de mensagens
- `src/Domain/ValueObjects/TransactionId.cs:9-11` → Validação de TransactionId
- `src/Domain/Messaging/MessageEnvelope.cs` → Estrutura do envelope

### Referências Técnicas
- [Rebus Headers](https://github.com/rebus-org/Rebus/wiki/Headers)
- [RabbitMQ Routing](https://www.rabbitmq.com/tutorials/tutorial-four-dotnet.html)
- [System.Text.Json Serialization](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/overview)

---

## ✅ Checklist de Implementação

### Antes de começar
- [ ] Backup da configuração atual
- [ ] Documentar routing keys existentes
- [ ] Verificar versão do Rebus em uso

### Durante implementação
- [ ] Atualizar appsettings.json do HeartbeatPublisher
- [ ] Adicionar validação de MessageType nos receivers
- [ ] Testar localmente com Podman
- [ ] Verificar logs de todos os serviços
- [ ] Validar métricas de sucesso

### Após implementação
- [ ] Monitorar logs por 24h
- [ ] Confirmar zero erros de deserialização
- [ ] Documentar mudanças no README
- [ ] Criar issue para Fase 2 (se aplicável)

---

**Última atualização:** 2025-12-15T01:31:27Z
