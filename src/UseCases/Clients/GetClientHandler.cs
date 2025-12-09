using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Clients;

public sealed record GetClientQuery(Guid ClientId);

public interface IGetClientHandler
{
    Task<Result<Client>> HandleAsync(GetClientQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetClientHandler : IGetClientHandler
{
    private readonly IReadRepository<Client> _clientReader;

    public GetClientHandler(IReadRepository<Client> clientReader)
    {
        ArgumentNullException.ThrowIfNull(clientReader);
        _clientReader = clientReader;
    }

    public async Task<Result<Client>> HandleAsync(GetClientQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var clientIdResult = ClientId.TryCreate(query.ClientId);
        if (clientIdResult.IsFailure)
        {
            return Result<Client>.Failure(clientIdResult.Error!);
        }

        var client = await _clientReader
            .GetByIdAsync(new object[] { clientIdResult.Value }, cancellationToken)
            .ConfigureAwait(false);

        if (client is null)
        {
            return Result<Client>.Failure("client_not_found");
        }

        return Result<Client>.Success(client);
    }
}
