using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Clients;

public sealed record GetClientByCpfQuery(string Cpf);

public interface IGetClientByCpfHandler
{
    Task<Result<Client>> HandleAsync(GetClientByCpfQuery query, CancellationToken cancellationToken = default);
}

public sealed class GetClientByCpfHandler : IGetClientByCpfHandler
{
    private readonly IReadRepository<Client> _clientReader;

    public GetClientByCpfHandler(IReadRepository<Client> clientReader)
    {
        ArgumentNullException.ThrowIfNull(clientReader);
        _clientReader = clientReader;
    }

    public async Task<Result<Client>> HandleAsync(GetClientByCpfQuery query, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var cpfResult = Cpf.TryCreate(query.Cpf);
        if (cpfResult.IsFailure)
        {
            return Result<Client>.Failure(cpfResult.Error!);
        }

        var clients = await _clientReader
            .ListAsync(client => client.Cpf == cpfResult.Value, cancellationToken)
            .ConfigureAwait(false);

        if (clients.Count == 0)
        {
            return Result<Client>.Failure("client_not_found");
        }

        return Result<Client>.Success(clients[0]);
    }
}
