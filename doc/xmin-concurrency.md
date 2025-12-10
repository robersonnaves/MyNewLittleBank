# PostgreSQL xmin Concurrency Control

## Overview

This project uses PostgreSQL's built-in `xmin` system column for optimistic concurrency control instead of a custom `row_version` column.

## What is xmin?

`xmin` is a hidden system column present in every PostgreSQL table that stores the transaction ID (XID) of the inserting transaction. PostgreSQL automatically maintains this column, and it gets updated on every row modification.

## Benefits

1. **Native PostgreSQL Solution** - Zero configuration, automatic management
2. **High Performance** - Uses PostgreSQL's internal transaction management
3. **Zero Overhead** - No triggers, no application logic needed
4. **Battle-Tested** - Used by thousands of production PostgreSQL applications

## Implementation Details

### Domain Entities

Three entities use `xmin` for concurrency:
- `Client` (`/src/Domain/Entities/Client.cs`)
- `BankAccount` (`/src/Domain/Entities/BankAccount.cs`)
- `Transaction` (`/src/Domain/Entities/Transaction.cs`)

Each entity has:
```csharp
public uint Xmin { get; private set; }
```

### EF Core Configuration

In each configuration file (`ClientConfiguration.cs`, `BankAccountConfiguration.cs`, `TransactionConfiguration.cs`):

```csharp
builder.Property(e => e.Xmin)
    .HasColumnName("xmin")
    .HasColumnType("xid")
    .IsRowVersion()
    .IsConcurrencyToken()
    .ValueGeneratedOnAddOrUpdate();
```

### Database Schema

No special column needed - `xmin` is a system column automatically present in all PostgreSQL tables:

```sql
CREATE TABLE clients (
    id UUID PRIMARY KEY,
    cpf VARCHAR(11) NOT NULL,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(320) NOT NULL,
    mobile_number VARCHAR(32) NOT NULL
    -- xmin column is implicit, no need to define it
);
```

## How It Works

1. **Insert**: PostgreSQL assigns current transaction ID to `xmin`
2. **Read**: EF Core reads the `xmin` value along with other columns
3. **Update**: EF Core includes `xmin` in the WHERE clause:
   ```sql
   UPDATE clients 
   SET name = @p0, email = @p1 
   WHERE id = @p2 AND xmin = @p3;
   ```
4. **Conflict Detection**: If another transaction modified the row, `xmin` changes and the UPDATE affects 0 rows, triggering `DbUpdateConcurrencyException`

## Testing

### Unit Tests
InMemory database doesn't support `xmin`, so it will always be 0 in unit tests. This is expected and documented in test code.

### Integration Tests
With real PostgreSQL, `xmin` values are automatically populated with transaction IDs (e.g., 771, 772, etc.).

### Manual Testing
```bash
# Query to see xmin values
PGPASSWORD=S3gr3d0123 psql -h localhost -U postgres -d mynewlittlebank \
  -c "SELECT id, name, xmin FROM clients;"
```

## Important Notes

1. **PostgreSQL-Only**: This solution is specific to PostgreSQL and won't work with SQL Server, MySQL, etc.
2. **Transaction Wraparound**: Theoretically possible after ~4 billion transactions, but PostgreSQL handles this automatically through VACUUM
3. **Type**: `xmin` is a `uint` (unsigned 32-bit integer), representing transaction IDs
4. **Not Human-Readable**: Unlike timestamps, `xmin` values are transaction IDs with no inherent meaning to users

## Migration from row_version

If migrating from an existing `row_version` column:

1. Update domain entities: Replace `byte[] RowVersion` with `uint Xmin`
2. Update EF Core configurations: Map to `xmin` system column
3. Drop database and recreate tables (manual process per project guidelines)
4. Update tests: Adjust assertions to use `Xmin` property

## References

- [PostgreSQL System Columns](https://www.postgresql.org/docs/current/ddl-system-columns.html)
- [Npgsql Concurrency Tokens](https://www.npgsql.org/efcore/mapping/general.html#concurrency-tokens)
- [EF Core Concurrency Tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency)
