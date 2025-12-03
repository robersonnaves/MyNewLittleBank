namespace Domain.Tests;

public class ValueObjectsTests
{
    [Fact]
    public void CpfShouldFailWhenChecksumInvalid()
    {
        var result = Cpf.TryCreate("12345678900");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("cpf_invalid_checksum");
    }

    [Fact]
    public void CpfShouldSucceedForValidCpf()
    {
        var result = Cpf.TryCreate("52998224725");

        result.IsSuccess.Should().BeTrue();
        result.Value.Value.Should().Be("52998224725");
    }

    [Fact]
    public void MoneyShouldFailWhenNegative()
    {
        var result = Money.TryCreate(-1m);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("money_negative");
    }

    [Fact]
    public void MoneyShouldSupportArithmetic()
    {
        var leftResult = Money.TryCreate(10m);
        var rightResult = Money.TryCreate(2.5m);

        leftResult.IsSuccess.Should().BeTrue();
        rightResult.IsSuccess.Should().BeTrue();

        var left = leftResult.Value!;
        var right = rightResult.Value!;

        (left + right).Value.Should().Be(12.5m);
        (left - right).Value.Should().Be(7.5m);
    }
}
