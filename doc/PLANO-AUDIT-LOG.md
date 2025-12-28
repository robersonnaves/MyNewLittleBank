# Plano de Implementação: Audit Log (EF Core Interceptor)

Este documento descreve a estratégia para implementar o rastreamento de dados (Audit Log) no `MyNewLittleBank` utilizando interceptadores do Entity Framework Core, em conformidade com o `doc/code-style-guide.md`.

## 1. Objetivo

Capturar automaticamente as alterações (Inserção, Atualização, Exclusão) nas entidades de domínio e persistir o histórico ("Antes" e "Depois") em uma tabela `audit_logs` no PostgreSQL, garantindo consistência transacional e correlação com a stack de observabilidade.

## 2. Arquitetura e Componentes

A implementação será contida inteiramente na camada `Infra.Database`, pois trata-se de um mecanismo de persistência e infraestrutura.

### 2.0. Interface de Marcação: `IAuditable`

**Local:** `src/Domain/Common/IAuditable.cs`

Uma interface vazia (marker interface) para indicar explicitamente quais entidades do domínio devem ser rastreadas. Isso evita auditar tabelas técnicas ou irrelevantes e desacopla a lógica de auditoria da herança de classes.

### 2.1. Nova Entidade: `AuditLog`

**Local:** `src/Infra.Database/Entities/AuditLog.cs`

Deve ser uma classe POCO seguindo as convenções do projeto (PascalCase, propriedades públicas).

**Propriedades:**
- `Id` (Guid, PK)
- `EntityName` (string, MaxLength 100)
- `EntityId` (string, MaxLength 100) - Representação string da PK da entidade auditada.
- `Action` (string, MaxLength 20) - "Added", "Modified", "Deleted".
- `OldValues` (string?, JSONB) - Snapshot dos valores anteriores.
- `NewValues` (string?, JSONB) - Snapshot dos novos valores.
- `OccurredOn` (DateTime) - UTC timestamp.
- `TraceId` (string?, MaxLength 100) - Para correlação com OpenTelemetry (`Activity.Current.TraceId`).
- `SpanId` (string?, MaxLength 100) - Para correlação fina (`Activity.Current.SpanId`).

### 2.2. Configuração do EF Core: `AuditLogConfiguration`

**Local:** `src/Infra.Database/Configurations/AuditLogConfiguration.cs`

Implementar `IEntityTypeConfiguration<AuditLog>`:
- Definir nome da tabela: `audit_logs` (snake_case).
- Configurar colunas `OldValues` e `NewValues` com tipo `jsonb` (PostgreSQL).
- Definir índices se necessário (ex: por `EntityId` ou `TraceId`).

### 2.3. Interceptor: `AuditInterceptor`

**Local:** `src/Infra.Database/Interceptors/AuditInterceptor.cs`

Herdar de `SaveChangesInterceptor`.

**Lógica no `SavingChangesAsync`:**
1. Iterar sobre `eventData.Context.ChangeTracker.Entries()`.
2. Filtrar estados: `Added`, `Modified`, `Deleted`.
3. Verificar se a entidade implementa `IAuditable`. Se não implementar, ignorar a entrada.
4. Para cada entrada:
    - Extrair PK (lidar com chaves compostas se houver, ou assumir `Id`).
    - Serializar `OriginalValues` (para Deleted/Modified) e `CurrentValues` (para Added/Modified) usando `System.Text.Json`.
    - Capturar `Activity.Current` para preencher `TraceId` e `SpanId`.
5. Criar instâncias de `AuditLog` e adicionar ao `Context` antes do `base.SavingChangesAsync`.

### 2.4. Integração no DbContext

**Local:** `src/Infra.Database/MyNewLittleBankContext.cs`

- Adicionar `DbSet<AuditLog> AuditLogs { get; set; }`.
- Registrar o interceptor no método `OnConfiguring` ou via injeção de dependência na configuração do serviço.

## 3. Plano de Execução (Tasks)

### Passo 1: Criação da Entidade e Configuração
- [ ] Criar `src/Domain/Common/IAuditable.cs`.
- [ ] Criar `src/Infra.Database/Entities/AuditLog.cs`.
- [ ] Criar `src/Infra.Database/Configurations/AuditLogConfiguration.cs`.
- [ ] Garantir que `MyNewLittleBankContext` aplique as configurações do assembly (`ApplyConfigurationsFromAssembly`).

### Passo 2: Implementação do Interceptor
- [ ] Criar `src/Infra.Database/Interceptors/AuditInterceptor.cs`.
- [ ] Implementar lógica de extração de mudanças filtrando por `IAuditable`.
- [ ] Implementar serialização JSON segura (evitar referência circular).
- [ ] Integrar com `System.Diagnostics.Activity` para tracing.

### Passo 3: Registro e Migração
- [ ] Registrar `AuditInterceptor` na injeção de dependência (`DatabaseServiceCollectionExtensions`).
- [ ] Configurar `AddDbContext` para usar o interceptor (`AddInterceptors`).
- [ ] Adicionar `DbSet<AuditLog>` no contexto.
- [ ] Criar migration: `dotnet ef migrations add AddAuditLog --project src/Infra.Database`.
- [ ] Revisar script SQL gerado (garantir `jsonb`).

### Passo 4: Testes
- [ ] Criar testes de integração em `tests/Infra.Database.Tests`.
- [ ] Cenário: Inserir `Client` -> Verificar `audit_logs` com Action "Added".
- [ ] Cenário: Atualizar `BankAccount` -> Verificar `audit_logs` com Action "Modified" e valores antigos/novos.
- [ ] Cenário: Rollback de transação -> Verificar que `audit_logs` não persistiu nada (garantia ACID).

## 4. Exemplo de Código (Referência)

```csharp
// src/Infra.Database/Configurations/AuditLogConfiguration.cs
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OldValues).HasColumnType("jsonb");
        builder.Property(x => x.NewValues).HasColumnType("jsonb");
    }
}
```