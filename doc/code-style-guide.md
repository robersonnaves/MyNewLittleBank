---
trigger: always_on
---

# Regras de Desenvolvimento - MyNewLittleBank

Padrões obrigatórios baseados em Clean Architecture com DDD para arquitetura de microsserviços orientada a eventos.

## Arquitetura

**Padrão Arquitetural**: Arquitetura de microsserviços orientada a eventos

**Direção de Dependências**: `Services → UseCases → Domain ← Infrastructure` e `Shared` (sem dependências)

**Princípios SOLID obrigatórios**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion

**Estrutura de Projetos**:

- **Domain**: Aggregates, ValueObjects, Domain Events, Interfaces
- **UseCases**: Lógica de aplicação que orquestra o domínio
- **Infra.Database**: Persistence, Repositories, Migrations, DbContext, Unit of Work
- **Infra.Message**: RabbitMQ Publisher/Consumer, Connection Factory
- **Services**: Workers executáveis (PixTransactionReceiver, MoneyTransactionReceiver, HeartbeatPublisher/Consumer)
- **Mock.Transactions**: Worker para gerar transações de teste
- **Shared**: Results, Errors, Interfaces, Configuration

**Containerização**: Podman com `docker-compose.yml`. Builds multi-stage, healthchecks e imagens enxutas.

**Stack Tecnológica**:

- .NET 8 (LTS ou ST)
- PostgreSQL
- RabbitMQ (RabbitMQ.Client 7.x+)
- Entity Framework Core
- OpenTelemetry (OTLP)
- Serilog (logs estruturados)

