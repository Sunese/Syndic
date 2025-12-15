using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Syndic.ReaderDb.Entities;

namespace ReaderService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Subscription entity.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
  public void Configure(EntityTypeBuilder<User> builder)
  {
    builder.ToTable("users");

    builder.HasKey(s => s.Id);

    builder.HasIndex(s => s.Email)
        .IsUnique();

    builder.Property(s => s.Email)
        .IsRequired()
        .HasMaxLength(255);
  }
}
