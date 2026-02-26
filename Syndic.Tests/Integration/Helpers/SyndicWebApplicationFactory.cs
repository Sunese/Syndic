using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Syndic.ReaderDb;
using Syndic.ReaderService.Rss;

namespace Syndic.Tests.Integration.Helpers;

/// <summary>
/// A <see cref="WebApplicationFactory{TEntryPoint}"/> configured for integration testing:
/// <list type="bullet">
///   <item>Replaces PostgreSQL with an isolated InMemory database per factory instance.</item>
///   <item>Replaces the <see cref="RssParser"/> HTTP handler with <see cref="FakeRssHandler"/>.</item>
///   <item>Injects a known <c>INTERNAL_JWT_SECRET</c> so tests can mint valid tokens.</item>
/// </list>
/// </summary>
public class SyndicWebApplicationFactory : WebApplicationFactory<Program>
{
    /// <summary>Secret used to sign JWTs in tests. Must match what the app validates.</summary>
    public const string TestJwtSecret = "syndic-integration-test-secret-32chars!!";

    /// <summary>
    /// Shared fake RSS handler. Tests can set <see cref="FakeRssHandler.ResponseContent"/>
    /// or <see cref="FakeRssHandler.StatusCode"/> before making requests.
    /// </summary>
    public FakeRssHandler RssHandler { get; } = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Program.cs reads INTERNAL_JWT_SECRET *before* builder.Build() is called,
        // so ConfigureAppConfiguration is too late. The env var is picked up by
        // WebApplication.CreateBuilder's default providers which load before user code runs.
        Environment.SetEnvironmentVariable("INTERNAL_JWT_SECRET", TestJwtSecret);
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Use Development environment so appsettings.Development.json is loaded
        // (avoids the "optional: false" guard on appsettings.{env}.json)
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Provide a connection string so Aspire's AddNpgsqlDbContext doesn't
                // fail during DI registration (the actual DbContext is replaced below)
                ["ConnectionStrings:syndicdb"] =
                    "Host=localhost;Database=testdb;Username=test;Password=test",
            });
        });

        builder.ConfigureTestServices(services =>
        {
            // ── Replace PostgreSQL DbContext with InMemory ────────────────────────
            // Aspire registers a DbContextPool (singleton) + related services. Remove all
            // descriptors whose generic type argument is ReaderDbContext so the pool,
            // scoped lease, options, and factory are all cleared before we add InMemory.
            var toRemove = services
                .Where(d =>
                    d.ServiceType == typeof(ReaderDbContext) ||
                    (d.ServiceType.IsGenericType &&
                     d.ServiceType.GetGenericArguments().Contains(typeof(ReaderDbContext))))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            // Each factory instance gets its own InMemory database
            var dbName = $"SyndicTests_{Guid.NewGuid()}";
            services.AddDbContext<ReaderDbContext>(options =>
                options.UseInMemoryDatabase(dbName));

            // ── Redirect RssParser's HttpClient to the fake handler ───────────────
            // Adding a second ConfigurePrimaryHttpMessageHandler call on the same named
            // client appends to the builder actions; the last one to set PrimaryHandler wins.
            services.AddHttpClient<RssParser>()
                .ConfigurePrimaryHttpMessageHandler(() => RssHandler);
        });
    }

    /// <summary>
    /// Creates an <see cref="HttpClient"/> with a pre-set Authorization header
    /// containing a valid JWT for the given email/provider.
    /// </summary>
    public HttpClient CreateAuthenticatedClient(
        string email = "test@example.com",
        string provider = "github")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            JwtHelper.BearerHeader(email, provider, TestJwtSecret);
        return client;
    }

    /// <summary>
    /// Seeds a <see cref="ReaderDb.Entities.User"/> directly into the InMemory database,
    /// bypassing the UserMiddleware. Returns the created user so tests can reference its Id.
    /// </summary>
    public async Task<ReaderDb.Entities.User> SeedUserAsync(
        string email,
        string provider = "github")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReaderDbContext>();
        var user = new ReaderDb.Entities.User(email, provider);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    /// <summary>
    /// Seeds a <see cref="ReaderDb.Entities.Subscription"/> for a given user.
    /// </summary>
    public async Task<ReaderDb.Entities.Subscription> SeedSubscriptionAsync(
        Guid userId,
        string url = "https://example.com/feed",
        string channelTitle = "Test Channel")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReaderDbContext>();
        var sub = new ReaderDb.Entities.Subscription(userId, new Uri(url), channelTitle);
        db.Subscriptions.Add(sub);
        await db.SaveChangesAsync();
        return sub;
    }
}
