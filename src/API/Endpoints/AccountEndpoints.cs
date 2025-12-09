using API.Contracts.Responses;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using UseCases.Accounts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;

namespace API.Endpoints;

internal static class AccountEndpoints
{
    public static RouteGroupBuilder MapAccountEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/accounts");

        group.MapGet("/{accountNumber}", GetAccountAsync)
            .WithName("GetAccount")
            .WithSummary("Retrieve account details by account number.")
            .Produces<AccountResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        group.MapGet("/{accountNumber}/balance", GetAccountBalanceAsync)
            .WithName("GetAccountBalance")
            .WithSummary("Retrieve current account balance.")
            .Produces<AccountBalanceResponse>(StatusCodes.Status200OK)
            .Produces<ErrorResponse>(StatusCodes.Status400BadRequest)
            .Produces<ErrorResponse>(StatusCodes.Status404NotFound);

        return group;
    }

    private static async Task<Results<Ok<AccountResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> GetAccountAsync(
        string accountNumber,
        IGetAccountHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new GetAccountQuery(accountNumber), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                "bank_account_not_found" => TypedResults.NotFound(new ErrorResponse(result.Error)),
                _ => TypedResults.BadRequest(new ErrorResponse(result.Error!))
            };
        }

        return TypedResults.Ok(AccountResponse.FromDomain(result.Value!));
    }

    private static async Task<Results<Ok<AccountBalanceResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>>> GetAccountBalanceAsync(
        string accountNumber,
        IGetAccountBalanceHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler
            .HandleAsync(new GetAccountBalanceQuery(accountNumber), cancellationToken)
            .ConfigureAwait(false);

        if (result.IsFailure)
        {
            return result.Error switch
            {
                "bank_account_not_found" => TypedResults.NotFound(new ErrorResponse(result.Error)),
                _ => TypedResults.BadRequest(new ErrorResponse(result.Error!))
            };
        }

        var response = new AccountBalanceResponse(accountNumber, result.Value!.Value);
        return TypedResults.Ok(response);
    }
}
