using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Syndic.ReaderDb;

namespace ReaderService.MigrationService;

public class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
  public const string ActivitySourceName = "Migrations";
  private static readonly ActivitySource s_activitySource = new(ActivitySourceName);

  protected override async Task ExecuteAsync(CancellationToken cancellationToken)
  {
    using var activity = s_activitySource.StartActivity("Migrating database", ActivityKind.Client);

    try
    {
      using var scope = serviceProvider.CreateScope();
      var dbContext = scope.ServiceProvider.GetRequiredService<ReaderDbContext>();

      await RunMigrationAsync(dbContext, activity, cancellationToken);
      // await SeedDataAsync(dbContext, cancellationToken);
    }
    catch (Exception ex)
    {
      activity?.AddException(ex);
      throw;
    }

    hostApplicationLifetime.StopApplication();
  }

  private async Task RunMigrationAsync(ReaderDbContext dbContext, Activity? activity, CancellationToken cancellationToken)
  {
    if (dbContext.Database.HasPendingModelChanges())
    {
      var message = "The database model has pending changes that require a migration. Please create the migrations and re-run the application to apply the necessary migrations.";
      logger.LogError(message);
      activity?.SetStatus(ActivityStatusCode.Error, message);
      throw new InvalidOperationException(message);
    }

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
    if (pendingMigrations.Any())
    {
      var strategy = dbContext.Database.CreateExecutionStrategy();
      await strategy.ExecuteAsync(async () =>
      {
        // Run migration in a transaction to avoid partial migration if it fails.
        await dbContext.Database.MigrateAsync(cancellationToken);
      });
    }
    else
    {
      logger.LogInformation("No pending migrations to apply.");
    }
  }

  // private async Task SeedDataAsync(InventoryDbContext dbContext, CancellationToken cancellationToken)
  // {
  //   if (await dbContext.InventoryItems.AnyAsync(cancellationToken))
  //   {
  //     logger.LogInformation("Database already seeded.");
  //     return;
  //   }

  //   InventoryItem firstItem = new("This is a test item", 5);

  //   var strategy = dbContext.Database.CreateExecutionStrategy();
  //   await strategy.ExecuteAsync(async () =>
  //   {
  //     // Seed the database
  //     await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
  //     await dbContext.InventoryItems.AddAsync(firstItem, cancellationToken);
  //     await dbContext.SaveChangesAsync(cancellationToken);
  //     await transaction.CommitAsync(cancellationToken);
  //   });

  //   logger.LogInformation("Database seeded with initial test data.");
  // }
}
