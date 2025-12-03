namespace Domain.ValueObjects;

public readonly record struct TransactionId(Guid Value)
{
    public static Result<TransactionId> New() => Result<TransactionId>.Success(new TransactionId(Guid.NewGuid()));

    public static Result<TransactionId> TryCreate(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result<TransactionId>.Failure("transaction_id_empty");
        }

        return Result<TransactionId>.Success(new TransactionId(value));
    }

    public override string ToString() => Value.ToString();
}
