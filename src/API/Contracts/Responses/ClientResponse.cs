using Domain.Entities;

namespace API.Contracts.Responses;

public sealed record ClientResponse(Guid Id, string Name, string Email, string Cpf, string MobileNumber)
{
    public static ClientResponse FromDomain(Client client)
    {
        ArgumentNullException.ThrowIfNull(client);

        return new ClientResponse(
            client.Id.Value,
            client.Name,
            client.Email,
            client.Cpf.Value,
            client.MobileNumber);
    }
}
