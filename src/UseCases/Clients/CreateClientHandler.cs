using Domain.Common;
using Domain.Entities;
using Domain.Interfaces;
using Domain.ValueObjects;

namespace UseCases.Clients;

public sealed record CreateClientCommand(string Cpf, string Name, string Email, string MobileNumber);

public interface ICreateClientHandler
{
    Task<Result<Client>> HandleAsync(CreateClientCommand command, CancellationToken cancellationToken = default);
}

public sealed class CreateClientHandler : ICreateClientHandler
{
    private readonly IReadRepository<Client> _clientReader;
    private readonly IWriteRepository<Client> _clientWriter;
    private readonly IUnitOfWork _unitOfWork;

    public CreateClientHandler(
        IReadRepository<Client> clientReader,
        IWriteRepository<Client> clientWriter,
        IUnitOfWork unitOfWork)
    {
        ArgumentNullException.ThrowIfNull(clientReader);
        ArgumentNullException.ThrowIfNull(clientWriter);
        ArgumentNullException.ThrowIfNull(unitOfWork);

        _clientReader = clientReader;
        _clientWriter = clientWriter;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Client>> HandleAsync(CreateClientCommand command, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var cpfResult = Cpf.TryCreate(command.Cpf);
        if (cpfResult.IsFailure)
        {
            return Result<Client>.Failure(cpfResult.Error!);
        }

        var clientIdResult = ClientId.TryCreate(Guid.NewGuid());
        if (clientIdResult.IsFailure)
        {
            return Result<Client>.Failure(clientIdResult.Error!);
        }

        var clientExists = await _clientReader
            .ExistsAsync(client => client.Cpf == cpfResult.Value, cancellationToken)
            .ConfigureAwait(false);
        if (clientExists)
        {
            return Result<Client>.Failure("client_cpf_already_exists");
        }

        var createResult = Client.Create(
            clientIdResult.Value,
            cpfResult.Value,
            command.Name,
            command.Email,
            command.MobileNumber);
        if (createResult.IsFailure)
        {
            return Result<Client>.Failure(createResult.Error!);
        }

        var client = createResult.Value ?? throw new InvalidOperationException("Client creation returned null.");

        await _clientWriter.AddAsync(client, cancellationToken).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return Result<Client>.Success(client);
    }
}
