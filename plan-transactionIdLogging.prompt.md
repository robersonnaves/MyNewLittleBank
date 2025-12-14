Title: Include transaction_id in publish log using LoggerMessage

Goal
- Show transaction_id in the publish log line in MockTransactionsWorker using performant, type-safe patterns.

Guiding Principles
- Prefer LoggerMessage.Define delegates for structured logging performance.
- Avoid reflection and JSON round-trips for extracting TransactionId.
- Keep current behavior and validations intact, minimizing changes.

Plan
1) Add a new LoggerMessage delegate
- Field name: _publishedWithId
- Signature: Action<ILogger, string, string, Guid, Exception?>
- Template: "Published mock transaction of type {Type} to {RoutingKey} with transaction_id {TransactionId}"
- EventId: 3 (Information) to distinguish from existing _published (EventId 2)

2) Extract transactionId type-safely
- Immediately after dto generation succeeds, extract TransactionId via type pattern matching:
  - if (dto is PixTransactionDto p) var transactionId = p.TransactionId;
  - else if (dto is CardTransactionDto c) var transactionId = c.TransactionId;
  - else if (dto is MoneyTransactionDto m) var transactionId = m.TransactionId;
  - else: log error and continue (unsupported DTO).
- Validate transactionId != Guid.Empty before publish/logging.

3) Use the new delegate when publishing
- After successful publish: _publishedWithId(_logger, effectiveType, routingKey, transactionId, null)
- Retain existing _published delegate for backward compatibility if needed, or replace its usage entirely.

4) Simplify/align validation
- Prefer the type-safe check over reflection-based TransactionId validation.
- Optionally keep one JSON validation (defense-in-depth) but avoid duplicate parsing.
- Ensure error logs remain structured and informative.

5) Naming consistency
- Use TransactionId in the log template, or switch to transaction_id if observability expects snake_case. Decide based on sink conventions; default to TransactionId for C# consistency.

6) Non-functional considerations
- Performance: LoggerMessage with value types (Guid) is efficient and avoids boxing on structured logging.
- Maintainability: Clear event separation using EventId 3; comment near definitions for intent.

Acceptance Criteria
- Build and tests pass.
- The publish log line includes the correct transaction_id for Pix/Card/Money DTOs.
- No reflection or extra JSON parsing is required to obtain TransactionId.
- Structured properties appear in telemetry with the chosen property name (TransactionId or transaction_id).
