using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Syndic.Tests.Integration.Helpers;

namespace Syndic.Tests.Integration;

/// <summary>
/// Integration tests for <c>GET/POST/DELETE /api/subscriptions</c>.
/// Each test uses a unique user email to stay isolated within the shared InMemory database.
/// </summary>
public class SubscriptionEndpointsTests : IClassFixture<SyndicWebApplicationFactory>
{
    private readonly SyndicWebApplicationFactory _factory;

    public SubscriptionEndpointsTests(SyndicWebApplicationFactory factory)
    {
        _factory = factory;
        _factory.RssHandler.ResponseContent = FakeRssHandler.ValidRssXml;
        _factory.RssHandler.StatusCode = HttpStatusCode.OK;
    }

    // ── GET /api/subscriptions ────────────────────────────────────────────────

    [Fact]
    public async Task GetSubscriptions_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetSubscriptions_WithNoSubscriptions_ReturnsEmptyList()
    {
        var email = $"sub-empty-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var subs = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        subs.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSubscriptions_WithExistingSubscriptions_ReturnsList()
    {
        var email = $"sub-list-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed1", "Feed One");
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed2", "Feed Two");
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        body.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetSubscriptions_ReturnsCorrectFields()
    {
        var email = $"sub-fields-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed", "My Feed");
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.GetAsync("/api/subscriptions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();
        var sub = body![0];

        sub.GetProperty("channelUrl").GetString().Should().Contain("example.com");
        sub.GetProperty("channelTitle").GetString().Should().Be("My Feed");
        sub.GetProperty("id").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetSubscriptions_DoesNotReturnOtherUsersSubscriptions()
    {
        var emailA = $"sub-usera-{Guid.NewGuid()}@example.com";
        var emailB = $"sub-userb-{Guid.NewGuid()}@example.com";
        var userA = await _factory.SeedUserAsync(emailA);
        await _factory.SeedSubscriptionAsync(userA.Id, "https://example.com/feedA");
        await _factory.SeedSubscriptionAsync(userA.Id, "https://example.com/feedA2");
        await _factory.SeedUserAsync(emailB); // B has no subscriptions
        var clientB = _factory.CreateAuthenticatedClient(emailB);

        var response = await clientB.GetAsync("/api/subscriptions");
        var body = await response.Content.ReadFromJsonAsync<JsonElement[]>();

        body.Should().BeEmpty();
    }

    // ── POST /api/subscriptions ───────────────────────────────────────────────

    [Fact]
    public async Task CreateSubscription_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync(
            "/api/subscriptions", new { url = "https://example.com/feed" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateSubscription_WithValidUrl_Returns201()
    {
        var email = $"sub-create-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.PostAsJsonAsync(
            "/api/subscriptions", new { url = "https://example.com/feed" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateSubscription_WithValidUrl_AppearsInGetSubscriptions()
    {
        var email = $"sub-appears-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        await client.PostAsJsonAsync(
            "/api/subscriptions", new { url = "https://example.com/feed" });

        var getResponse = await client.GetAsync("/api/subscriptions");
        var subs = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        subs.Should().HaveCount(1);
    }

    [Fact]
    public async Task CreateSubscription_WithCustomTitle_StoredAndReturned()
    {
        var email = $"sub-custom-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        await client.PostAsJsonAsync("/api/subscriptions",
            new { url = "https://example.com/feed", customTitle = "My Custom Name" });

        var getResponse = await client.GetAsync("/api/subscriptions");
        var subs = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        subs![0].GetProperty("customTitle").GetString().Should().Be("My Custom Name");
    }

    [Fact]
    public async Task CreateSubscription_WithInvalidUrl_Returns400()
    {
        var email = $"sub-badurl-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.PostAsJsonAsync(
            "/api/subscriptions", new { url = "not-a-url" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSubscription_Duplicate_Returns409()
    {
        var email = $"sub-dup-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);
        var body = new { url = "https://example.com/feed" };

        await client.PostAsJsonAsync("/api/subscriptions", body);
        var second = await client.PostAsJsonAsync("/api/subscriptions", body);

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateSubscription_WhenFeedCannotBeParsed_Returns400()
    {
        _factory.RssHandler.ResponseContent = FakeRssHandler.InvalidXml;
        var email = $"sub-parsefail-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.PostAsJsonAsync(
            "/api/subscriptions", new { url = "https://example.com/broken" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Error while parsing RSS Feed");
    }

    [Fact]
    public async Task CreateSubscription_ParsedChannelTitle_StoredFromFeed()
    {
        // FakeRssHandler returns "Test Channel" as the feed title
        var email = $"sub-title-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        await client.PostAsJsonAsync(
            "/api/subscriptions", new { url = "https://example.com/feed" });

        var getResponse = await client.GetAsync("/api/subscriptions");
        var subs = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        subs![0].GetProperty("channelTitle").GetString().Should().Be("Test Channel");
    }

    // ── DELETE /api/subscriptions/{id} ───────────────────────────────────────

    [Fact]
    public async Task DeleteSubscription_WithoutAuth_Returns401()
    {
        var response = await _factory.CreateClient()
            .DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeleteSubscription_WithValidId_Returns200()
    {
        var email = $"sub-delete-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        var sub = await _factory.SeedSubscriptionAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.DeleteAsync($"/api/subscriptions/{sub.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DeleteSubscription_WithValidId_RemovedFromGetSubscriptions()
    {
        var email = $"sub-delete-check-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        var sub = await _factory.SeedSubscriptionAsync(user.Id);
        var client = _factory.CreateAuthenticatedClient(email);

        await client.DeleteAsync($"/api/subscriptions/{sub.Id}");

        var getResponse = await client.GetAsync("/api/subscriptions");
        var subs = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        subs.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSubscription_WithNonExistentId_Returns400()
    {
        var email = $"sub-delmissing-{Guid.NewGuid()}@example.com";
        var client = _factory.CreateAuthenticatedClient(email);

        var response = await client.DeleteAsync($"/api/subscriptions/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteSubscription_OnlyDeletesTargetedSubscription()
    {
        var email = $"sub-delone-{Guid.NewGuid()}@example.com";
        var user = await _factory.SeedUserAsync(email);
        var sub1 = await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed1");
        await _factory.SeedSubscriptionAsync(user.Id, "https://example.com/feed2");
        var client = _factory.CreateAuthenticatedClient(email);

        await client.DeleteAsync($"/api/subscriptions/{sub1.Id}");

        var getResponse = await client.GetAsync("/api/subscriptions");
        var subs = await getResponse.Content.ReadFromJsonAsync<JsonElement[]>();
        subs.Should().HaveCount(1);
    }
}
