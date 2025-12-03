#pragma warning disable CA1000
namespace Domain.Common;

/// <summary>
/// Lightweight result type to communicate success/failure without exceptions.
/// </summary>
public readonly record struct Result(bool IsSuccess, string? Error)
{
    public static Result Success() => new(true, null);

    public static Result Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message must be provided.", nameof(error));
        }

        return new Result(false, error);
    }

    public bool IsFailure => !IsSuccess;
}

public readonly record struct Result<T>(bool IsSuccess, T? Value, string? Error)
{
    public static Result<T> Success(T value) => new(true, value, null);

    public static Result<T> Failure(string error)
    {
        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Error message must be provided.", nameof(error));
        }

        return new Result<T>(false, default, error);
    }

    public bool IsFailure => !IsSuccess;
}
#pragma warning restore CA1000
