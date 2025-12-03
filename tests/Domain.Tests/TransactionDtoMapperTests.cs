using AwesomeAssertions;
using Domain.DTOs;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests;

public class TransactionDtoMapperTests
{
    [Fact]
    public void PixTransactionDtoRoundTripShouldPreserveData()
    {
        var id = TransactionId.New().Value!;

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        clientIdResult.IsSuccess.Should().BeTrue();
        var clientId = clientIdResult.Value!;

        var accountNumberResult = AccountNumber.TryCreate("123456");
        accountNumberResult.IsSuccess.Should().BeTrue();
        var accountNumber = accountNumberResult.Value!;

        var moneyResult = Money.TryCreate(42.5m);
        moneyResult.IsSuccess.Should().BeTrue();
        var money = moneyResult.Value!;

        var pixResult = PixTransaction.Create(
            id,
            clientId,
            accountNumber,
            money,
            originPixKey: "orig-key",
            destinationPixKey: "dest-key",
            status: TransactionStatus.Completed,
            occurredOn: new DateTime(2024, 01, 01, 12, 0, 0, DateTimeKind.Utc));

        pixResult.IsSuccess.Should().BeTrue();
        var dto = TransactionDtoMapper.ToDto(pixResult.Value!);
        var entityResult = TransactionDtoMapper.ToEntity(dto);

        entityResult.IsSuccess.Should().BeTrue();
        var entity = entityResult.Value!;
        entity.Id.Should().Be(id);
        entity.ClientId.Should().Be(clientId);
        entity.BankAccountId.Should().Be(accountNumber);
        entity.Amount.Should().Be(money);
        entity.Status.Should().Be(TransactionStatus.Completed);
        entity.OccurredOn.Should().Be(new DateTime(2024, 01, 01, 12, 0, 0, DateTimeKind.Utc));
        entity.OriginPixKey.Should().Be("orig-key");
        entity.DestinationPixKey.Should().Be("dest-key");
    }
}
