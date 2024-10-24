
namespace CardanoTxBuilding.Data.Models.Api.Request;

public record LockTransactionRequest
{
    public string OwnerAddress { get; init; } = default!;
    public double Amount { get; init; } = default!;
    public string UtxoCbor { get; init; } = default!;
}