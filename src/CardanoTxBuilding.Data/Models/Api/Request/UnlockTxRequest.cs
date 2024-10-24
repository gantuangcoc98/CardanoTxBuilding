namespace CardanoTxBuilding.Data.Models.Api.Request;

public record UnlockTxRequest
{
    public string OwnerAddress { get; init; } = default!;
    public string LockedUtxoHash { get; init; } = default!;
    public ulong LockedUtxoIndex { get; init; } = default!;
    public string CollateralUtxoCbor { get; init; } = default!;
}