namespace Domain.ValueObjects;

public readonly record struct ClientId(Guid Value)
{
    public static Result<ClientId> TryCreate(Guid value)
    {
        if (value == Guid.Empty)
        {
            return Result<ClientId>.Failure("client_id_empty");
        }

        return Result<ClientId>.Success(new ClientId(value));
    }

    public override string ToString() => Value.ToString();
}
