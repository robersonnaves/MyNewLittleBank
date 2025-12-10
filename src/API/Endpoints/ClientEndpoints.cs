using API.Contracts.Requests;
using API.Contracts.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Builder;
using UseCases.Clients;
using Microsoft.AspNetCore.Http;
using System.Diagnostics.CodeAnalysis;

namespace API.Endpoints;

internal static class ClientEndpoints
{
    public static RouteGroupBuilder MapClientEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/clients");

        group.MapPost("/", CreateClientAsync)
            .WithName("CreateClient")
            .WithSummary("Create a new client.")
            .Produces<ClientResponse>(StatusCodes.Status201Created)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status409Conflict);

        group.MapGet("/{id:guid}", GetClientAsync)
            .WithName("GetClient")
            .WithSummary("Retrieve a client by id.")
            .Produces<ClientResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/cpf/{cpf}", GetClientByCpfAsync)
            .WithName("GetClientByCpf")
            .WithSummary("Retrieve a client by CPF.")
            .Produces<ClientResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Created<ClientResponse>, BadRequest<ErrorResponse>, Conflict<ErrorResponse>>> CreateClientAsync(
        [FromBody] CreateClientRequest request,
        ICreateClientHandler handler,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var result = await handler.HandleAsync(
            new CreateClientCommand(request.Cpf, request.Name, request.Email, request.MobileNumber),
            cancellationToken).ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                "client_cpf_already_exists" => TypedResults.Conflict(new ErrorResponse(result.Error)),
                _ => TypedResults.BadRequest(new ErrorResponse(result.Error!))
            };
        }

        var response = ClientResponse.FromDomain(result.Value!);
        return TypedResults.Created($"/clients/{response.Id}", response);
    }

    private static async Task<Results<Ok<ClientResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> GetClientAsync(
        Guid id,
        IGetClientHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new GetClientQuery(id), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                "client_not_found" => TypedResults.NotFound(new ErrorResponse(result.Error)),
                _ => TypedResults.BadRequest(new ErrorResponse(result.Error!))
            };
        }

        return TypedResults.Ok(ClientResponse.FromDomain(result.Value!));
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Mapping failures are converted to bad request responses for HTTP clients.")]
    private static async Task<Results<Ok<ClientResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> GetClientByCpfAsync(
        string cpf,
        IGetClientByCpfHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await handler
                .HandleAsync(new GetClientByCpfQuery(cpf), cancellationToken)
                .ConfigureAwait(false);

            if (result.IsFailure)
            {
                return result.Error switch
                {
                    "client_not_found" => TypedResults.NotFound(new ErrorResponse(result.Error)),
                    _ => TypedResults.BadRequest(new ErrorResponse(result.Error!))
                };
            }

            return TypedResults.Ok(ClientResponse.FromDomain(result.Value!));
        }
        catch (Exception)
        {
            return TypedResults.BadRequest(new ErrorResponse("client_lookup_failed"));
        }
    }
}
