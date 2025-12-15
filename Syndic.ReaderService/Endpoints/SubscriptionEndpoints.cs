using Syndic.ReaderDb;
using Syndic.ReaderDb.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Syndic.ReaderService.Rss;

namespace Syndic.ReaderService.Endpoints;

public static class SubscriptionEndpoints
{
  public static IEndpointRouteBuilder MapSubscriptionsEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/subscriptions")
      .WithTags("Subscriptions")
      .RequireAuthorization();

    // GET /api/subscriptions
    group.MapGet("/", async (HttpContext context, ReaderDbContext db, CancellationToken cancellationToken) =>
    {
      var user = context.GetUser();

      var subscriptions = await db.Subscriptions
        .Where(s => s.UserId == user.Id)
        .ToListAsync(cancellationToken);

      var dto = subscriptions.Select(s => new SubscriptionResponse(
        Id: s.Id,
        ChannelUrl: s.ChannelUrl.ToString(),
        CustomTitle: s.CustomTitle,
        ChannelTitle: s.ChannelTitle,
        SubscribedAt: s.SubscribedAt
      )).ToList();

      return Results.Ok(dto);
    })
    .WithName("GetSubscriptions")
    .WithSummary("Get all subscriptions for the current user")
    .Produces<List<SubscriptionResponse>>(StatusCodes.Status200OK);

    // POST /api/subscriptions
    group.MapPost("/", async ([FromBody] CreateSubscriptionRequest request, HttpContext context, ReaderDbContext db, RssParser rssParser, CancellationToken cancellationToken) =>
    {
      if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var uri))
        return Results.BadRequest("Invalid URL format.");

      var user = context.GetUser();

      var existingSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.UserId == user.Id && s.ChannelUrl == uri, cancellationToken);
      if (existingSub is not null)
      {
        return Results.Conflict("Already subscribed to this feed.");
      }

      var tempSub = new Subscription(user.Id,
                                          uri,
                                          null,
                                          null);
      var result = await rssParser.Parse(uri, tempSub);

      if (!result.Success)
        return Results.BadRequest($"Error while parsing RSS Feed: {result.ErrorMessage}");

      var subscription = new Subscription(user.Id,
                                          uri,
                                          channelTitle: result.Channel!.Title,
                                          customTitle: request.CustomTitle);
      await db.Subscriptions.AddAsync(subscription, cancellationToken);
      await db.SaveChangesAsync(cancellationToken);

      return Results.Created();
    })
    .WithName("CreateSubscription")
    .WithSummary("Create a new subscription for the current user")
    .Produces<Subscription>(StatusCodes.Status201Created);

    group.MapDelete("/{id}", async ([FromRoute] Guid id, HttpContext httpCtx, ReaderDbContext db, CancellationToken ct) =>
    {
      var user = httpCtx.GetUser();

      var existingSub = await db.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, ct);
      if (existingSub is null)
        return Results.BadRequest("User is not subscribed to given ID");

      db.Subscriptions.Remove(existingSub);
      await db.SaveChangesAsync(ct);

      return Results.Ok("Deleted");

    })
    .WithName("DeleteSubscription")
    .Produces(200);

    return app;
  }
}

record CreateSubscriptionRequest(string Url, string? CustomTitle);
record SubscriptionResponse(Guid Id, string? ChannelTitle, string? CustomTitle, string ChannelUrl, DateTimeOffset SubscribedAt);
