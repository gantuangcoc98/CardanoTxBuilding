using Cardano.Sync.Reducers;
using CardanoTxBuilding.Data.Models;
using CardanoTxBuilding.Data.Models.Entity;
using Microsoft.EntityFrameworkCore;
using PallasDotnet.Models;

namespace CardanoTxBuilding.Sync.Reducers;

public class LockedAdaByAddressReducer(
    IConfiguration configuration,
    IDbContextFactory<CardanoTxBuildingDbContext> dbContextFactory
) : IReducer<LockedAda>
{
    public async Task RollBackwardAsync(NextResponse response)
    {
        using CardanoTxBuildingDbContext _dbContext = dbContextFactory.CreateDbContext();

        ulong rollbackSlot = response.Block.Slot;
        IQueryable<LockedAda> rollbackEntries = _dbContext.LockedAda.AsNoTracking().Where(lba => lba.Slot > rollbackSlot);
        _dbContext.LockedAda.RemoveRange(rollbackEntries);

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }

    public async Task RollForwardAsync(NextResponse response)
    {
        using CardanoTxBuildingDbContext _dbContext = dbContextFactory.CreateDbContext();

        Block block = response.Block;

        IEnumerable<TransactionBody> transactionBodies = block.TransactionBodies;
 
        List<LockedAda?> lockedAdas = transactionBodies
            .SelectMany(txBody => txBody.Outputs
                .Select(output =>
                {
                    string txHash = txBody.Id.ToHex();

                    string validatorAddress = configuration["ValidatorScriptAddress"]!;

                    string outputAddress = output.Address.ToBech32();

                    if (output.Datum is null || outputAddress != validatorAddress) return null;

                    LockedAda lockedAda = new()
                    {
                        Slot = block.Slot,
                        Hash = txBody.Id.ToHex(),
                        Index = output.Index,
                        Amount = output.Amount.Coin,
                        Datum = output.Datum.Data
                    };

                    return lockedAda;
                })
            ).ToList();

        lockedAdas.ForEach(lockedAda => 
        {
            if (lockedAda is null) return;

            _dbContext.Add(lockedAda);
        });

        await _dbContext.SaveChangesAsync();
        await _dbContext.DisposeAsync();
    }
}