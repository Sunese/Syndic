using Microsoft.EntityFrameworkCore;
using Syndic.ReaderDb.Entities;

namespace Syndic.ReaderDb;

/// <summary>
///
/// </summary>
public class ReaderDbContext : DbContext
{
  public ReaderDbContext(DbContextOptions<ReaderDbContext> options) : base(options)
  {
  }

  public DbSet<Subscription> Subscriptions => Set<Subscription>();
  public DbSet<User> Users => Set<User>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    base.OnModelCreating(modelBuilder);

    // Apply all entity configurations from the current assembly
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReaderDbContext).Assembly);
  }
}
