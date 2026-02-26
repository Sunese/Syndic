using System.ServiceModel.Syndication;
using FluentAssertions;
using Syndic.ReaderService.Rss;

namespace Syndic.Tests.Unit.Rss;

public class ChannelDtoTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static SyndicationFeed ValidFeed(
        string title = "Test Channel",
        string description = "Test Description",
        string link = "https://example.com") =>
        new(title, description, new Uri(link));

    private static SyndicationItem ValidItem(string title = "Item Title") =>
        new(title, "Item Content", new Uri("https://example.com/item"));

    // ── success cases ─────────────────────────────────────────────────────────

    [Fact]
    public void TryCreate_WithValidFeed_ReturnsSuccess()
    {
        var feed = ValidFeed();

        var result = ChannelDto.TryCreate(feed, out var warning, out var error, out var dto);

        result.Should().BeTrue();
        warning.Should().BeNull();
        error.Should().BeNull();
        dto.Should().NotBeNull();
    }

    [Fact]
    public void TryCreate_MapsTitle()
    {
        var feed = ValidFeed(title: "My Tech Blog");

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Title.Should().Be("My Tech Blog");
    }

    [Fact]
    public void TryCreate_MapsLink()
    {
        var feed = ValidFeed(link: "https://blog.example.com");

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Link.Should().Be(new Uri("https://blog.example.com"));
    }

    [Fact]
    public void TryCreate_MapsDescription()
    {
        var feed = ValidFeed(description: "A blog about things");

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Description.Should().Be("A blog about things");
    }

    [Fact]
    public void TryCreate_WithNullDescription_UsesEmptyString()
    {
        var feed = new SyndicationFeed();
        feed.Title = new TextSyndicationContent("Title");
        feed.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("https://example.com")));
        // Description intentionally not set (null)

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Description.Should().BeEmpty();
    }

    [Fact]
    public void TryCreate_WithLastUpdatedTime_MapsPublished()
    {
        var feed = ValidFeed();
        var updated = new DateTimeOffset(2025, 2, 26, 12, 0, 0, TimeSpan.Zero);
        feed.LastUpdatedTime = updated;

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Published.Should().Be(updated);
    }

    [Fact]
    public void TryCreate_WithMinValueLastUpdatedTime_PublishedIsNull()
    {
        var feed = ValidFeed();
        // LastUpdatedTime defaults to DateTimeOffset.MinValue

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Published.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithAbsoluteImageUrl_MapsImageUrl()
    {
        var feed = ValidFeed();
        feed.ImageUrl = new Uri("https://example.com/logo.png");

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.ImageUrl.Should().Be(new Uri("https://example.com/logo.png"));
    }

    [Fact]
    public void TryCreate_WithRelativeImageUrl_ImageUrlIsNull()
    {
        var feed = ValidFeed();
        feed.ImageUrl = new Uri("/logo.png", UriKind.Relative);

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithNoImageUrl_ImageUrlIsNull()
    {
        var feed = ValidFeed();

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.ImageUrl.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithItems_MapsAllItems()
    {
        var feed = ValidFeed();
        feed.Items = [ValidItem("First"), ValidItem("Second"), ValidItem("Third")];

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Items.Should().HaveCount(3);
    }

    [Fact]
    public void TryCreate_WithNoItems_ItemsIsEmpty()
    {
        var feed = ValidFeed();

        ChannelDto.TryCreate(feed, out _, out _, out var dto);

        dto!.Items.Should().BeEmpty();
    }

    // ── failure cases ─────────────────────────────────────────────────────────

    [Fact]
    public void TryCreate_WithInvalidItem_ReturnsFalse()
    {
        var feed = ValidFeed();
        // An item with empty title and no summary is invalid per RSS 2.0
        var badItem = new SyndicationItem();
        badItem.Title = new TextSyndicationContent("");
        feed.Items = [badItem];

        var result = ChannelDto.TryCreate(feed, out _, out var error, out var dto);

        result.Should().BeFalse();
        dto.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryCreate_FeedWithNoLinks_Throws()
    {
        var feed = new SyndicationFeed();
        feed.Title = new TextSyndicationContent("No Link Feed");
        // No links added

        var act = () => ChannelDto.TryCreate(feed, out _, out _, out _);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*link*");
    }
}
