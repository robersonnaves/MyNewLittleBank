# Regra de implementação de telemetria para aplicações .NET 8

Este documento define o **prompt padrão** que deve ser seguido para implementar telemetria em todas as aplicações do ecossistema, usando **.NET 8** e **OpenTelemetry**, com foco em SOLID, desacoplamento e extensibilidade.

---

## 1. Contexto padrão

Use sempre este contexto ao pedir geração ou refatoração de código:

- Plataforma: `.NET 8`, preferencialmente usando `Generic Host` (`Worker Service`, console, serviços background ou APIs).
- Cenário geral: aplicações distribuídas que podem:
  - Publicar mensagens em filas (ex.: RabbitMQ via Rebus ou equivalente).
  - Consumir mensagens dessas filas.
  - Expor APIs HTTP ou gRPC (quando existir).
- Telemetria:
  - Usar **OpenTelemetry** para **traces**, **métricas** e **logs**.
  - Enviar tudo para um **OpenTelemetry Collector** via **OTLP/gRPC (porta 4317 por padrão)**.
  - O destino final (Jaeger, Grafana, Tempo, Loki, Prometheus etc.) será sempre tratado como responsabilidade do Collector, não da aplicação.
- Configuração:
  - Tudo configurável via `appsettings` + variáveis de ambiente.
  - Nada de endpoint, chave ou destino “fixo” no código.

---

## 2. Objetivos da telemetria

Ao gerar código, sempre garantir:

1. **Traces distribuídos ponta a ponta**
   - Desde a entrada da requisição ou geração da mensagem até o processamento final.
   - Propagação de contexto entre serviços e através da mensageria (headers).

2. **Métricas técnicas e de negócio**
   - Taxa de mensagens/requests.
   - Latências (processamento, filas, chamadas externas).
   - Erros técnicos e de negócio, idealmente categorizados.

3. **Logs estruturados correlacionados com traces**
   - Logs em formato estruturado (idealmente JSON).
   - Sempre com `traceId` e `spanId`.
   - Campos de domínio relevantes (ex.: `transactionId`, `status`, `amount`, `queue`, `routingKey`), evitando dados sensíveis.

4. **Boas práticas de arquitetura**
   - Respeito estrito a **SOLID**.
   - Telemetria como **infraestrutura**, não misturada com regras de negócio.
   - Fácil desligar/alterar telemetria sem quebrar o domínio.

---

## 3. Requisitos técnicos para geração de código

Ao responder qualquer pedido de código com este prompt, a IA deve:

### 3.1. Dependências e pacotes

- Usar os pacotes oficiais de OpenTelemetry para .NET 8:
  - Traces
  - Métricas
  - Logs
  - Exportador OTLP
- Usar instrumentação automática quando disponível (ASP.NET Core, HttpClient, etc.) e complementar com **instrumentação manual** quando necessário (mensageria, domínio).

### 3.2. Configuração central de telemetria

- Criar uma classe de configuração central, por exemplo:
  - `TelemetryConfigurator`
  - `ObservabilityExtensions`
- Essa classe deve expor **métodos de extensão** para:
  - `IHostApplicationBuilder` ou
  - `IServiceCollection`
- Responsabilidades dessa classe:
  - Configurar `ResourceBuilder` com:
    - Nome do serviço (ex.: `service.name` = `TODO-ServiceName`)
    - Versão (ex.: `service.version`)
    - Ambiente (ex.: `deployment.environment` = `Development`, `Staging`, `Production`)
    - Tipo de componente (ex.: `service.instance.id`, `service.role` = `Producer`, `Consumer`, `API` etc.)
  - Registrar **traces, métricas e logs** em um ponto único e coeso.
  - Configurar exportação via OTLP usando valores de configuração:
    - `OpenTelemetry:Otlp:Endpoint` (ex.: `http://otel-collector:4317`)
    - Flags como:
      - `OpenTelemetry:Tracing:Enabled`
      - `OpenTelemetry:Metrics:Enabled`
      - `OpenTelemetry:Logging:Enabled`
    - Política de amostragem:
      - Começar com `AlwaysOn` para PoC (`OpenTelemetry:Tracing:Sampling`), mas deixar configurável.

### 3.3. Traces distribuídos e mensageria

Para qualquer sistema que use mensageria (ex.: RabbitMQ via Rebus):

**Producer (publicador de mensagens)**:

- Criar spans para:
  - Geração do evento/domínio (ex.: `GenerateTransaction`).
  - Publicação da mensagem (ex.: `PublishTransactionMessage`).
- Atributos mínimos no span:
  - Identificador lógico do evento (ex.: `transaction.id`).
  - Tipo de operação (ex.: `operation.type`).
  - Fila ou exchange (ex.: `messaging.destination`).
  - Resultado (sucesso/erro).
- Propagar o contexto de trace nos headers da mensagem (W3C `traceparent`/`tracestate`).

**Consumer (consumidor de mensagens)**:

- Extrair o contexto de trace dos headers.
- Criar spans que **continuem** o trace:
  - Nome sugestivo, ex.: `ProcessTransactionMessage`.
