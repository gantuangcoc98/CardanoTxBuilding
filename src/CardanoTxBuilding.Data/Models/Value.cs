namespace CardanoTxBuilding.Data.Models;

public record Value
{
    public ulong Coin { get; set; } = default!;

    public Dictionary<string, Dictionary<string, ulong>> MultiAsset { get; set; } = default!;
}