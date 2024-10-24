using CardanoSharp.Wallet.Extensions.Models.Transactions;
using CardanoSharp.Wallet.Models;
using CardanoSharp.Wallet.Models.Addresses;
using CardanoSharp.Wallet.Models.Transactions;
using CardanoSharp.Wallet.Models.Transactions.TransactionWitness.PlutusScripts;
using CardanoSharp.Wallet.TransactionBuilding;
using CardanoTxBuilding.Data.Services;
using CardanoTxBuilding.Data.Models.Api.Request;
using CardanoTxBuilding.Data.Models.Datum;
using CardanoTxBuilding.Data.Utils;
using Chrysalis.Cbor;

namespace CardanoTxBuilding.API.Handlers;

public class TransactionHandler(
    TransactionService transactionService
)
{
    public string LockTransaction(LockTransactionRequest lockTransactionRequest)
    {
        return transactionService.LockTransaction(
            lockTransactionRequest.OwnerAddress, 
            lockTransactionRequest.Amount, 
            lockTransactionRequest.UtxoCbor
        );
    }

    public string FinalizeTx(FinalizeTxRequest finalizeTxRequest)
    {
        return transactionService.FinalizeTx(finalizeTxRequest.unsignedTxCbor, finalizeTxRequest.witnessSetCbor);
    }

    public async Task<string> UnlockTxAsync(UnlockTxRequest unlockTxRequest)
    {
        return await transactionService.UnlockTransactionAsync(
            unlockTxRequest.OwnerAddress, 
            unlockTxRequest.LockedUtxoHash, 
            unlockTxRequest.LockedUtxoIndex,
            unlockTxRequest.CollateralUtxoCbor
        );
    }
}