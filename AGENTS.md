<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# Build & Test Commands

```bash
# Build all projects
dotnet build

# Run all tests
dotnet test

# Run single test class
dotnet test --filter "FullyQualifiedName~TestClassName"

# Run single test method
dotnet test --filter "FullyQualifiedName~TestMethodName"

# Run tests with detailed output
dotnet test --logger "console;verbosity=detailed"

# Build specific project
dotnet build src/Domain/Domain.csproj

# Test specific project
dotnet test tests/Domain.Tests/Domain.Tests.csproj
```

# Code Style Guidelines

## Architecture
- Clean Architecture with DDD: `Services → UseCases → Domain ← Infrastructure`
- .NET 8.0 with nullable reference types enabled
- Target Framework: net8.0

## Naming Conventions
- PascalCase: Classes, Interfaces, Methods, Properties
- camelCase: parameters, variables
- _camelCase: private fields
- Prefix `I` for interfaces
- Suffix `Async` for async methods
- snake_case: database tables/columns

## Code Style
- File-scoped namespaces
- Explicit access modifiers: `public`, `private`, `sealed`, `readonly`
- Global usings in `GlobalUsings.cs` files
- Async/await with `CancellationToken` throughout
- Result pattern for domain operations
- Factory methods `TryCreate()` return `Result<T>`

## Testing
- xUnit framework with AwesomeAssertions (NOT FluentAssertions)
- Test naming: `MethodName_Scenario_ExpectedResult`
- Arrange-Act-Assert pattern
- Use `Should().BeTrue()`, `Should().BeFalse()` etc.

## Dependencies
- Entity Framework Core 9.0.11 with PostgreSQL
- RabbitMQ.Client 7.x (use async methods: `CreateChannelAsync`, `BasicPublishAsync`)
- OpenTelemetry for observability
- Serilog for structured logging
- No MediatR, AutoMapper, FluentAssertions, or MassTransit

## Key Patterns
- Value Objects: immutable, `TryCreate()` factory, record struct where appropriate
- Aggregates: private constructors, factory methods, domain events
- Result<T> pattern for error handling
- Outbox/Inbox patterns for reliable messaging
- Unit of Work for database transactions

## Configuration
- Central package management in `Directory.Packages.props`
- Global properties in `Directory.Build.props`
- Analysis level: latest-all with SonarAnalyzer.CSharp