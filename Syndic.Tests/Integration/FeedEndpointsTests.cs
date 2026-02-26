using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Syndic.Tests.Integration.Helpers;

namespace Syndic.Tests.Integration;

/// <summary>
/// Integration tests for <c>GET /api/feed</c>.
/// Each test uses a unique user email to stay isolated within the shared InMemory database.
/// </summary>
public class FeedEndpointsTests : IClassFixture<SyndicWebApplicationFactory>
{
    private readonly SyndicWebApplicationFactory _factory;

    public FeedEndpointsTests(SyndicWebApplicationFactory factory)
    {
        _factory = factory;
        // Reset the fake RSS handler to a known good state before every test class run
        _factory.RssHandler.ResponseContent = FakeRssHandler.ValidRssXml;
        _factory.RssHandler.StatusCode = HttpStatusCode.OK;
    }

    // ── authentication ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetFeed_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── authorised, no subscriptions ─────────────────────────────────────────

    [Fact]
    public async Task GetFeed_WithNoSubscriptions_ReturnsEmptyChannels()
    {
        var email = $"feed-empty-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("channels").GetArrayLength().Should().Be(0);
    }

    // ── authorised, with subscriptions ───────────────────────────────────────

    [Fact]
    public async Task GetFeed_WithOneSubscription_ReturnsOneChannel()
    {
        var email = $"feed-one-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed1");
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("channels").GetArrayLength().Should().Be(1);
    }

    [Fact]
    public async Task GetFeed_WithMultipleSubscriptions_ReturnsAllChannels()
    {
        var email = $"feed-multi-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed1");
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed2");
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed3");
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/feed");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("channels").GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task GetFeed_WithSuccessfulParse_ChannelHasSuccessTrue()
    {
        var email = $"feed-success-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/feed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var channel = body.GetProperty("channels")[0];

        channel.GetProperty("success").GetBoolean().Should().BeTrue();
        channel.GetProperty("channel").GetProperty("title").GetString()
            .Should().Be("Test Channel");
    }

    [Fact]
    public async Task GetFeed_WhenFeedFails_ChannelHasSuccessFalse()
    {
        // Configure fake handler to return bad XML for this test
        _factory.RssHandler.ResponseContent = FakeRssHandler.InvalidXml;

        var email = $"feed-fail-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/feed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var channel = body.GetProperty("channels")[0];

        channel.GetProperty("success").GetBoolean().Should().BeFalse();
        channel.GetProperty("errorMessage").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetFeed_DoesNotReturnOtherUsersSubscriptions()
    {
        // User A has 2 subscriptions; User B has 1
        var emailA = $"feed-usera-{Guid.NewGuid()}@example.com";
        var emailB = $"feed-userb-{Guid.NewGuid()}@example.com";
        var userA = await _factory.SeedUserAsync(emailA);
        await _factory.SeedSubscriptionAsync(userA.Id, "https://example.com/feedA1");
        await _factory.SeedSubscriptionAsync(userA.Id, "https://example.com/feedA2");
        var userB = await _factory.SeedUserAsync(emailB);
        await _factory.SeedSubscriptionAsync(userB.Id, "https://example.com/feedB1");

        var clientB = _factory.CreateAuthenticatedClient(emailB);
        var response = await clientB.GetAsync("/api/feed");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        body.GetProperty("channels").GetArrayLength().Should().Be(1);
    }
}
