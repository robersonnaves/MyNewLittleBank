# Error Handling Strategy

## Overview

This document describes the error handling strategy for the MyNewLittleBank transaction processing system. The system distinguishes between **business errors** and **technical errors** to ensure proper message acknowledgment and retry behavior in the event-driven architecture.

---

## Business Errors vs Technical Errors

### Business Errors (No Retry)

**Definition:** Errors caused by business rule violations or invalid input data. These are expected behaviors and are part of normal system operation.

**Characteristics:**
- Caused by invalid user input or business rule violations
- Not transient - retrying will produce the same result
- Should be logged and acknowledged without retry
- May trigger user notifications (e.g., insufficient funds)

**Complete List of Business Errors:**

| Error Code | Description | Origin | User Notification |
|-----------|-------------|--------|------------------|
| `insufficient_funds` | Account balance insufficient for debit operation | `Domain.Entities.BankAccount:64` | ✅ Yes |
| `bank_account_not_found` | Bank account does not exist in database | `UseCases.Transactions.ProcessTransactionsHandler:122` | ❌ No |
| `client_not_found` | Client does not exist in database | `UseCases.Clients.*Handler` | ❌ No |
| `invalid_cpf` | CPF format is invalid | `Domain.Common.DomainException:30` | ❌ No |
| `invalid_account_number` | Account number format is invalid | `Domain.Common.DomainException:33` | ❌ No |
| `invalid_money` | Monetary value is invalid (e.g., negative) | `Domain.Common.DomainException:36` | ❌ No |
| `invalid_transaction` | Transaction data is invalid | `Domain.Common.DomainException:42` | ❌ No |

**Handling:**
- Log at `Warning` level with EventId=1 (`BusinessErrorOccurred`)
- Return without throwing exception
- Message is acknowledged (no retry in RabbitMQ)
- User notification triggered for specific errors (`insufficient_funds`)

---

### Technical Errors (With Retry)

**Definition:** Errors caused by infrastructure or transient failures. These may be resolved by retrying the operation.

**Characteristics:**
- Caused by infrastructure issues (database, network, external services)
- Transient - may succeed on retry
- Should be logged and thrown to trigger retry mechanism
- After max retries, message goes to Dead Letter Queue (DLQ)

**Examples of Technical Errors:**

| Error Type | Example | Transient? | Max Retries |
|-----------|---------|------------|-------------|
| Database connection | `database_connection_error` | ✅ Yes | 3 |
| Network timeout | `network_timeout` | ✅ Yes | 3 |
| Service unavailable | `service_unavailable` | ✅ Yes | 3 |
| External API failure | `external_api_error` | ✅ Yes | 3 |

**Handling:**
- Log at `Error` level with EventId=5 (`TechnicalErrorOccurred`)
- Throw `InvalidOperationException` with error message
- Message is NOT acknowledged (triggers retry in RabbitMQ)
- After max retries, message moves to Dead Letter Queue

---

## Implementation

### Transaction Receivers (Card, Money, Pix)

All three transaction receivers (`CardTransactionReceiver`, `MoneyTransactionReceiver`, `PixTransactionReceiver`) implement the same error handling pattern:

```csharp
// Business errors that should not trigger message retry
private static readonly HashSet<string> BusinessErrors = new()
{
    "bank_account_not_found",
    "insufficient_funds",
    "client_not_found",
    "invalid_cpf",
    "invalid_account_number",
    "invalid_money",
    "invalid_transaction"
};

public async Task HandleAsync(MessageEnvelope envelope, CancellationToken cancellationToken)
{
    // ... deserialization and validation ...

    var result = await _handler.HandleAsync(message, cancellationToken).ConfigureAwait(false);
    
    if (result.IsFailure)
    {
        // Business errors - acknowledge without retry
        if (BusinessErrors.Contains(result.Error!))
        {
            BusinessErrorOccurred(_logger, result.Error!, null);
            return; // Message acknowledged, no retry
        }
        
        // Technical errors - throw for retry
        TechnicalErrorOccurred(_logger, result.Error!, null);
        throw new InvalidOperationException(result.Error);
    }
}
```

### Logging

**Business Error Logger:**
```csharp
private static readonly Action<ILogger, string, Exception?> BusinessErrorOccurred =
    LoggerMessage.Define<string>(
        LogLevel.Warning,
        new EventId(1, nameof(BusinessErrorOccurred)),
        "{ServiceName} transaction rejected due to business rule: {Error}");
```

**Technical Error Logger:**
```csharp
private static readonly Action<ILogger, string, Exception?> TechnicalErrorOccurred =
    LoggerMessage.Define<string>(
        LogLevel.Error,
        new EventId(5, nameof(TechnicalErrorOccurred)),
        "{ServiceName} transaction processing failed due to technical error: {Error}");
```

---

## Testing

Each transaction receiver has two test scenarios to validate error handling:

### Test 1: Business Errors
**Test Name:** `HandleAsync_WithBusinessError_Should_LogProcessingError_And_AcknowledgeMessage`

**Purpose:** Verify that business errors are handled gracefully without throwing exceptions.

