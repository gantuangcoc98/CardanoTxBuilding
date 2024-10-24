namespace CardanoTxBuilding.Data.Models.Api.Request;

public record FinalizeTxRequest
{
    public string unsignedTxCbor { get; init; } = default!;
    public string witnessSetCbor { get; init; } = default!;
}