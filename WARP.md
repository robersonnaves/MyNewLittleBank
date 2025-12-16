# WARP.md

This file provides guidance to WARP (warp.dev) when working with code in this repository.

## Visão Geral do Projeto

MyNewLittleBank é uma arquitetura de microsserviços orientada a eventos para um sistema bancário, construído com .NET 8, usando Clean Architecture e Domain-Driven Design (DDD).

### Stack Tecnológica
- **.NET**: 8.0 (LTS)
- **Database**: PostgreSQL com Entity Framework Core 9.x
- **Message Broker**: RabbitMQ (RabbitMQ.Client 7.x)
- **Observabilidade**: OpenTelemetry (OTLP) + Serilog + Jaeger + Prometheus + OpenSearch
- **Containerização**: Podman/Docker com docker-compose
- **Testes**: xUnit + AwesomeAssertions + Testcontainers

## Comandos de Desenvolvimento

### Comandos Rápidos do Dia a Dia
```bash
# Subir infraestrutura + API + Workers
export CONTAINER_ENGINE=podman
./infra/scripts/bootstrap-compose.sh local
./infra/scripts/start-all.sh

# Executar todos os testes (unitários + integração)
export CONTAINER_ENGINE=podman
./infra/scripts/run-integration-tests.sh

# Ver logs de um serviço específico
podman logs -f mynewlittlebank-postgres-1
podman logs -f mynewlittlebank-rabbitmq-1

# Verificar containers em execução
podman ps
```

### Build e Restore
```bash
# Restaurar dependências
dotnet restore

# Build da solução
dotnet build --configuration Release

# Build de um projeto específico
dotnet build src/API/API.csproj

# Limpar build artifacts
dotnet clean
```

### Executar Serviços
```bash
# API REST
dotnet run --project src/API

# Workers (serviços de processamento de mensagens)
dotnet run --project src/Services.Pix
dotnet run --project src/Services.Card
dotnet run --project src/Services.Money
dotnet run --project src/Services.Heartbeats

# Gerador de transações mock
dotnet run --project src/Mock.Transactions

# Executar todos os serviços (infraestrutura deve estar rodando)
./infra/scripts/start-all.sh
```

### Testes

#### Testes Unitários
```bash
# Todos os testes unitários (exclui integração)
dotnet test --filter "Category!=Integration"

# Testes de um projeto específico
dotnet test tests/Domain.Tests
dotnet test tests/UseCases.Tests

# Executar teste específico
dotnet test --filter "FullyQualifiedName~ClientTests.Create_Should_ReturnSuccess"

# Ver output detalhado
dotnet test --verbosity detailed
```

#### Testes de Integração
```bash
# Usando script (sobe compose profile ci automaticamente)
# Para Podman (padrão):
export CONTAINER_ENGINE=podman
./infra/scripts/run-integration-tests.sh

# Para Docker:
export CONTAINER_ENGINE=docker
./infra/scripts/run-integration-tests.sh

# Executar diretamente (requer infraestrutura rodando)
dotnet test tests/Integration/MyNewLittleBank.Tests.Integration.csproj --filter "Category=Integration"
```

### Infraestrutura (Podman/Docker)

#### Subir Infraestrutura Local
```bash
# Para Podman (padrão - preferido no Fedora):
export CONTAINER_ENGINE=podman
./infra/scripts/bootstrap-compose.sh local

# Para Docker:
export CONTAINER_ENGINE=docker
./infra/scripts/bootstrap-compose.sh local

# Subir direto com podman:
podman compose -f infra/docker-compose.yml up -d
```

**Serviços disponíveis**:
- **PostgreSQL**: localhost:5432 (postgres/postgres)
- **RabbitMQ**: localhost:5672 (guest/guest) | Management: http://localhost:15672
- **OpenSearch**: http://localhost:9200
- **Jaeger UI**: http://localhost:16686
- **Prometheus**: http://localhost:9090

