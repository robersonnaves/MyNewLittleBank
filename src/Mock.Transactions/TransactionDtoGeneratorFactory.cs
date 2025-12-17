using System.Collections.Frozen;
using Bogus;
using Bogus.DataSets;
using Domain.DTOs;
using Domain.Enums;

namespace Mock.Transactions;

public sealed class TransactionDtoGeneratorFactory
{
    private readonly SeededAccountProvider _accountProvider;
    private readonly FrozenDictionary<string, Func<object>> _generators;

    public TransactionDtoGeneratorFactory(SeededAccountProvider accountProvider)
    {
        ArgumentNullException.ThrowIfNull(accountProvider);
        _accountProvider = accountProvider;

        var pixFaker = new Faker<PixTransactionDto>()
            .CustomInstantiator(faker =>
            {
                var origin = PickPixKey();
                var destination = PickPixKey(origin.PixKey);

                return new PixTransactionDto(
                    TransactionId: Guid.NewGuid(),
                    ClientId: origin.Account.ClientId,
                    AccountNumber: origin.Account.AccountNumber,
                    Amount: faker.Random.Decimal(1m, 2000m),
                    OriginPixKey: origin.PixKey,
                    DestinationPixKey: destination.PixKey,
                    Status: TransactionStatus.Pending,
                    OccurredOn: DateTime.UtcNow);
            });

        var moneyFaker = new Faker<MoneyTransactionDto>()
            .CustomInstantiator(faker =>
            {
                var account = PickAccount();
                return new MoneyTransactionDto(
                    TransactionId: Guid.NewGuid(),
                    ClientId: account.ClientId,
                    AccountNumber: account.AccountNumber,
                    Amount: faker.Random.Decimal(1m, 1500m),
                    Status: TransactionStatus.Pending,
                    OccurredOn: DateTime.UtcNow);
            });

        var cardFaker = new Faker<CardTransactionDto>()
            .CustomInstantiator(faker =>
            {
                var account = PickAccount();
                
                var cardNumber = GenerateCardNumber(faker);

                return new CardTransactionDto(
                    TransactionId: Guid.NewGuid(),
                    ClientId: account.ClientId,
                    AccountNumber: account.AccountNumber,
                    Amount: faker.Random.Decimal(1m, 1000m),
                    CardNumber: cardNumber,
                    Status: TransactionStatus.Pending,
                    OccurredOn: DateTime.UtcNow);
            });

        _generators = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pix"] = () => pixFaker.Generate(),
            ["money"] = () => moneyFaker.Generate(),
            ["card"] = () => cardFaker.Generate()
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string transactionType, out Func<object>? generator) =>
        _generators.TryGetValue(transactionType, out generator);

    public IEnumerable<string> AvailableTypes => _generators.Keys;

    private SeededAccount PickAccount()
    {
        var account = _accountProvider.Next();
        if (account is null)
        {
            throw new InvalidOperationException("No seeded accounts available to generate a transaction.");
        }

        return account;
    }

    private (SeededAccount Account, string PixKey) PickPixKey(string? excludePixKey = null)
    {
        return _accountProvider.NextPixKey(excludePixKey);
    }

    private static string GenerateCardNumber(Faker faker)
    {
        var cardNumber = faker.Finance.CreditCardNumber(CardType.Mastercard);
        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            cardNumber = $"5{faker.Random.Long(100000000000000, 999999999999999)}";
        }

        cardNumber = cardNumber.Replace(" ", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal);

        if (string.IsNullOrWhiteSpace(cardNumber))
        {
            throw new InvalidOperationException("Card number generation failed.");
        }

        return cardNumber;
    }
}
