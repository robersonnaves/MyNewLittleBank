using FluentAssertions;
using Domain.ValueObjects;

namespace Domain.Tests;

public class ValueObjectsTests
{
    [Fact]
    public void Cpf_ShouldFail_WhenChecksumInvalid()
    {
        var result = Cpf.TryCreate("12345678900");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("cpf_invalid_checksum");
    }

    [Fact]
    public void Cpf_ShouldSucceed_ForValidCpf()
    {
        var result = Cpf.TryCreate("52998224725");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("52998224725");
    }

    [Fact]
    public void Money_ShouldFail_WhenNegative()
    {
        var result = Money.TryCreate(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("money_negative");
    }

    [Fact]
    public void Money_ShouldSupportArithmetic()
    {
        var left = Money.TryCreate(10m).Value;
        var right = Money.TryCreate(2.5m).Value;

        (left + right).Value.Should().Be(12.5m);
        (left - right).Value.Should().Be(7.5m);
    }
}
