using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Syndic.ReaderDb.Entities;
using Syndic.ReaderService.Rss;
using Syndic.Tests.Integration.Helpers;

namespace Syndic.Tests.Unit.Rss;

public class RssParserTests
{
    private static RssParser BuildParser(HttpMessageHandler handler) =>
        new(NullLogger<RssParser>.Instance, new HttpClient(handler));

    private static Subscription FakeSubscription(string url = "https://example.com/feed") =>
        new(Guid.NewGuid(), new Uri(url), "Feed Title");

    // ── success ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Parse_WithValidRssFeed_ReturnsSuccessResult()
    {
        var parser = BuildParser(new FakeRssHandler());
        var sub = FakeSubscription();

        var result = await parser.Parse(new Uri("https://example.com/feed"), sub);

        result.Success.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.Channel.Should().NotBeNull();
    }

    [Fact]
    public async Task Parse_WithValidRssFeed_PopulatesChannelTitle()
    {
        var parser = BuildParser(new FakeRssHandler());
        var sub = FakeSubscription();

        var result = await parser.Parse(new Uri("https://example.com/feed"), sub);

        result.Channel!.Title.Should().Be("Test Channel");
    }

    [Fact]
    public async Task Parse_WithValidRssFeed_PopulatesItems()
    {
        var parser = BuildParser(new FakeRssHandler());
        var sub = FakeSubscription();

        var result = await parser.Parse(new Uri("https://example.com/feed"), sub);

        result.Channel!.Items.Should().HaveCount(2);
    }

    [Fact]
    public async Task Parse_PreservesSubscriptionTitlesOnResult()
    {
        var parser = BuildParser(new FakeRssHandler());
        var sub = new Subscription(Guid.NewGuid(), new Uri("https://example.com/feed"),
            channelTitle: "My Channel",
            customTitle: "My Custom Title");

        var result = await parser.Parse(sub.ChannelUrl, sub);

        result.ChannelTitle.Should().Be("My Channel");
        result.CustomTitle.Should().Be("My Custom Title");
    }

    // ── failure ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Parse_WithInvalidXml_ReturnsFailureResult()
    {
        var handler = new FakeRssHandler { ResponseContent = FakeRssHandler.InvalidXml };
        var parser = BuildParser(handler);
        var sub = FakeSubscription();

        var result = await parser.Parse(new Uri("https://example.com/feed"), sub);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
        result.Channel.Should().BeNull();
    }

    [Fact]
    public async Task Parse_WithHttpError_ReturnsFailureResult()
    {
        var handler = new FakeRssHandler { StatusCode = System.Net.HttpStatusCode.NotFound };
        var parser = BuildParser(handler);
        var sub = FakeSubscription();

        var result = await parser.Parse(new Uri("https://example.com/feed"), sub);

        result.Success.Should().BeFalse();
        result.Channel.Should().BeNull();
    }

    [Fact]
    public async Task Parse_Failure_PreservesSubscriptionTitles()
    {
        var handler = new FakeRssHandler { ResponseContent = FakeRssHandler.InvalidXml };
        var parser = BuildParser(handler);
        var sub = new Subscription(Guid.NewGuid(), new Uri("https://example.com/feed"),
            channelTitle: "Broken Feed",
            customTitle: "My Broken Feed");

        var result = await parser.Parse(sub.ChannelUrl, sub);

        result.Success.Should().BeFalse();
        result.ChannelTitle.Should().Be("Broken Feed");
        result.CustomTitle.Should().Be("My Broken Feed");
    }
}
