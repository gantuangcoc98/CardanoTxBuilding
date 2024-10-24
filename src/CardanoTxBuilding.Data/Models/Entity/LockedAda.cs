using Cardano.Sync.Data.Models;

namespace CardanoTxBuilding.Data.Models.Entity;

public record LockedAda : IReducerModel
{
    public ulong Slot { get; set; } = default!;
    public string Hash { get; set; } = default!;
    public ulong Index { get; set; } = default!;
    public ulong Amount { get; set; } = default!;
    public byte[] Datum { get; set; } = default!;
};