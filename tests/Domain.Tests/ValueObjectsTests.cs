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

    [Fact]
    public void Money_Should_Support_Comparison_Operations()
    {
        // Arrange
        var money10Result = Money.TryCreate(10m);
        money10Result.IsSuccess.Should().BeTrue();
        var money10 = money10Result.Value!;

        var money20Result = Money.TryCreate(20m);
        money20Result.IsSuccess.Should().BeTrue();
        var money20 = money20Result.Value!;

        var money10DuplicateResult = Money.TryCreate(10m);
        money10DuplicateResult.IsSuccess.Should().BeTrue();
        var money10Duplicate = money10DuplicateResult.Value!;

        // Act & Assert
        (money10 < money20).Should().BeTrue();
        (money20 > money10).Should().BeTrue();
        (money10 <= money20).Should().BeTrue();
        (money20 >= money10).Should().BeTrue();
        (money10 <= money10Duplicate).Should().BeTrue();
        (money10 >= money10Duplicate).Should().BeTrue();
        (money10 == money10Duplicate).Should().BeTrue();
        (money10 != money20).Should().BeTrue();
    }

    [Fact]
    public void Money_Should_Support_Add_And_Subtract_Methods()
    {
        // Arrange
        var money15Result = Money.TryCreate(15m);
        money15Result.IsSuccess.Should().BeTrue();
        var money15 = money15Result.Value!;

        var money5Result = Money.TryCreate(5m);
        money5Result.IsSuccess.Should().BeTrue();
        var money5 = money5Result.Value!;

        // Act
        var addResult = money15.Add(money5);
        var subtractResult = money15.Subtract(money5);

        // Assert
        addResult.Value.Should().Be(20m);
        subtractResult.Value.Should().Be(10m);
    }

    [Fact]
    public void Money_Should_Round_To_Two_Decimal_Places()
    {
        // Act
        var result = Money.TryCreate(10.156m);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(10.16m); // Rounded away from zero
    }

    [Fact]
    public void Money_Should_Round_To_Two_Decimal_Places_MidPoint_Rounding()
    {
        // Act
        var result1 = Money.TryCreate(10.155m);
        var result2 = Money.TryCreate(10.125m);

        // Assert
        result1.IsSuccess.Should().BeTrue();
        result1.Value!.Value.Should().Be(10.16m); // MidpointRounding.AwayFromZero

        result2.IsSuccess.Should().BeTrue();
        result2.Value!.Value.Should().Be(10.13m); // MidpointRounding.AwayFromZero
    }

    [Fact]
    public void Money_Should_Support_CompareTo()
    {
        // Arrange
        var money10Result = Money.TryCreate(10m);
        money10Result.IsSuccess.Should().BeTrue();
        var money10 = money10Result.Value!;

        var money20Result = Money.TryCreate(20m);
        money20Result.IsSuccess.Should().BeTrue();
        var money20 = money20Result.Value!;

        var money10DuplicateResult = Money.TryCreate(10m);
        money10DuplicateResult.IsSuccess.Should().BeTrue();
        var money10Duplicate = money10DuplicateResult.Value!;

        // Act & Assert
        money10.CompareTo(money20).Should().BeLessThan(0);
        money20.CompareTo(money10).Should().BeGreaterThan(0);
        money10.CompareTo(money10Duplicate).Should().Be(0);
    }

    [Fact]
    public void Money_Zero_Should_Have_Zero_Value()
    {
        // Act & Assert
        Money.Zero.Value.Should().Be(0m);
    }

    [Fact]
    public void Cpf_Should_Normalize_Input_With_Formatting()
    {
        // Act
        var result = Cpf.TryCreate("529.982.247-25");

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be("52998224725"); // Should remove formatting
    }

    [Fact]
    public void Cpf_Should_Fail_When_Empty()
    {
        // Act
        var result1 = Cpf.TryCreate("");
        var result2 = Cpf.TryCreate(null);
        var result3 = Cpf.TryCreate("   ");

        // Assert
        result1.IsSuccess.Should().BeFalse();
        result1.Error.Should().Be("cpf_empty");

        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Be("cpf_empty");

        result3.IsSuccess.Should().BeFalse();
        result3.Error.Should().Be("cpf_empty");
    }

    [Fact]
    public void Cpf_Should_Fail_When_Invalid_Length()
    {
        // Act
        var result1 = Cpf.TryCreate("123456789"); // Too short
        var result2 = Cpf.TryCreate("1234567890123"); // Too long

        // Assert
        result1.IsSuccess.Should().BeFalse();
        result1.Error.Should().Be("cpf_invalid_format");

        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Be("cpf_invalid_format");
    }

    [Fact]
    public void Cpf_Should_Fail_When_Contains_Non_Numeric_Characters()
    {
        // Act
        var result = Cpf.TryCreate("5299822472A");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("cpf_invalid_format");
    }

    [Fact]
    public void Cpf_Should_Fail_When_All_Digits_Are_Same()
    {
        // Act
        var result1 = Cpf.TryCreate("11111111111");
        var result2 = Cpf.TryCreate("00000000000");
        var result3 = Cpf.TryCreate("99999999999");

        // Assert
        result1.IsSuccess.Should().BeFalse();
        result1.Error.Should().Be("cpf_invalid_checksum");

        result2.IsSuccess.Should().BeFalse();
        result2.Error.Should().Be("cpf_invalid_checksum");

        result3.IsSuccess.Should().BeFalse();
        result3.Error.Should().Be("cpf_invalid_checksum");
    }

    [Fact]
    public void Cpf_ToString_Should_Return_Value()
    {
        // Arrange
        var result = Cpf.TryCreate("52998224725");
        result.IsSuccess.Should().BeTrue();
        var cpf = result.Value!;

        // Act
        var toString = cpf.ToString();

        // Assert
        toString.Should().Be("52998224725");
    }

    [Fact]
    public void Money_ToString_Should_Return_Formatted_Value()
    {
        // Arrange
        var result = Money.TryCreate(123.45m);
        result.IsSuccess.Should().BeTrue();
        var money = result.Value!;

        // Act
        var toString = money.ToString();

        // Assert
        toString.Should().Contain("123");
        toString.Should().Contain("45");
    }
}
