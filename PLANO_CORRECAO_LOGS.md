# Plano de Correção - Análise dos Logs dos Contêineres

**Data da Análise:** 17 de dezembro de 2025  
**Ambiente:** Podman containers - MyNewLittleBank

---

## 📊 Resumo Executivo

Após análise dos logs dos contêineres de serviços, foi identificado **um erro crítico recorrente** no serviço PIX e **operações normais** nos demais serviços.

### Status dos Serviços

| Serviço | Status | Problema Identificado |
|---------|--------|----------------------|
| **services-pix** | ⚠️ ERRO CRÍTICO | Type mismatch no Entity Framework |
| **services-card** | ✅ OK | Funcionando normalmente |
| **services-money** | ✅ OK | Funcionando normalmente |
| **services-heartbeats** | ✅ OK | Funcionando normalmente |
| **api** | ✅ OK | Funcionando normalmente |

---

## 🔴 PROBLEMA CRÍTICO: Service PIX

### Descrição do Erro

```
System.ArgumentException: The key value at position 0 of the call to 'DbSet<Client>.Find' 
was of type 'Guid', which does not match the property type of 'ClientId'.
```

### Localização

- **Arquivo:** `src/UseCases/Transactions/ProcessTransactionsHandler.cs`
- **Linha:** 153-154
- **Método:** `NotifyInsufficientFundsAsync`

### Código Problemático

```csharp
var client = await _clientReader
    .GetByIdAsync(new object[] { transaction.ClientId.Value }, cancellationToken)
    .ConfigureAwait(false);
```

### Causa Raiz

O Entity Framework está esperando um objeto do tipo `ClientId` (value object/struct), mas está recebendo um `Guid` diretamente. Isso ocorre porque:

1. **ClientId** é um `record struct` que encapsula um `Guid`
2. O Entity Framework precisa do objeto completo `ClientId`, não apenas o valor interno
3. A linha 154 passa `transaction.ClientId.Value` (Guid) quando deveria passar `transaction.ClientId` (ClientId struct)

### Impacto

- ❌ Falha ao processar transações PIX com saldo insuficiente
- ❌ Notificações de fundos insuficientes não são enviadas
- ❌ Exceções recorrentes poluindo os logs
- ⚠️ Possível perda de eventos de notificação

---

## 🔧 Plano de Correção

### Prioridade: **ALTA** 🔴

### Solução 1: Correção Direta (Recomendada)

**Arquivo:** `src/UseCases/Transactions/ProcessTransactionsHandler.cs`  
**Linha:** 154

**Mudança:**
```csharp
// ANTES (incorreto)
var client = await _clientReader
    .GetByIdAsync(new object[] { transaction.ClientId.Value }, cancellationToken)
    .ConfigureAwait(false);

// DEPOIS (correto)
var client = await _clientReader
    .GetByIdAsync(new object[] { transaction.ClientId }, cancellationToken)
    .ConfigureAwait(false);
```

**Justificativa:**
- Passa o value object completo `ClientId` em vez de apenas seu valor interno `Guid`
- O Entity Framework conseguirá fazer a conversão apropriada para o tipo da chave primária
- Alinhado com a configuração do DbContext

### Solução 2: Sobrecarga do Método (Alternativa)

Criar um método específico no repositório para buscar por `ClientId`:

**Arquivo:** `src/Infra.Database/Repositories/EfRepository.cs`

```csharp
public async Task<TEntity?> GetByIdAsync<TKey>(TKey keyValue, CancellationToken cancellationToken = default) 
    where TKey : struct =>
    await _set.FindAsync(new object[] { keyValue }, cancellationToken)
        .AsTask()
        .ConfigureAwait(false);
```

**Uso:**
```csharp
var client = await _clientReader
    .GetByIdAsync(transaction.ClientId, cancellationToken)
    .ConfigureAwait(false);
```

---

## ✅ Serviços Funcionando Corretamente

### services-card
- ✅ Processando transações de cartão normalmente
- ✅ Queries ao banco de dados executando com sucesso
- ✅ Padrão Outbox funcionando corretamente

### services-money
- ✅ Processando transações monetárias normalmente
- ✅ Atualizações de saldo executando corretamente
- ✅ Integração com RabbitMQ funcionando

### services-heartbeats
- ✅ Recebendo heartbeats a cada 30 segundos
- ✅ Sistema de monitoramento operacional
- ✅ Sem erros ou avisos

### api
- ✅ Endpoints respondendo corretamente
- ✅ Queries GET funcionando (accounts, clients)
- ✅ Status codes apropriados (200, 404)

---

## 📋 Checklist de Implementação

### Passo 1: Backup e Preparação
- [ ] Criar branch para correção: `fix/pix-service-clientid-type-mismatch`
- [ ] Garantir que todos os testes estão passando antes da mudança

### Passo 2: Implementar Correção
- [ ] Modificar linha 154 em `ProcessTransactionsHandler.cs`
- [ ] Remover `.Value` do `transaction.ClientId`
- [ ] Revisar código para outras ocorrências similares

### Passo 3: Testes
- [ ] Executar testes unitários do `ProcessTransactionsHandler`
- [ ] Testar cenário de transação PIX com saldo insuficiente
- [ ] Verificar se a notificação é enviada corretamente
- [ ] Executar testes de integração

### Passo 4: Validação
- [ ] Rebuild das imagens Docker/Podman
- [ ] Deploy em ambiente de desenvolvimento
- [ ] Monitorar logs do services-pix por 15 minutos
- [ ] Validar ausência do erro `ArgumentException`

### Passo 5: Deploy
- [ ] Criar Pull Request com a correção
- [ ] Code Review
- [ ] Merge para branch principal
- [ ] Deploy em produção
- [ ] Monitoramento pós-deploy

---

## 🔍 Verificações Adicionais Recomendadas

### Auditoria de Value Objects
Procurar por outras ocorrências onde `.Value` está sendo usado incorretamente:

```bash
# Buscar padrões similares no código
grep -r "GetByIdAsync.*\.Value" src/
```

### Padrões a Revisar
- [ ] Uso de `TransactionId.Value`
- [ ] Uso de `AccountNumber.Value`
- [ ] Uso de `Cpf.Value`

### Testes de Regressão
- [ ] Adicionar teste específico para cenário de saldo insuficiente
- [ ] Garantir cobertura de testes para notificações

---

## 📈 Métricas de Sucesso

Após implementação da correção, validar:

1. **Zero ocorrências** do erro `ArgumentException` relacionado a `ClientId`
2. **100% de notificações** enviadas em casos de saldo insuficiente
3. **Logs limpos** sem stack traces de exceções
4. **Latência normal** no processamento de transações PIX

---

## 📚 Documentação de Referência

- **Entity Framework Core:** Uso de Value Objects como chaves
- **DDD Value Objects:** Quando usar `.Value` vs o objeto completo
- **Padrão Repository:** Best practices para métodos GetById

---

## 🔄 Próximos Passos

1. **Imediato:** Implementar correção no `ProcessTransactionsHandler.cs`
2. **Curto prazo:** Auditoria completa de uso de Value Objects
3. **Médio prazo:** Adicionar linting rules para detectar padrões incorretos
4. **Longo prazo:** Documentar guidelines de uso de Value Objects no projeto

---

**Preparado por:** Análise Automatizada de Logs  
**Revisão necessária:** Equipe de Desenvolvimento  
**Aprovação necessária:** Tech Lead
