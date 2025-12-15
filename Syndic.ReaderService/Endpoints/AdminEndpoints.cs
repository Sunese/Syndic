namespace Syndic.ReaderService.Endpoints;

public static class AdminEndpoints
{
  public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
  {
    var group = app.MapGroup("/api/admin")
      .WithTags("Admin")
      .RequireAuthorization("MustBeAdmin");

    // GET /api/admin
    // This does nothing special for now..
    group.MapGet("/", async (HttpContext ctx, CancellationToken cancellationToken) =>
    {
      var user = ctx.Items["CurrentUser"] as Syndic.ReaderDb.Entities.User;
      if (user is null)
      {
        return Results.Unauthorized();
      }
      return Results.Ok();
    })
    .WithName("GetAdmin")
    .WithSummary("Get admin information");

    return app;
  }
}

