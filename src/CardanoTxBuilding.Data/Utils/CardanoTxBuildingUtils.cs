using CardanoSharp.Wallet.CIPs.CIP2;
using CardanoSharp.Wallet.CIPs.CIP2.ChangeCreationStrategies;
using CardanoSharp.Wallet.CIPs.CIP2.Models;
using CardanoSharp.Wallet.Enums;
using CardanoSharp.Wallet.Extensions.Models;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoSharp.Wallet.Utilities;
using Microsoft.Extensions.Configuration;
using PeterO.Cbor2;

namespace CardanoTxBuilding.Data.Utils;

public static class CardanoTxBuildingUtils
{    
    public static NetworkType GetNetworkType(IConfiguration configuration)
    {
        int netowrkType = int.Parse(configuration["CardanoNetworkMagic"]!);
        return netowrkType switch
        {
            764824073 => NetworkType.Mainnet,
            1 => NetworkType.Preprod,
            2 => NetworkType.Preview,
            _ => throw new NotImplementedException()
        };
    }
    public static Address ValidatorAddress(PlutusV2Script plutusV2Script, IConfiguration configuration)
    {
        return AddressUtility.GetEnterpriseScriptAddress(plutusV2Script, GetNetworkType(configuration));
    }

    public static Address ValidatorAddress(byte[] validatorScriptCbor, IConfiguration configuration)
    {
        PlutusV2Script plutusScript = PlutusV2ScriptBuilder.Create
            .SetScript(validatorScriptCbor)
            .Build();

        return ValidatorAddress(plutusScript, configuration);
    }

    public static CoinSelection GetCoinSelection(
        IEnumerable<TransactionOutput> outputs,
        IEnumerable<Utxo> utxos, string changeAddress,
        ITokenBundleBuilder? mint = null,
        List<Utxo>? requiredUtxos = null,
        int limit = 20, ulong feeBuffer = 0uL
    )
    {
        RandomImproveStrategy coinSelectionStrategy = new();
        SingleTokenBundleStrategy changeCreationStrategy = new();
        CoinSelectionService coinSelectionService = new(coinSelectionStrategy, changeCreationStrategy);

        int retry = 0;

        while (retry < 100)
        {
            try
            {
                CoinSelection result = coinSelectionService
                    .GetCoinSelection(outputs.ToList(), utxos.ToList(), changeAddress, mint, null, requiredUtxos, limit, feeBuffer);

                return result;
            }
            catch
            {
                retry++;
            }
        }

        throw new Exception("Coin selection failed");
    }

    public static IEnumerable<Utxo>? ConvertUtxoListCbor(IEnumerable<string>? utxoCbors)
    {
        if (utxoCbors is null) return null;
        if (!utxoCbors.Any()) return null;

        return utxoCbors.Select(utxoCbor =>
        {
            CBORObject utxoCborObj = CBORObject.DecodeFromBytes(Convert.FromHexString(utxoCbor));
            return utxoCborObj.GetUtxo();
        }).ToList();
    }
    
    public static Dictionary<byte[], NativeAsset> NativeAssetsFromMultiAsset(Dictionary<string, Dictionary<string, ulong>> value)
    {
        return value.ToDictionary(
            kvp => Convert.FromHexString(kvp.Key),
            kvp => new NativeAsset
            {
                Token = kvp.Value.ToDictionary(
                    kvp2 => Convert.FromHexString(kvp2.Key),
                    kvp2 => (long)kvp2.Value
                )
            }
        );
    }
}