#### Derrubar Infraestrutura
```bash
# Para Podman:
podman compose -f infra/docker-compose.yml down -v --remove-orphans

# Para Docker:
docker compose -f infra/docker-compose.yml --profile local down -v --remove-orphans

# Parar todos os serviços da aplicação:
./infra/scripts/stop-all.sh
```

#### Resetar Banco de Dados
```bash
# Reset completo do banco (DESTRUTIVO)
./infra/scripts/reset-database.sh

# Com backup antes de dropar
./infra/scripts/reset-database.sh --backup

# Sem confirmação (para CI/CD)
./infra/scripts/reset-database.sh --force
```

## Arquitetura e Estrutura de Código

### Direção de Dependências
```
Services → UseCases → Domain ← Infrastructure
                      ↑
                    Shared (sem dependências)
```

### Estrutura de Projetos

#### src/Domain
**Responsabilidade**: Lógica de negócio pura, entidades, value objects, interfaces de repositório.

**Entidades Principais**:
- `Client`: Cliente do banco (CPF, nome, email, telefone)
- `BankAccount`: Conta bancária com saldo e histórico de transações
- `Transaction` (abstrata): Base para `PixTransaction`, `MoneyTransaction`, `CardTransaction`

**Padrões**:
- Construtores privados + factory methods `Create()` retornando `Result<T>`
- Value Objects imutáveis (ex: `Cpf`, `Money`, `AccountNumber`, `TransactionId`)
- Nenhuma dependência de infraestrutura

#### src/UseCases
**Responsabilidade**: Orquestração de lógica de negócio, coordenação de repositórios.

**Handlers**:
- `CreateClientHandler`: Criar novo cliente
- `GetClientHandler`: Buscar cliente por ID
- `GetAccountHandler`: Buscar conta por número
- `GetAccountBalanceHandler`: Consultar saldo de conta
- `ProcessTransactionsHandler`: Processar transações financeiras (usado pelos workers)

**Padrões**:
- Usa `IUnitOfWork` para transações de banco
- Retorna `Result<T>` para indicar sucesso/falha
- Aceita `CancellationToken` em todos os métodos assíncronos

#### src/Infra.Database
**Responsabilidade**: Persistência, repositórios, DbContext, Outbox/Inbox pattern.

**Componentes**:
- `MyNewLittleBankContext`: DbContext principal com PostgreSQL e snake_case
- Repositórios genéricos: `IWriteRepository<T>`, `IReadRepository<T>`
- `IUnitOfWork`: Gerenciamento de transações
- `OutboxDispatcher`: Background service para garantir entrega de mensagens
- Inbox pattern para idempotência de consumidores

**Importante**:
- **NÃO criar, aplicar ou executar migrations** - banco é recriado manualmente
- `LazyLoadingEnabled = false` (sempre)
- TPH (Table-per-Hierarchy) para transações - todas em uma tabela com discriminator
- Conversores para Value Objects (Money, Cpf, AccountNumber, etc.)

#### src/Infra.Message
**Responsabilidade**: Comunicação via RabbitMQ (publisher e consumer).

**Componentes**:
- `RabbitConnectionFactory`: Gerenciamento de conexões RabbitMQ
- `PublisherService`: Publica mensagens com publisher confirms
- `RabbitConsumerService`: Base para consumidores com ack manual

**IMPORTANTE - RabbitMQ.Client 7.x**:
- Usar `IChannel` (não `IModel` - obsoleto)
- Usar `CreateChannelAsync()` com `CreateChannelOptions`
- TODOS os métodos são assíncronos: `BasicPublishAsync`, `BasicAckAsync`, `BasicConsumeAsync`, `BasicQosAsync`
- Sempre usar `ConfigureAwait(false)` em chamadas assíncronas
- Passar `CancellationToken` em métodos assíncronos
- Publisher confirms habilitados via `CreateChannelOptions.PublisherConfirms = true`

#### src/Services.*
**Responsabilidade**: Workers executáveis que consomem mensagens RabbitMQ.

