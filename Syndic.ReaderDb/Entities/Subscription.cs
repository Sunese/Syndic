namespace Syndic.ReaderDb.Entities;

/// <summary>
/// Subscription entity representing a user's subscription to a feed.
/// </summary>
public class Subscription : Entity
{
  public Guid UserId { get; private set; }
  public Uri ChannelUrl { get; private set; }
  public string? ChannelTitle { get; private set; }
  public string? CustomTitle { get; private set; }
  public DateTimeOffset SubscribedAt { get; private set; }
  public DateTimeOffset? LastReadAt { get; private set; }

  // Navigation properties
  // public Feed Feed { get; private set; } = null!;
  public User User { get; private set; } = null!;

  // EF Constructor
  private Subscription() { }

  public Subscription(Guid userId, Uri channelUrl, string? channelTitle = null, string? customTitle = null)
  {
    UserId = userId;
    CustomTitle = customTitle;
    SubscribedAt = DateTimeOffset.UtcNow;
    ChannelTitle = channelTitle;
    ChannelUrl = channelUrl;
  }
}
