using Chrysalis.Cbor;

namespace CardanoTxBuilding.Data.Models.Datum;

[CborSerializable(CborType.Constr, Index = 0)]
public record Empty() : RawCbor;