- Atributos mínimos:
  - `transaction.id`
  - Nome da fila/rota
  - Tentativas/retries
  - Indicação de DLQ se aplicável
- Diferenciar claramente:
  - Erros de negócio (ex.: regra de domínio) vs.
  - Erros técnicos (ex.: conexão, timeout, serialização).

Implementação sugerida:

- Usar **decorators** em handlers (ex.: handlers do Rebus) para:
  - Criar/fechar spans.
  - Registrar métricas.
  - Fazer logging.

### 3.4. Métricas

- Criar um `Meter` por contexto de serviço (ex.: `TODO.ServiceName.Telemetry`).
- Definir instrumentos como:
  - `Counter<long>` para:
    - Mensagens geradas.
    - Mensagens processadas com sucesso.
    - Mensagens processadas com erro (por tipo).
    - Retries e DLQ.
  - `Histogram<double>` para:
    - Latência de processamento (end-to-end e/ou por componente).
- Encapsular a criação e uso das métricas em classes específicas (ex.: `TransactionMetrics`, `MessagingMetrics`) para manter SRP e facilitar testes.

### 3.5. Logs estruturados

- Integrar o pipeline de logging do .NET com OpenTelemetry.
- Geração de logs:
  - Sempre com campos de correlação: `traceId`, `spanId`.
  - Campos de domínio relevantes (ex.: `transactionId`, `status`, `amount`, `queue`).
- Se necessário, criar adaptadores/decorators sobre `ILogger` respeitando o **DIP**:
  - Depender de interfaces.
  - Injetar via DI.

---

## 4. Arquitetura e SOLID

Ao gerar código, seguir estas diretrizes:

- **Single Responsibility Principle (SRP)**  
  - Classes de domínio não devem conhecer diretamente APIs de OpenTelemetry.
  - Configuração de telemetria centralizada em uma camada de infraestrutura.

- **Open/Closed Principle (OCP)**  
  - Facilitar extensão de telemetria (novas métricas, novos exporters) sem modificar código de domínio.
  - Uso de métodos de extensão e composição.

- **Liskov Substitution Principle (LSP)**  
  - Decorators de handlers ou serviços devem respeitar interfaces originais.

- **Interface Segregation Principle (ISP)**  
  - Interfaces pequenas para funcionalidades específicas (ex.: propagação de contexto, registradores de métricas).

- **Dependency Inversion Principle (DIP)**  
  - Domínio depende de abstrações, nunca de detalhes de OpenTelemetry ou mensageria.
  - Telemetria plugada via infraestrutura e DI.

---

## 5. Estrutura de configuração (appsettings)

Sempre sugerir algo nesta linha (adaptar conforme o projeto):

```

{
"OpenTelemetry": {
"ServiceName": "TODO-ServiceName",
"ServiceVersion": "1.0.0",
"Environment": "Development",
"Otlp": {
"Endpoint": "http://otel-collector:4317"
},
"Tracing": {
"Enabled": true,
"Sampling": "AlwaysOn"
},
"Metrics": {
"Enabled": true
},
"Logging": {
"Enabled": true
},
"Role": "TODO-Role(Producer|Consumer|API)"
}
}

```

---

## 6. O que a IA deve sempre entregar

Ao pedir código com este prompt, exigir que a resposta inclua:

1. **Código de configuração de OpenTelemetry**
   - Métodos de extensão para registrar:
     - Resource
     - Traces
     - Métricas
     - Logs
     - Exporter OTLP
   - Leitura de configurações do `appsettings`/variáveis de ambiente.

2. **Exemplos de instrumentação**
   - Para produtores de mensagens:
     - Criação de spans e métricas ao publicar.
     - Propagação de contexto nos headers.
   - Para consumidores:
     - Extração de contexto.
     - Spans em torno do processamento.
     - Métricas e logs relacionados.

3. **Separação clara de camadas**
   - Código de telemetria em projeto/camada de infraestrutura (ex.: `Infrastructure.Telemetry`), não misturado com domínio.
   - Uso de DI para plugar telemetria sem afetar a lógica de negócio.

4. **Comentários explicando decisões**
   - Explicar brevemente:
     - Por que certos atributos são adicionados nos spans.
     - Como o design respeita SOLID.
     - Como desligar/ajustar telemetria apenas pela configuração.

---

## 7. Como usar este prompt

- Sempre que for pedir geração ou refatoração de código de observabilidade/telemetria para uma aplicação .NET 8:
  - Copiar este markdown.
  - Adicionar um bloco final com o contexto específico do serviço, por exemplo:

---

### Contexto específico deste serviço

- Nome do serviço: MyNewLittleBank
- Papel: Producer (publica transações financeiras em RabbitMQ via Rebus).
- Responsabilidades principais:
  - Gerar transações de teste.
  - Publicar mensagens de transação em uma fila específica.
- Necessidades extras:
  - Métricas de throughput (transações por segundo).
  - Métricas de erro por tipo de exceção.
