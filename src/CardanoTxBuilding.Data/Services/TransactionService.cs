using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoTxBuilding.Data.Models;
using CardanoTxBuilding.Data.Models.Datum;
using CardanoTxBuilding.Data.Models.Entity;
using CardanoTxBuilding.Data.Utils;
using Chrysalis.Cbor;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CSharpRedeemer = CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts.Redeemer;
using PeterO.Cbor2;
using CardanoTxBuilding.Data.Extensions;
using System.Net.Http.Headers;
using CardanoSharp.Wallet.Extensions.Models;

namespace CardanoTxBuilding.Data.Services;

public class TransactionService(
    IDbContextFactory<CardanoTxBuildingDbContext> dbContextFactory,
    IConfiguration configuration
)
{
    public string LockTransaction(string ownerAddress, double amount, string utxoCbor)
    {
        Address validatorAddress = new(configuration["ValidatorScriptAddress"]!);
        Address _ownerAddress = new(ownerAddress);
        IEnumerable<Utxo> utxos = CardanoTxBuildingUtils.DeserializeUtxoCborHex([utxoCbor]);
        Utxo utxo = utxos.First();
        ulong lockedAmount = (ulong)(amount * Math.Pow(10, 6));

        ITransactionBodyBuilder txBodyBuilder = TransactionBodyBuilder.Create;

        txBodyBuilder.AddInput(utxo);

        TransactionOutput lockedOutput = new()
        {
            Address = validatorAddress.GetBytes(),
            Value = new()
            {
                Coin = lockedAmount,
                MultiAsset = []
            },
            DatumOption = new()
            {
                RawData = CborSerializer.Serialize(new Empty())
            }
        };
        txBodyBuilder.AddOutput(lockedOutput);

        TransactionOutput changeAddress = new()
        {
            Address = _ownerAddress.GetBytes(),
            Value = new()
            {
                Coin = utxo.Balance.Lovelaces - lockedAmount,
                MultiAsset = []
            }
        };
        txBodyBuilder.AddOutput(changeAddress);

        ITransactionBuilder txBuilder = TransactionBuilder.Create;
        txBuilder.SetBody(txBodyBuilder);
        txBuilder.SetWitnesses(TransactionWitnessSetBuilder.Create);

        Transaction tx = txBuilder.Build();
        uint fee = tx.CalculateAndSetFee(numberOfVKeyWitnessesToMock: 1);
        tx.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;

        return Convert.ToHexString(tx.Serialize());
    }

    public string FinalizeTx(string unsignedTxCbor, string witnessSetCbor)
    {
        ITransactionWitnessSetBuilder witnessSetBuilder = TransactionWitnessSetBuilder.Create;

        TransactionWitnessSet witnessSet = CardanoTxBuildingUtils.DeserializeTxWitnessSet(witnessSetCbor);
        witnessSet.VKeyWitnesses.ToList().ForEach(witness => witnessSetBuilder.AddVKeyWitness(witness));

        Transaction tx = Convert.FromHexString(unsignedTxCbor).DeserializeTransaction();
        tx.TransactionWitnessSet = witnessSet;

        return Convert.ToHexString(tx.Serialize());
    }

    public async Task<string> UnlockTransactionAsync(
        string ownerAddress, 
        string lockedUtxoHash, 
        ulong lockedUtxoIndex, 
        string collateralUtxoCbor
    )
    {
        // Prepare the properties we need for TxInputs, TxOutputs
        using CardanoTxBuildingDbContext dbContext = dbContextFactory.CreateDbContext();

        Address _validatorAddress = new(configuration["ValidatorScriptAddress"]!);
        Address _ownerAddress = new(ownerAddress);
        IEnumerable<Utxo> collateralUtxos = CardanoTxBuildingUtils.ConvertUtxoListCbor([collateralUtxoCbor])
            ?? throw new Exception("Collateral utxo not found");
        Utxo collateralUtxo = collateralUtxos.First();

        LockedAda lockedAda = await dbContext.LockedAda
            .AsNoTracking()
            .Where(la => la.Hash == lockedUtxoHash && la.Index == lockedUtxoIndex)
            .FirstOrDefaultAsync() ?? throw new Exception("Locked ADA not found");

        Utxo lockedUtxo = new()
        {
            TxHash = lockedAda.Hash,
            TxIndex = (uint)lockedAda.Index,
            Balance = new()
            {
                Lovelaces = lockedAda.Amount,
                Assets = []
            },
            OutputAddress = configuration["ValidatorScriptAddress"]!,
            OutputDatumOption = new()
            {
                RawData = lockedAda.Datum
            }
        };

        TransactionOutput changeAddress = new()
        {
            Address = _ownerAddress.GetBytes(),
            Value = new()
            {
                Coin = lockedAda.Amount,
                MultiAsset = []
            }
        };

        TransactionInput referenceScript = new()
        {
            TransactionId = Convert.FromHexString(configuration["ValidatorScriptRefHash"]!),
            TransactionIndex = uint.Parse(configuration["ValidatorScriptRefIndex"]!),
            Output = new()
            {
                Address = _validatorAddress.GetBytes(),
                Value = new()
                {
                    Coin = ulong.Parse(configuration["ValidatorScriptLovelaceAmount"]!)
                },
                ScriptReference = new()
                {
                    PlutusV2Script = new()
                    {
                        script = Convert.FromHexString(configuration["ValidatorScriptRefCbor"]!)
                    }
                }
            }
        };

        ITransactionBodyBuilder txBodyBuilder = TransactionBodyBuilder.Create;
        txBodyBuilder.AddReferenceInput(referenceScript);
        txBodyBuilder.AddInput(lockedUtxo);
        txBodyBuilder.AddCollateralInput(collateralUtxo.TxHash, collateralUtxo.TxIndex);
        txBodyBuilder.AddOutput(changeAddress);
        txBodyBuilder.AddRequiredSigner(_ownerAddress.GetPublicKeyHash());

        // Redeemer
        RedeemerBuilder redeemerBuilder = RedeemerBuilder.Create;
        redeemerBuilder.SetTag(CardanoSharp.Wallet.Enums.RedeemerTag.Spend);
        redeemerBuilder.SetPlutusData(CBORObject.DecodeFromBytes(CborSerializer.Serialize(new Models.Datum.Redeemer())).GetPlutusData());

        List<string> inputOutrefs = txBodyBuilder.Build().TransactionInputs.Select(i => Convert.ToHexString(i.TransactionId).ToLowerInvariant() + i.TransactionIndex).ToList();
        inputOutrefs.Sort();
        int redeemerIndex = inputOutrefs.IndexOf(lockedAda.Hash.ToLowerInvariant() + lockedAda.Index);
        redeemerBuilder.SetIndex((uint)redeemerIndex);

        // Add redeemer to witness set
        ITransactionWitnessSetBuilder witnessSetBuilder = TransactionWitnessSetBuilder.Create;
        witnessSetBuilder.AddRedeemer(redeemerBuilder);

        // Build the entire transaction including the body and witness set
        ITransactionBuilder txBuilder = TransactionBuilder.Create;
        txBuilder.SetBody(txBodyBuilder);
        txBuilder.SetWitnesses(witnessSetBuilder);

        Transaction tx = txBuilder.Build();
        uint fee = tx.CalculateAndSetFee(numberOfVKeyWitnessesToMock: 1);
        tx.TransactionBody.TransactionOutputs.Last().Value.Coin -= fee;
        string unsignedTxCbor = Convert.ToHexString(tx.Serialize());

        return unsignedTxCbor;
    }
}