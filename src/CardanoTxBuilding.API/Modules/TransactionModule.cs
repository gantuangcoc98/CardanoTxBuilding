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

        group.MapPost("/lockTx", transactionHandler.LockTransaction)
            .WithName("LockTransaction")
            .WithDescription("Lock an asset to a smart contract");

        group.MapPost("/finalizeTx", transactionHandler.FinalizeTx)
            .WithName("FinalizeTransaction")
            .WithDescription("Finalize an unsigned transaction with witness set");

        group.MapPost("unlockTx", transactionHandler.UnlockTxAsync)
            .WithName("UnlockTransaction")
            .WithDescription("Unlock a transaction from smart contract");
    }
}