namespace Domain.Entities;

public sealed class CardTransaction : Transaction
{
    private CardTransaction(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        string cardNumber,
        TransactionStatus status,
        DateTime occurredOn) : base(id, clientId, bankAccountId, amount, status, occurredOn)
    {
        CardNumber = cardNumber;
    }

    public string CardNumber { get; }

    public override TransactionType Type => TransactionType.Card;

    public static Result<CardTransaction> Create(
        TransactionId id,
        ClientId clientId,
        AccountNumber bankAccountId,
        Money amount,
        string cardNumber,
        TransactionStatus status = TransactionStatus.Pending,
        DateTime? occurredOn = null)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            return Result<CardTransaction>.Failure("card_number_empty");
        }

        if (amount < Money.Zero)
        {
            return Result<CardTransaction>.Failure("amount_negative");
        }

        return Result<CardTransaction>.Success(new CardTransaction(
            id,
            clientId,
            bankAccountId,
            amount,
            cardNumber.Trim(),
            status,
            occurredOn ?? DateTime.UtcNow));
    }
}
