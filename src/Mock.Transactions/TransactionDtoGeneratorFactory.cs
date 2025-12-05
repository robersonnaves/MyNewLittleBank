using System.Collections.Frozen;
using Bogus;
using Domain.DTOs;
using Domain.Enums;

namespace Mock.Transactions;

public sealed class TransactionDtoGeneratorFactory
{
    private readonly FrozenDictionary<string, Func<object>> _generators;

    public TransactionDtoGeneratorFactory()
    {
        var pixFaker = new Faker<PixTransactionDto>()
            .CustomInstantiator(faker => new PixTransactionDto(
                TransactionId: Guid.NewGuid(),
                ClientId: Guid.NewGuid(),
                AccountNumber: faker.Random.ReplaceNumbers("########"),
                Amount: faker.Random.Decimal(1m, 2000m),
                OriginPixKey: $"{faker.Internet.Email()}",
                DestinationPixKey: $"{faker.Internet.Email()}",
                Status: TransactionStatus.Pending,
                OccurredOn: DateTime.UtcNow));

        var moneyFaker = new Faker<MoneyTransactionDto>()
            .CustomInstantiator(faker => new MoneyTransactionDto(
                TransactionId: Guid.NewGuid(),
                ClientId: Guid.NewGuid(),
                AccountNumber: faker.Random.ReplaceNumbers("########"),
                Amount: faker.Random.Decimal(1m, 1500m),
                Status: TransactionStatus.Pending,
                OccurredOn: DateTime.UtcNow));

        var cardFaker = new Faker<CardTransactionDto>()
            .CustomInstantiator(faker => new CardTransactionDto(
                TransactionId: Guid.NewGuid(),
                ClientId: Guid.NewGuid(),
                AccountNumber: faker.Random.ReplaceNumbers("########"),
                Amount: faker.Random.Decimal(1m, 1000m),
                CardNumber: faker.Finance.CreditCardNumber(),
                Status: TransactionStatus.Pending,
                OccurredOn: DateTime.UtcNow));

        _generators = new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase)
        {
            ["pix"] = () => pixFaker.Generate(),
            ["money"] = () => moneyFaker.Generate(),
            ["card"] = () => cardFaker.Generate()
        }.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
    }

    public bool TryGet(string transactionType, out Func<object>? generator) =>
        _generators.TryGetValue(transactionType, out generator);
}
