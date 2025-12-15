using Syndic.ReaderDb;
using Microsoft.EntityFrameworkCore;
using Syndic.ReaderService.Rss;

namespace Syndic.ReaderService.Endpoints;

public static class FeedEndpoints
{
  public static IEndpointRouteBuilder MapFeedEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/feed")
      .WithTags("Feed")
      .RequireAuthorization();

    // GET /api/feed
    group.MapGet("/", async (HttpContext ctx, ReaderDbContext dbContext, RssParser rssParser, CancellationToken cancellationToken) =>
    {
      var user = ctx.Items["CurrentUser"] as ReaderDb.Entities.User;
      if (user is null)
      {
        return Results.Unauthorized();
      }
      var subscriptions = await dbContext.Subscriptions.Where(x => x.UserId == user.Id).ToListAsync(cancellationToken: cancellationToken);
      var channels = await Task.WhenAll(subscriptions.Select(s => rssParser.Parse(s.ChannelUrl, s)));
      return Results.Ok(new FeedResponse(channels));
    })
    .WithName("GetFeed")
    .WithSummary("Get the user's feed")
    .WithDescription("For each subscription, the latest items are fetched from the respective RSS feed.")
    .Produces<FeedResponse>();

    return app;
  }
}

public record FeedResponse(FetchChannelResult[] Channels);
