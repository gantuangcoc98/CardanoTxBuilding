using Cardano.Sync.Data.Models;
using Cardano.Sync.Extensions;
using Cardano.Sync.Reducers;
using CardanoTxBuilding.Data.Models;
using CardanoTxBuilding.Sync.Reducers;
using Microsoft.EntityFrameworkCore;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddCardanoIndexer<CardanoTxBuildingDbContext>(builder.Configuration);
builder.Services.AddSingleton<IReducer<IReducerModel>, LockedAdaByAddressReducer>();

WebApplication app = builder.Build();

using IServiceScope scope = app.Services.CreateScope();
IServiceProvider services = scope.ServiceProvider;
var context = services.GetRequiredService<CardanoTxBuildingDbContext>();
if (context.Database.GetPendingMigrations().Any())
    context.Database.Migrate();

app.Run();
