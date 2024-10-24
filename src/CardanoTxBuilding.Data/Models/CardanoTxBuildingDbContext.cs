using System.Dynamic;
using Cardano.Sync.Data;
using CardanoTxBuilding.Data.Models.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace CardanoTxBuilding.Data.Models;

public class CardanoTxBuildingDbContext(
    DbContextOptions options, IConfiguration configuration
) : CardanoDbContext(options, configuration)
{
    public DbSet<LockedAda> LockedAda { get; set; }
    override protected void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<LockedAda>(entity => {
            entity.HasKey(e => new { e.Slot, e.Hash, e.Index });
            entity.Property(e => e.Slot).IsRequired();
            entity.Property(e => e.Hash).IsRequired();
            entity.Property(e => e.Index).IsRequired();
        });
    }
}