**Test Coverage:**
```csharp
[Theory]
[InlineData("insufficient_funds")]
[InlineData("bank_account_not_found")]
[InlineData("client_not_found")]
[InlineData("invalid_cpf")]
[InlineData("invalid_account_number")]
[InlineData("invalid_money")]
[InlineData("invalid_transaction")]
public async Task HandleAsync_WithBusinessError_Should_LogProcessingError_And_AcknowledgeMessage(string errorMessage)
{
    // Arrange: Setup handler to return business error
    // Act: Process message
    // Assert:
    //   - Method completes without throwing
    //   - Handler called exactly once
    //   - Warning log (EventId=1) recorded
    //   - Message acknowledged
}
```

### Test 2: Technical Errors
**Test Name:** `HandleAsync_WithTechnicalError_Should_LogProcessingError_And_ThrowException`

**Purpose:** Verify that technical errors trigger retry mechanism by throwing exceptions.

**Test Coverage:**
```csharp
[Fact]
public async Task HandleAsync_WithTechnicalError_Should_LogProcessingError_And_ThrowException()
{
    // Arrange: Setup handler to return technical error
    // Act & Assert:
    //   - InvalidOperationException thrown
    //   - Exception message matches error
    //   - Error log (EventId=5) recorded
    //   - Handler called exactly once
}
```

---

## Message Flow

### Business Error Flow

```
┌─────────────────┐
│ Transaction DTO │
│  received from  │
│   RabbitMQ      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Deserialize   │
│    & Validate   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Process with   │
│     Handler     │
└────────┬────────┘
         │
         ▼
    ┌────────┐
    │ Error? │
    └───┬────┘
        │ Yes
        ▼
   ┌─────────┐
   │Business?│
   └────┬────┘
        │ Yes
        ▼
┌──────────────────┐
│ Log Warning (1)  │
│ Acknowledge Msg  │
│   NO RETRY ✓     │
└──────────────────┘
```

### Technical Error Flow

```
┌─────────────────┐
│ Transaction DTO │
│  received from  │
│   RabbitMQ      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Deserialize   │
│    & Validate   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Process with   │
│     Handler     │
└────────┬────────┘
         │
         ▼
    ┌────────┐
    │ Error? │
    └───┬────┘
        │ Yes
        ▼
   ┌──────────┐
   │Technical?│
   └────┬─────┘
        │ Yes
        ▼
┌──────────────────┐
│  Log Error (5)   │
│ Throw Exception  │
│  Message Retry   │
└────────┬─────────┘
         │
         ▼ (after 3 retries)
┌──────────────────┐
│ Dead Letter Queue│
│   (DLQ) ⚠️       │
└──────────────────┘
```

---

## Observability

### Metrics to Monitor

**Business Errors:**
- Count of each business error type per hour
- Top 5 most frequent business errors
- Client accounts with high insufficient_funds rate (fraud detection)
- Trend analysis of business errors over time

**Technical Errors:**
- Count of technical errors per hour
- Retry success rate
- Messages sent to DLQ
- Average time to recovery after technical error

### Log Queries

**Query business errors (last 24h):**
```kusto
logs
| where EventId == 1
| where TimeGenerated > ago(24h)
| summarize count() by Error
| order by count_ desc
```

**Query technical errors requiring attention:**
```kusto
logs
| where EventId == 5
| where TimeGenerated > ago(1h)
| project TimeGenerated, ServiceName, Error, TraceId
```

---

## Best Practices

### Adding New Business Errors

1. **Define the error in `Domain.Common.DomainException`:**
   ```csharp
   public static DomainException MyNewBusinessError(string context) =>
       new("my_new_business_error", $"Description: {context}");
   ```

2. **Add to BusinessErrors HashSet in all three receivers:**
   ```csharp
   private static readonly HashSet<string> BusinessErrors = new()
   {
       // ... existing errors ...
       "my_new_business_error"
   };
   ```

3. **Add test cases:**
   ```csharp
   [InlineData("my_new_business_error")]
   ```

4. **Update this documentation** with the new error code

### Adding New Technical Error Scenarios

Technical errors are any errors NOT in the BusinessErrors list. No code changes needed - they automatically trigger retry mechanism.

### Notification Integration

Some business errors trigger user notifications:

- **insufficient_funds**: Triggers `NotifyInsufficientFundsAsync()` in `ProcessTransactionsHandler:131`
- **Other errors**: Currently do not trigger notifications (future enhancement)

See `UseCases/Transactions/ProcessTransactionsHandler.cs:151-170` for notification implementation.

---

## References

- **Implementation:** 
  - `src/Services.Card/CardTransactionReceiver.cs`
  - `src/Services.Money/MoneyTransactionReceiver.cs`
  - `src/Services.Pix/PixTransactionReceiver.cs`

- **Tests:**
  - `tests/Services.Card.Tests/CardTransactionReceiverTests.cs`
  - `tests/Services.Money.Tests/MoneyTransactionReceiverTests.cs`
  - `tests/Services.Pix.Tests/PixTransactionReceiverTests.cs`

- **Domain Errors:** 
  - `src/Domain/Common/DomainException.cs`
  - `src/Domain/Common/Result.cs`

- **Handler:** 
  - `src/UseCases/Transactions/ProcessTransactionsHandler.cs`

---

## Revision History

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-20 | 1.0 | Initial documentation of error handling strategy | AI Assistant |

---

**Last Updated:** 2025-12-20  
**Maintained By:** Development Team
