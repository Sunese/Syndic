using System.Globalization;
using Syndic.ReaderDb;
using Microsoft.EntityFrameworkCore;

namespace Syndic.ReaderService.Middleware;

public class UserMiddleware
{
  private readonly RequestDelegate _next;

  public UserMiddleware(RequestDelegate next)
  {
    _next = next;
  }

  public async Task InvokeAsync(HttpContext context, ReaderDbContext db, ILogger<UserMiddleware> logger)
  {
    // // If user is authenticated via OIDC, make sure they are created in our system
    if (context.User.Identity is not null && context.User.Identity.IsAuthenticated)
    {
      // NOTE: if we re-add identity or any "sign in with email" kind of thing, 
      //       we should check wether we signed in with that or via an external provider
      //      (because if via identity, the user should already exist in our system)

      var userEmail = context.User.FindFirst(Constants.SubClaim)?.Value;

      if (userEmail is null)
      {
        logger.LogWarning("Authenticated user has no sub claim (expected email), cannot proceed");
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsync("Unauthorized: Email claim is missing");
        return;
      }

      // One account per email, regardless of provider. If the same email is used via GitHub
      // and Google, they share the same account and subscriptions. OIDCSubject records the
      // provider used when the account was first created.
      var user = await db.Users
        .FirstOrDefaultAsync(u => u.Email == userEmail);
      if (user is null)
      {
        var provider = context.User.FindFirst(Constants.ProviderClaim)?.Value;
        if (provider is null)
        {
          logger.LogWarning("Authenticated user has no provider claim, cannot create user");
          context.Response.StatusCode = StatusCodes.Status401Unauthorized;
          await context.Response.WriteAsync("Unauthorized: Provider claim is missing");
          return;
        }
        user = new ReaderDb.Entities.User(userEmail, provider);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("Created new user {UserEmail} with ID {UserId}", userEmail, user.Id);
      }
      else
      {
        var currentProvider = context.User.FindFirst(Constants.ProviderClaim)?.Value;
        if (currentProvider is not null && currentProvider != user.OIDCSubject)
        {
          logger.LogDebug(
            "User {UserEmail} signed in via {CurrentProvider} but account was created via {StoredProvider}",
            userEmail, currentProvider, user.OIDCSubject);
        }
      }

      // add user to context items to save a db lookup later if needed
      context.Items[Constants.CurrentUserKey] = user;
    }

    await _next.Invoke(context);
  }
}

public static class UserMiddlewareExtensions
{
  public static IApplicationBuilder UseUserMiddleware(this IApplicationBuilder builder)
  {
    return builder.UseMiddleware<UserMiddleware>();
  }
}
