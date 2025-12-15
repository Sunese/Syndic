using Syndic.ReaderDb.Entities;
using Syndic.ReaderService;

namespace Syndic;

public static class HttpContextExtensions
{
  public static User GetUser(this HttpContext httpContext)
  {
    var item = httpContext.Items[Constants.CurrentUserKey]
      ?? throw new InvalidOperationException($"No {Constants.CurrentUserKey} item is set for the HTTP Context");

    var user = item as User
      ?? throw new InvalidCastException($"Item {Constants.CurrentUserKey} could not be casted to a {typeof(User)}");

    return user;
  }
}
