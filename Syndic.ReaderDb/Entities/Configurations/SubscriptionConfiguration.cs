using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Syndic.ReaderDb.Entities;

namespace ReaderService.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for the Subscription entity.
/// </summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
  public void Configure(EntityTypeBuilder<Subscription> builder)
  {
    builder.ToTable("subscriptions");

    builder.HasKey(s => s.Id);

    builder.Property(s => s.UserId)
        .IsRequired();

    builder.Property(s => s.CustomTitle)
        .HasMaxLength(500);

    builder.Property(s => s.SubscribedAt)
        .IsRequired();

    builder.Property(s => s.LastReadAt);

    // Navigation properties are configured in FeedConfiguration and CategoryConfiguration

    // Indexes for better query performance
    builder.HasIndex(s => s.UserId);

    // Unique constraint - one subscription per user per channel
    builder.HasIndex(s => new { s.UserId, s.ChannelUrl })
        .IsUnique();
  }
}
