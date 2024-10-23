using Carter;

namespace CardanoTxBuilding.API.Handlers;

public class TransactionModule(TransactionHandler transactionHandler) : CarterModule
{
    public override void AddRoutes(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder group = app
            .MapGroup("/api/transaction")
            .WithTags("Transaction")
            .WithOpenApi();

        group.MapPost("/lock", transactionHandler.LockTransaction)
            .WithName("LockTransaction")
            .WithDescription("Lock an asset to a smart contract");
    }
}