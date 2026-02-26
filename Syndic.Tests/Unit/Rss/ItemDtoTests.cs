using System.ServiceModel.Syndication;
using System.Xml;
using FluentAssertions;
using Syndic.ReaderService.Rss;

namespace Syndic.Tests.Unit.Rss;

public class ItemDtoTests
{
    // ── helpers ──────────────────────────────────────────────────────────────

    private static SyndicationItem ItemWithTitle(string title)
    {
        var item = new SyndicationItem();
        item.Title = new TextSyndicationContent(title);
        return item;
    }

    private static SyndicationItem ItemWithTitleAndSummary(string title, string summary)
    {
        var item = ItemWithTitle(title);
        item.Summary = new TextSyndicationContent(summary);
        return item;
    }

    private static SyndicationItem MinimalItem() => ItemWithTitle("Minimal Title");

    // ── success cases ─────────────────────────────────────────────────────────

    [Fact]
    public void TryCreate_WithTitleOnly_ReturnsSuccess()
    {
        var item = ItemWithTitle("Hello World");

        var result = ItemDto.TryCreate(item, out var warning, out var error, out var dto);

        result.Should().BeTrue();
        error.Should().BeNull();
        dto.Should().NotBeNull();
        dto!.Title.Should().Be("Hello World");
    }

    [Fact]
    public void TryCreate_WithTitleAndSummary_PopulatesBothFields()
    {
        var item = ItemWithTitleAndSummary("Title", "Some description");

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.Title.Should().Be("Title");
        dto.Summary.Should().Be("Some description");
        dto.Description.Should().Be("Some description"); // same field
    }

    [Fact]
    public void TryCreate_WithEmptyTitleButNonEmptySummary_ReturnsSuccess()
    {
        var item = new SyndicationItem();
        item.Title = new TextSyndicationContent("");   // empty – not null
        item.Summary = new TextSyndicationContent("A real description");

        var result = ItemDto.TryCreate(item, out _, out var error, out var dto);

        result.Should().BeTrue();
        error.Should().BeNull();
        dto!.Description.Should().Be("A real description");
    }

    [Fact]
    public void TryCreate_WithAlternateLink_PopulatesLink()
    {
        var item = MinimalItem();
        item.Links.Add(SyndicationLink.CreateAlternateLink(new Uri("https://example.com/post")));

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.Link.Should().Be("https://example.com/post");
    }

    [Fact]
    public void TryCreate_WithoutAlternateLink_LinkIsNull()
    {
        var item = MinimalItem();

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.Link.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithAuthor_PopulatesAuthor()
    {
        var item = MinimalItem();
        item.Authors.Add(new SyndicationPerson { Name = "Jane Doe" });

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.Author.Should().Be("Jane Doe");
    }

    [Fact]
    public void TryCreate_WithoutAuthor_AuthorIsNull()
    {
        var item = MinimalItem();

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.Author.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithPublishDate_PopulatesPublishedAt()
    {
        var published = new DateTimeOffset(2025, 2, 26, 10, 0, 0, TimeSpan.Zero);
        var item = MinimalItem();
        item.PublishDate = published;

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.PublishedAt.Should().Be(published);
    }

    [Fact]
    public void TryCreate_WithMinValuePublishDate_PublishedAtIsNull()
    {
        var item = MinimalItem();
        // PublishDate defaults to DateTimeOffset.MinValue

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.PublishedAt.Should().BeNull();
    }

    [Fact]
    public void TryCreate_WithValidMediaContentUrl_PopulatesImageUrl()
    {
        var item = MinimalItem();
        var imageUrl = "https://example.com/image.jpg";
        var extension = new SyndicationElementExtension(XElement.Parse(
            $"""<content xmlns="http://search.yahoo.com/mrss/" url="{imageUrl}" />"""));
        item.ElementExtensions.Add(extension);

        ItemDto.TryCreate(item, out var warning, out _, out var dto);

        dto!.ImageUrl.Should().Be(new Uri(imageUrl));
        warning.Should().BeNull(); // valid URL → no warning
    }

    [Fact]
    public void TryCreate_WithInvalidMediaContentUrl_SetsWarningAndNullImageUrl()
    {
        var item = MinimalItem();
        var extension = new SyndicationElementExtension(XElement.Parse(
            """<content xmlns="http://search.yahoo.com/mrss/" url="not-a-valid-url" />"""));
        item.ElementExtensions.Add(extension);

        ItemDto.TryCreate(item, out var warning, out _, out var dto);

        dto!.ImageUrl.Should().BeNull();
        warning.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryCreate_WithNoMediaContent_ImageUrlIsNull()
    {
        var item = MinimalItem();

        ItemDto.TryCreate(item, out _, out _, out var dto);

        dto!.ImageUrl.Should().BeNull();
    }

    // ── failure cases ─────────────────────────────────────────────────────────

    [Fact]
    public void TryCreate_WithEmptyTitleAndNoSummary_ReturnsFalse()
    {
        var item = new SyndicationItem();
        item.Title = new TextSyndicationContent(""); // both empty
        // Summary defaults to null

        var result = ItemDto.TryCreate(item, out _, out var error, out var dto);

        result.Should().BeFalse();
        dto.Should().BeNull();
        error.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void TryCreate_WithWhitespaceTitleAndNoSummary_ReturnsFalse()
    {
        var item = new SyndicationItem();
        item.Title = new TextSyndicationContent("   ");

        var result = ItemDto.TryCreate(item, out _, out var error, out _);

        result.Should().BeFalse();
        error.Should().Contain("missing");
    }
}