**Restrições Tecnológicas**: **NÃO utilizar os pacotes MediatR, AutoMapper, FluentAssertions ou MassTransit**.
 - No lugar do MediatR, usar LiteBus (versão 4.2.0) (https://github.com/litenova/LiteBus) com o exemplo de implementação (https://dev.to/litenova/litebus-a-free-alternative-to-mediatr-for-net-applications-1mdp).
 - No lugar do AutoMapper, usar a solução nativa, Implicit/Explicit Operators.
 - Nos testes, no lugar do FluentAssertions, usar AwesomeAssertions (versão 9.3.0) (https://github.com/AwesomeAssertions/AwesomeAssertions).


## Camada Domain

### Aggregate Roots

- Herdar `AggregateRoot`, construtor privado
- Factory method `Create()` retorna `Result<T>` com validações
- Propriedades com setters privados
- Emitir Domain Events (Created, Updated, Deactivated)
- Implementar `ISoftDeletable` quando aplicável

### Value Objects

- Imutáveis, construtor privado, factory `Create()` retorna `Result<T>`
- Sobrescrever `Equals()`, `GetHashCode()`, operadores `==` e `!=`, `ToString()`
- Validar no factory method

### Entidades Principais

- **`Client`**: Cliente do banco, pode ter várias contas
- **`BankAccount`**: Conta bancária de um cliente, pode ter várias transações
- **`Transaction`**: Classe base para transações financeiras (PixTransaction, MoneyTransaction, CardTransaction)

### Entidades Base

- `Entity`: Base com `Id`, `DataCadastro`, `DataAlteracao`, `UpdateModificationDate()`
- `AggregateRoot`: Gerencia `DomainEvents` (AddDomainEvent, ClearDomainEvents)

## Camada UseCases

### Use Cases

- Cada UseCase representa uma operação de negócio específica (ex: `ProcessTransactions`)
- Usar `IUnitOfWork` e repositórios para buscar entidades e persistir mudanças
- Retornar `Result<T>` para indicar sucesso ou falha
- Validar regras de negócio antes de executar operações
- Usar `CancellationToken` em todos os métodos assíncronos

### DTOs de Mensagem

- DTOs para comunicação via RabbitMQ (ex: `PixTransactionDTO`, `MoneyTransactionDTO`)
- Propriedades init-only quando possível
- Serialização com `System.Text.Json` usando contexts gerados
- Incluir `MessageId` e `traceparent` para idempotência e tracing

## Camada Infrastructure

### Infra.Database

#### DbContext

- `LazyLoadingEnabled = false` (NUNCA habilitar)
- `DeleteBehavior.Restrict` para todas as FKs
- Filtros globais: soft delete (`ISoftDeletable`)
- Configurar para PostgreSQL com snake_case

#### Entity Configurations

- Cada entidade tem configuração explícita `IEntityTypeConfiguration<T>`
- **TPH (Table-per-Hierarchy)**: Todas as transações (`PixTransaction`, `MoneyTransaction`, `CardTransaction`) em uma única tabela `Transactions` com coluna discriminator
- Value Objects com conversores e `ValueComparer`
- Índices em campos de busca (Email, AccountNumber, etc.)

#### Repositórios Genéricos (NÃO criar específicos)

- `IWriteRepository<T>`: Add, Update, Remove (soft delete), SaveChanges
- `IReadRepository<T>`: GetById, List, Count, Exists
- `ISpecificationRepository<T>`: GetBySpec, ListAsync, CountAsync

#### Migrations

- PostgreSQL com snake_case
- Criar as configurações no contexto do Entity Framework Core
- **NÃO CRIAR, APLICAR OU EXECUTAR AS MIGRATIONS**
- O banco de dados será sempre recriado totalmente de forma manual quando houver alteração na estrutura das tabelas

#### Unit of Work

- Padrão Unit of Work para agrupar operações de banco em uma única transação
- Garantir consistência dos dados
- Implementar `IUnitOfWork` com `SaveChangesAsync()`

#### Outbox Pattern

- Registrar eventos/mensagens no commit da transação
- Dispatcher em background lê e publica de forma confiável
- Garantir que nenhuma mensagem seja perdida

#### Inbox Pattern

- Registrar processamento por `MessageId` para garantir idempotência
- Evitar processamento duplicado de mensagens
- Implementar em todos os consumidores

### Infra.Message

#### RabbitMQ.Client 7.x - Migração e Práticas

**IMPORTANTE**: A partir da versão 7.0 do RabbitMQ.Client, houve mudanças significativas:

- **IModel → IChannel**: A interface `IModel` foi renomeada para `IChannel` para alinhar com outros clientes
- **API Assíncrona**: Todos os métodos agora são assíncronos (ex: `BasicPublishAsync`, `BasicAckAsync`, `BasicConsumeAsync`)
- **CreateChannelAsync**: `CreateModel()` foi substituído por `CreateChannelAsync(CreateChannelOptions?, CancellationToken)`
- **Publisher Confirms**: Habilitados via `CreateChannelOptions.PublisherConfirms = true` ao criar o canal
- **CancellationToken**: Métodos assíncronos aceitam `CancellationToken` para controle de cancelamento

**Padrões Obrigatórios**:
- ✅ Usar `IChannel` em vez de `IModel`
- ✅ Usar `CreateChannelAsync()` com `CreateChannelOptions` para configurar publisher confirms
- ✅ Usar métodos assíncronos: `BasicPublishAsync`, `BasicAckAsync`, `BasicConsumeAsync`, `BasicQosAsync`
- ✅ Sempre usar `ConfigureAwait(false)` em chamadas assíncronas
- ✅ Passar `CancellationToken` em todos os métodos assíncronos
- ❌ NÃO usar `CreateModel()` (obsoleto)
- ❌ NÃO usar métodos síncronos como `BasicPublish`, `BasicAck`, `WaitForConfirmsOrDie`

#### RabbitMQ Connection Factory

- `RabbitConnectionFactory` para gerenciar conexões
- `CreateChannelAsync()` retorna `Task<IChannel>` com suporte a `CreateChannelOptions`
- Reutilização de conexões e canais
- Tratamento de reconexão automática
- Configurar `PublisherConfirms = true` via `CreateChannelOptions` ao criar canais para publishers

#### Publisher Service

- `PublisherService` com publisher confirms habilitados via `CreateChannelOptions`
- Usar `BasicPublishAsync()` com `CancellationToken` para aguardar confirmações
- Propagar `MessageId` e `traceparent` em headers
- Serialização com `System.Text.Json`
- Retries com filas de atraso (TTL)
- Métricas de publicação disponíveis
- Inicialização lazy do canal usando `Lazy<Task<IChannel>>` para evitar inicialização assíncrona no construtor

#### Consumer Service

- `ConsumerService` com ack manual usando `BasicAckAsync()`
- Usar `BasicConsumeAsync()` para iniciar consumo
- Usar `BasicQosAsync()` para configurar prefetch
- Processamento idempotente usando Inbox
- Extrair `MessageId` e `traceparent` dos headers
- Retries com DLQ (Dead Letter Queue) usando `BasicPublishAsync()`
- Usar `x-dead-letter-exchange` e `routing-key`

#### Topologia RabbitMQ

- Padronização de exchanges/queues por tipo de transação
- Filas de atraso para retries
- DLQ por fila para mensagens que falharam após múltiplas tentativas

## Camada Services (Workers)

### Workers Executáveis

- Cada worker roda em seu próprio contêiner
- Implementar `IHostedService` com `CancellationToken`
- Encerramento gracioso com shutdown adequado
- Health checks para monitoramento

### Tipos de Workers

- **Transaction Receivers**: `PixTransactionReceiver`, `MoneyTransactionReceiver`, `CardTransactionReceiver`
- **Heartbeat**: `HeartbeatPublisher`, `HeartbeatConsumer` para monitoramento
- **Mock**: `Mock.Transactions` para gerar transações de teste

### Processamento de Mensagens

- Desserializar mensagem do RabbitMQ
- Invocar UseCase apropriado
- Registrar no Inbox após processamento bem-sucedido
- Ack manual apenas após confirmação de persistência

## Observabilidade

### OpenTelemetry

- Instrumentar todos os `Services/*` com OpenTelemetry (traces, métricas, logs)
- Propagação de contexto: inserir/extrair `traceparent` nos headers RabbitMQ
- Correlação com operações de banco de dados
- Export OTLP para Jaeger/Tempo e Prometheus

### Logging

- Logs estruturados com Serilog para OpenSearch
- Logs correlacionados sem PII (Personally Identifiable Information)
- Níveis de log controlados por ambiente
- Amostragem para reduzir overhead

### Métricas

- Métricas por tipo de transação
- Métricas de retries e DLQ
- Health checks para RabbitMQ, Postgres e workers

### Tracing

- Traces de ponta-a-ponta visíveis
- Correlação entre mensagens e operações de banco
- Contexto distribuído via headers RabbitMQ

## Result Pattern & Errors

### Result

- `Result.Success()` / `Result<T>.Success(value)` / `Result.Failure(errors)`
- Verificar `IsSuccess` / `IsFailure`, acessar `Value` apenas se sucesso

### Tipos de Erro (UPPER_SNAKE_CASE)

- `ValidationError`: dados inválidos
- `DomainError`: regra de negócio violada
- `NotFoundError`: recurso não encontrado (ex: conta não encontrada)
- `InfrastructureError`: erro de infraestrutura (banco, RabbitMQ)

### Exceções vs Result

- ✅ Result para erros esperados (validações, regras de negócio)
- ✅ Exceções para erros inesperados (bugs, falhas de infraestrutura)

### Error Handling Strategy

**Transaction Receivers distinguem entre business errors e technical errors:**

- **Business errors**: Retornar sem lançar exceção (mensagem reconhecida, sem retry)
  - Exemplos: `insufficient_funds`, `bank_account_not_found`, `client_not_found`, `invalid_cpf`, `invalid_account_number`, `invalid_money`, `invalid_transaction`
  - Log: `Warning` level (EventId=1)
  - Comportamento: Acknowledge message without retry

- **Technical errors**: Lançar exceção para disparar retry
  - Exemplos: `database_connection_error`, `network_timeout`, `service_unavailable`
  - Log: `Error` level (EventId=5)
  - Comportamento: Message retry (até max retries) → Dead Letter Queue (DLQ)

**Documentação completa:** Ver `doc/error-handling.md` para strategy detalhada, lista completa de erros, fluxos de mensagem, observabilidade e best practices.

## Validações

### Múltiplas Camadas

1. **DTOs (Mensagens)**: estrutura e formato básico
2. **Value Objects (Domain)**: regras de negócio de valores
3. **Aggregate Roots (Domain)**: regras de negócio de entidades
4. **UseCases**: validações de contexto e orquestração

### Códigos de Erro

- UPPER_SNAKE_CASE: `EMAIL_REQUIRED`, `ACCOUNT_NOT_FOUND`, `INSUFFICIENT_BALANCE`

## Configuração e Saúde

### Configuração

- `Options<T>` centralizados em `Shared/Configuration` com validação no startup
- Variáveis de ambiente para configuração
- User Secrets para desenvolvimento local
- Validação obrigatória de configurações críticas

### Health Checks

- `Microsoft.Extensions.Diagnostics.HealthChecks` para RabbitMQ, Postgres e workers
- Endpoints de health check em cada worker
- Integração com orquestradores (Docker healthchecks)

### Segurança

- Segredos via variáveis de ambiente/User Secrets
- TLS em brokers/DB quando possível
- Redaction de logs (não expor dados sensíveis)
- Validação de entrada em todos os pontos

## Testes

### Estrutura

- Estrutura espelha código: `Domain.Tests/`, `UseCases.Tests/`, `Infrastructure.Tests/`, `Services.Tests/`
- Nomenclatura: `MethodName_Scenario_ExpectedResult`
- Arrange-Act-Assert pattern

### Testes Unitários

- Domain/UseCases (sem dependências externas)
- Mocks para repositórios e serviços
- Testes de Value Objects e Aggregate Roots

### Testes de Integração

- Infrastructure/Services (com DB e RabbitMQ)
- Usar Testcontainers para PostgreSQL e RabbitMQ
- Testes de publicação/consumo de mensagens
- Testes de Outbox dispatch e Inbox dedupe
- Testes de persistência EF

### Testes de Integração com Testcontainers

- Fixtures que usam Testcontainers devem detectar automaticamente Docker
- Testes executáveis com `dotnet test` sem scripts auxiliares
- Usar `docker-compose` dos serviços de infraestrutura
- Cobertura: publicação/consumo, outbox dispatch, inbox dedupe, persistência EF, métricas/trace básicos

## Convenções

### Nomenclatura

- PascalCase: Classes, Interfaces, Métodos, Propriedades
- camelCase: parâmetros, variáveis
- _camelCase: campos privados
- Prefixo `I` para interfaces
- Sufixo `Async` para métodos assíncronos
- snake_case: nomes de tabelas e colunas no PostgreSQL

### Modificadores

- Especificar explicitamente: `public`, `private`, `protected`, `internal`
- `sealed` para classes não herdáveis
- `readonly` para campos imutáveis após construção

### Código

- File-scoped namespaces
- Nullable reference types habilitado (`Nullable` em `Directory.Build.props`)
- `ImplicitUsings` habilitado
- `AnalysisLevel` configurado
- XML comments em membros públicos
- Async/await com `CancellationToken` em toda a cadeia
- `ValueTask` e buffers (`ReadOnlyMemory<byte>`) em hot paths
- `FrozenDictionary`/`ImmutableArray` para configurações estáticas

### Performance

- `System.Text.Json` com contexts gerados
- Uso de `ValueTask` quando apropriado
- Buffers (`ReadOnlyMemory<byte>`) em hot paths
- Coleções imutáveis para configurações estáticas

### Encerramento Gracioso

- Cancelamento e shutdown via `IHostedService` com `CancellationToken`
- Aguardar conclusão de operações em andamento
- Fechar conexões adequadamente

## Podman ou Docker e CI/CD

### Podman ou Docker

- Builds multi-stage por serviço
- Healthchecks em cada serviço
- Imagens enxutas (AOT quando viável)
- Labels e metadados apropriados

### Podman Compose ou Docker Compose

- Redes nomeadas
- Dependências explícitas
- Healthchecks e limites de recursos
- Perfis para local/CI

### CI/CD

- Testes de integração (subindo serviços)
- Build/push de imagens
- Versionamento semântico

### DevContainer

- `.devcontainer` para ambiente padronizado
- Acesso aos serviços (PostgreSQL, RabbitMQ)
- Configuração de desenvolvimento consistente

## Diretório Build Props

- Centralizar versões e propriedades em `Directory.Build.props`
- Reduzir duplicação entre projetos
- Versões pinadas de pacotes
- Configurações globais: `net8.0`, `Nullable`, `ImplicitUsings`, `AnalysisLevel`

**Última atualização**: 2025-12-02