**Workers disponíveis**:
- `Services.Pix`: Processa transações PIX
- `Services.Card`: Processa transações de cartão
- `Services.Money`: Processa transferências
- `Services.Heartbeats`: Monitoramento de saúde do sistema
- `Mock.Transactions`: Gerador de transações para testes

**Padrões**:
- Implementam `IHostedService` ou `BackgroundService`
- Consomem de filas RabbitMQ específicas por tipo de transação
- Registram mensagens no Inbox para idempotência
- Ack manual apenas após persistência bem-sucedida

#### src/API
**Responsabilidade**: API REST para gerenciamento de clientes e contas.

**Endpoints principais**:
- Clientes: criar, buscar por ID
- Contas: buscar por número, consultar saldo

**Observabilidade**:
- `/health/live`: Liveness check
- `/health/ready`: Readiness check (PostgreSQL + RabbitMQ)
- `/metrics`: Prometheus scraping endpoint
- Swagger UI (habilitado em Development e Staging)

#### src/Shared
**Responsabilidade**: Código compartilhado sem dependências (Results, Errors, Extensions).

**Componentes**:
- `Result<T>` pattern para tratamento de erros
- Error types: `ValidationError`, `DomainError`, `NotFoundError`, `InfrastructureError`
- Health checks customizados
- Configuração de observabilidade

### Padrões Arquiteturais Importantes

#### Result Pattern
Usado em toda a aplicação para erros esperados (evita exceções para fluxo de controle):
```csharp
var result = Client.Create(id, cpf, name, email, mobile);
if (result.IsFailure)
{
    return Result<ClientDto>.Failure(result.Errors);
}
```

#### Outbox Pattern
Mensagens são gravadas no banco junto com a transação e publicadas assincronamente pelo `OutboxDispatcher` para garantir entrega.

#### Inbox Pattern
Consumidores registram `MessageId` processados para evitar processamento duplicado (idempotência).

#### Unit of Work
Agrupa operações de banco em uma transação atômica:
```csharp
await _unitOfWork.SaveChangesAsync(cancellationToken);
```

## Restrições e Bibliotecas

### ✅ Usar
- **LiteBus 4.2.0** (alternativa ao MediatR)
- **AwesomeAssertions 9.3.0** (para testes, alternativa ao FluentAssertions)
- **Implicit/Explicit Operators** (mapeamento de objetos)
- **xUnit** (framework de testes)
- **Testcontainers** (testes de integração com PostgreSQL e RabbitMQ)

### ❌ NÃO Usar
- **MediatR** (usar LiteBus)
- **AutoMapper** (usar operadores implícitos/explícitos)
- **FluentAssertions** (usar AwesomeAssertions)
- **MassTransit** (usar RabbitMQ.Client diretamente)

## Observabilidade

### Traces (OpenTelemetry)
- Propagação de contexto via header `traceparent` no RabbitMQ
- Export para Jaeger (OTLP endpoint)
- Instrumentação automática de ASP.NET Core, EF Core e HTTP

### Logs (Serilog)
- Logs estruturados em formato ECS (Elastic Common Schema)
- Export para OpenSearch
- Correlação com traces
- Sem PII (dados pessoais) nos logs

### Métricas (Prometheus)
- Endpoint `/metrics` em cada serviço
- Métricas por tipo de transação
- Health checks e métricas de infraestrutura

## Validações e Códigos de Erro

### Camadas de Validação
1. **DTOs**: Estrutura e formato básico
2. **Value Objects**: Regras de negócio de valores
3. **Aggregate Roots**: Regras de negócio de entidades
4. **UseCases**: Validações de contexto e orquestração

### Formato de Códigos de Erro
Usar `UPPER_SNAKE_CASE`:
- `EMAIL_REQUIRED`
- `ACCOUNT_NOT_FOUND`
- `INSUFFICIENT_BALANCE`
- `CLIENT_NAME_EMPTY`

## OpenSpec (Spec-Driven Development)

O projeto usa OpenSpec para gerenciar mudanças via proposals e specs.

