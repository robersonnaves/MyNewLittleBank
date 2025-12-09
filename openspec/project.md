# Project Context

## Purpose
**Padrão Arquitetural**: Arquitetura de microsserviços orientada a eventos baseada em Clean Architecture com DDD.
**Direção de Dependências**: `Services → UseCases → Domain ← Infrastructure` e `Shared` (sem dependências).
**Princípios SOLID obrigatórios**: Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion.

## Tech Stack
- **Core**: .NET 8 (LTS ou ST)
- **Database**: PostgreSQL
- **Messaging**: RabbitMQ (RabbitMQ.Client 7.x+)
- **ORM**: Entity Framework Core
- **Observability**: OpenTelemetry (OTLP), Serilog (logs estruturados)
- **Containerization**: Podman com `docker-compose.yml`, builds multi-stage.

## Project Conventions

### Code Style
#### Nomenclatura
- **PascalCase**: Classes, Interfaces, Métodos, Propriedades.
- **camelCase**: Parâmetros, variáveis.
- **_camelCase**: Campos privados.
- **snake_case**: Nomes de tabelas e colunas no PostgreSQL.
- **Sufixos**: `Async` para métodos assíncronos.
- **Prefixos**: `I` para interfaces.

#### Modificadores e Práticas
- Especificar explicitamente: `public`, `private`, `protected`, `internal`.
- `sealed` para classes não herdáveis.
- `readonly` para campos imutáveis.
- File-scoped namespaces.
- Nullable reference types habilitados.
- `ImplicitUsings` habilitado.
- XML comments em membros públicos.
- Async/await com `CancellationToken` em toda a cadeia.

### Architecture Patterns
#### Estrutura de Projetos
- **Domain**: Aggregates, ValueObjects, Domain Events, Interfaces.
- **UseCases**: Lógica de aplicação, orquestração.
- **Infra.Database**: Persistence, Context, UoW.
- **Infra.Message**: RabbitMQ Implementation.
- **Services**: Workers (Receivers, Publishers).
- **Shared**: Results, Errors, Configuration.

#### Domain Layer Rules
- **Aggregate Roots**: Construtor privado, Factory Method `Create()` retorna `Result<T>`, Domain Events (`Created`, `Updated`).
- **Value Objects**: Imutáveis, Construtor privado, Factory `Create()`, Sobrescrever `Equals`/`GetHashCode`.
- **Entidades Base**: `Entity` (Id, Dates), `AggregateRoot` (Events).

#### Infrastructure Layer Rules
- **Database**:
  - `LazyLoadingEnabled = false` (Proibido habilitar).
  - Padrão **Repository** Genérico (`IWrite`, `IRead`, `ISpecification`) - Não criar específicos.
  - Padrão **Unit of Work**.
  - **Outbox Pattern** para confiabilidade de mensagens.
- **Messaging**:
  - RabbitMQ Client 7.x (Async API).
  - **Inbox Pattern** para idempotência.

#### Result Pattern
- Utilizar `Result<T>` para retorno de operações.
- Tipos de erro: `ValidationError`, `DomainError`, `NotFoundError`, `InfrastructureError`.
- **Exceções**: Apenas para erros inesperados (bugs, falhas críticas), não para regras de negócio.

### Testing Strategy
- **Framework**: xUnit (implícito no ecossistema .NET).
- **Assertions**: AwesomeAssertions (v9.3.0) - *Substituto obrigatório para FluentAssertions*.
- **Unit Tests**: Domain/UseCases isolados com Mocks.
- **Integration Tests**: Infrastructure/Services com **Testcontainers** (PostgreSQL, RabbitMQ).
- **Nomenclatura**: `MethodName_Scenario_ExpectedResult`.

### Git Workflow
- Versionamento Semântico.
- Commits atômicos e descritivos.

## Domain Context
### Entidades Principais
- **`Client`**: Cliente do banco.
- **`BankAccount`**: Conta bancária com transações.
- **`Transaction`**: Classe base Polimórfica (TPH).
  - `PixTransaction`
  - `MoneyTransaction`
  - `CardTransaction`

## Important Constraints
### Proibições Tecnológicas (Banned Libraries)
- ❌ **MediatR**: Usar **LiteBus** (v4.2.0).
- ❌ **AutoMapper**: Usar **Implicit/Explicit Operators**.
- ❌ **FluentAssertions**: Usar **AwesomeAssertions**.
- ❌ **MassTransit**: Usar implementação nativa com RabbitMQ.Client.

### Restrições de Banco de Dados
- **Migrations**: Não criar, aplicar ou executar automaticamente via código/pipeline. O banco é recriado manualmente em alterações estruturais.
- **Delete Behavior**: `Restrict` para todas as Foreign Keys.

## External Dependencies
- **PostgreSQL**: Persistência principal.
- **RabbitMQ**: Broker de mensageria.
- **OpenTelemetry Collector**: Coleta de traces e métricas.
