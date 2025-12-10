using System.Collections.Frozen;
using Bogus;
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
                var account = PickAccount();
                return new PixTransactionDto(
                    TransactionId: Guid.NewGuid(),
                    ClientId: account.ClientId,
                    AccountNumber: account.AccountNumber,
                    Amount: faker.Random.Decimal(1m, 2000m),
                    OriginPixKey: $"{faker.Internet.Email()}",
                    DestinationPixKey: $"{faker.Internet.Email()}",
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
                return new CardTransactionDto(
                    TransactionId: Guid.NewGuid(),
                    ClientId: account.ClientId,
                    AccountNumber: account.AccountNumber,
                    Amount: faker.Random.Decimal(1m, 1000m),
                    CardNumber: faker.Finance.CreditCardNumber(),
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

    private SeededAccount PickAccount()
    {
        var account = _accountProvider.Next();
        if (account is null)
        {
            throw new InvalidOperationException("No seeded accounts available to generate a transaction.");
        }

        return account;
    }
}