### Quando Criar Proposal
- Adicionar features ou funcionalidades
- Mudanças breaking (API, schema)
- Mudanças arquiteturais
- Otimizações que alteram comportamento
- Mudanças de segurança

### Quando NÃO Criar Proposal
- Bug fixes (restaurar comportamento esperado)
- Typos, formatação, comentários
- Updates de dependências (não-breaking)
- Mudanças de configuração
- Testes para comportamento existente

### Comandos Úteis
```bash
# Listar mudanças ativas
openspec list

# Listar specs existentes
openspec list --specs

# Ver detalhes
openspec show <change-id|spec-id>

# Validar
openspec validate <change-id> --strict

# Arquivar após deployment
openspec archive <change-id> --yes
```

### Estrutura de Changes
```
openspec/changes/<change-id>/
├── proposal.md      # Justificativa e impacto
├── tasks.md         # Checklist de implementação
├── design.md        # Decisões técnicas (opcional)
└── specs/           # Delta changes por capability
    └── <capability>/
        └── spec.md  # ADDED/MODIFIED/REMOVED requirements
```

**IMPORTANTE**: Sempre consultar `@/openspec/AGENTS.md` e `openspec/project.md` antes de criar proposals.

## Convenções de Código

### C# e .NET
- **Target Framework**: net8.0
- **Nullable**: Habilitado em todos os projetos
- **ImplicitUsings**: Habilitado
- **SonarAnalyzer**: Habilitado em todos os projetos
- **ConfigureAwait(false)**: Sempre em código de biblioteca/infraestrutura

### Nomenclatura
- Interfaces: `I` prefix (ex: `IClientRepository`)
- Handlers: `<Action><Entity>Handler` (ex: `CreateClientHandler`)
- Workers: `<Purpose>Worker` ou `<Purpose>Receiver` (ex: `PixTransactionReceiver`)
- DTOs de mensagem: `<Entity><Purpose>DTO` (ex: `PixTransactionDTO`)

### Entity Framework
- Configurações explícitas em `IEntityTypeConfiguration<T>`
- Snake_case para nomes de tabelas e colunas (PostgreSQL)
- Índices em campos de busca (Email, AccountNumber, CPF, etc.)
- `DeleteBehavior.Restrict` em todas as foreign keys

## CI/CD

### GitHub Actions
Workflow `.github/workflows/ci.yml` com jobs:
- `build`: Restore e build da solução
- `unit-tests`: Testes unitários
- `integration-tests`: Testes de integração com compose profile `ci`
- `docker-build`: Build de imagens Docker dos serviços

### Container Engine
O projeto é agnóstico e suporta Docker ou Podman. Defina `CONTAINER_ENGINE` (padrão: `podman`).

## Configuração de Ambiente

### Variáveis Importantes
- `ConnectionStrings:DefaultConnection`: String de conexão PostgreSQL
- `RabbitMQ:Host`, `RabbitMQ:Port`, `RabbitMQ:Username`, `RabbitMQ:Password`
- `OpenTelemetry:Otlp:Endpoint`: Endpoint OTLP (ex: `http://localhost:4317`)
- `Serilog:OpenSearch:Uri`: URI do cluster OpenSearch

### User Secrets (Desenvolvimento Local)
Usar `dotnet user-secrets` para configurações sensíveis:
```bash
# Definir connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=mynewlittlebank;Username=postgres;Password=postgres"

# Listar todos os secrets configurados
dotnet user-secrets list --project src/API

# Remover um secret
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" --project src/API

# Limpar todos os secrets
dotnet user-secrets clear --project src/API
```

## Documentação Adicional

- `doc/code-style-guide.md`: Guia completo de estilo e padrões
- `doc/infra-and-tests.md`: Instruções de infraestrutura e testes
- `doc/observability.md`: Configuração de observabilidade
- `openspec/AGENTS.md`: Instruções OpenSpec detalhadas
- `AGENTS.md`: Referência para instruções OpenSpec
