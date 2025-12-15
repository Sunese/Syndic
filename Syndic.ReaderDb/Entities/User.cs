using Syndic.ReaderDb.Entities;

namespace Syndic.ReaderDb.Entities;

public class User : Entity
{
  public string Email { get; private set; } = null!;
  public string? OIDCSubject { get; private set; }
  public DateTimeOffset CreatedAt { get; private set; }

  // Navigation Properties
  public ICollection<Subscription> Subscriptions { get; private set; } = new List<Subscription>();

  // EF Constructor
  private User() { }

  public User(string email, string? oidcSubject = null)
  {
    if (string.IsNullOrWhiteSpace(email))
      throw new ArgumentException("Email cannot be null or empty", nameof(email));

    Email = email;
    CreatedAt = DateTimeOffset.UtcNow;
    OIDCSubject = oidcSubject;
  }
}
