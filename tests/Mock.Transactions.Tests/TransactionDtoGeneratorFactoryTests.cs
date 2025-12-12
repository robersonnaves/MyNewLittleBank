using Domain.DTOs;
using Domain.Enums;

namespace Mock.Transactions.Tests;

public sealed class TransactionDtoGeneratorFactoryTests
{
    [Fact]
    public void PixGeneratorUsesSeededPixKeys()
    {
        var clientId = Guid.NewGuid();
        var accountNumber = "123456";
        var pixKeys = new[]
        {
            "12345678901", // cpf
            "+5511981234567", // phone
            "pix-key-1@example.com", // email
            Guid.NewGuid().ToString("D") // random guid
        };

        var provider = new SeededAccountProvider();
        provider.SetAccounts(new List<SeededAccount>
        {
            new(clientId, accountNumber, pixKeys)
        });

        var factory = new TransactionDtoGeneratorFactory(provider);

        factory.TryGet("pix", out var generator).Should().BeTrue();
        generator.Should().NotBeNull();

        var dto = (PixTransactionDto)generator!();

        dto.ClientId.Should().Be(clientId);
        dto.AccountNumber.Should().Be(accountNumber);
        dto.OriginPixKey.Should().BeOneOf(pixKeys);
        dto.DestinationPixKey.Should().BeOneOf(pixKeys);
        dto.Status.Should().Be(TransactionStatus.Pending);
    }

    [Fact]
    public void PixGeneratorThrowsWhenOnlyEmptyPixKeys()
    {
        var clientId = Guid.NewGuid();
        var accountNumber = "999999";

        var provider = new SeededAccountProvider();
        provider.SetAccounts(new List<SeededAccount>
        {
            new(clientId, accountNumber, new List<string> { "", " ", "\t" })
        });

        var factory = new TransactionDtoGeneratorFactory(provider);

        factory.TryGet("pix", out var generator).Should().BeTrue();
        generator.Should().NotBeNull();

        Action action = () => generator!();

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("No Pix keys available for generation.");
    }

    [Fact]
    public void CardGeneratorProducesNonEmptyCardNumber()
    {
        var provider = new SeededAccountProvider();
        provider.SetAccounts(new List<SeededAccount>
        {
            new(Guid.NewGuid(), "123456", new List<string> { "any" })
        });

        var factory = new TransactionDtoGeneratorFactory(provider);

        factory.TryGet("card", out var generator).Should().BeTrue();
        generator.Should().NotBeNull();

        var dto = (CardTransactionDto)generator!();

        dto.CardNumber.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PixGeneratorFailsWhenNoPixKeysAreAvailable()
    {
        var provider = new SeededAccountProvider();
        provider.SetAccounts(new List<SeededAccount>
        {
            new(Guid.NewGuid(), "999999", Array.Empty<string>())
        });

        var factory = new TransactionDtoGeneratorFactory(provider);

        factory.TryGet("pix", out var generator).Should().BeTrue();
        generator.Should().NotBeNull();

        Action action = () => generator!();

        action.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("No Pix keys available for generation.");
    }

    [Fact]
    public void GeneratorsAlwaysAssignTransactionId()
    {
        var clientId = Guid.NewGuid();
        var accountNumber = "555555";
        var pixKeys = new[] { "pix@example.com" };

        var provider = new SeededAccountProvider();
        provider.SetAccounts(new List<SeededAccount>
        {
            new(clientId, accountNumber, pixKeys)
        });

        var factory = new TransactionDtoGeneratorFactory(provider);

        foreach (var type in new[] { "pix", "money", "card" })
        {
            factory.TryGet(type, out var generator).Should().BeTrue();
            generator.Should().NotBeNull();

            var dto = generator!();
            var transactionId = dto.GetType().GetProperty("TransactionId")?.GetValue(dto) as Guid?;
            transactionId.Should().NotBeNull();
            transactionId.Should().NotBe(Guid.Empty);
        }
    }
}
