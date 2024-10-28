using Chrysalis.Cardano.Models.Cbor;
using Chrysalis.Cbor;

namespace CardanoTxBuilding.Data.Models.Datum;

[CborSerializable(CborType.Constr, Index = 0)]
public record Redeemer(
    [CborProperty(0)]
    CborBytes Message
) : RawCbor;