using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoTxBuilding.Data.Models.Api.Request;
using CardanoTxBuilding.Data.Models.Datum;
using CardanoTxBuilding.Data.Utils;
using Chrysalis.Cbor;

namespace CardanoTxBuilding.API.Handlers;

public class TransactionHandler(
    IConfiguration configuration
)
{
    public string LockTransaction(LockTransactionRequest lockTransactionRequest)
    {
        Address validatorAddress = new Address(configuration["ValidatorScriptAddress"]!);
        Address ownerAddress = new Address(lockTransactionRequest.OwnerAddress);

        ITransactionBodyBuilder txBodyBuilder = TransactionBodyBuilder.Create;

        TransactionInput transactionInput = new(){
            TransactionId = Convert.FromHexString("97a6bde80ea447640bbdb369b2bee71c36b9a6e523b8a94328c0687945fb3e51"),
            TransactionIndex = 1,
            Output = new()
            {
                Value = new()
                {
                     Coin = 9_894_359_904
                },
                Address = ownerAddress.GetBytes()
            }
        };
        txBodyBuilder.AddInput(transactionInput);

        ulong lockedAmount = (ulong)(lockTransactionRequest.Amount * Math.Pow(10, 6));

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
            Address = ownerAddress.GetBytes(),
            Value = new()
            {
                Coin = 9_894_359_904 - lockedAmount,
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
        string unsignedTxCbor = Convert.ToHexString(tx.Serialize());

        return unsignedTxCbor;
    }
}