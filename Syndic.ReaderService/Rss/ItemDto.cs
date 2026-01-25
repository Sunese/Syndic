using System.ServiceModel.Syndication;
using System.Xml.Linq;

namespace Syndic.ReaderService.Rss;

/// <summary>
/// Application DTO representing an individual RSS feed item (e.g. article).
/// </summary>
public class ItemDto
{
  public string? Title { get; }
  public string? Description { get; }
  public string? Summary { get; }
  public string? Author { get; }
  public DateTimeOffset? PublishedAt { get; }
  public string? Link { get; }
  public Uri? ImageUrl { get; }

  private ItemDto(string? title = null, string? description = null, string? summary = null, string? author = null, DateTimeOffset? publishedAt = null, string? link = null, Uri? imageUrl = null)
  {
    Title = title;
    Description = description;
    Summary = summary;
    Author = author;
    PublishedAt = publishedAt;
    Link = link;
    ImageUrl = imageUrl;
  }

  public static bool TryCreate(SyndicationItem item, out string? warning, out string? error, out ItemDto? itemDto)
  {
    // at least a title or description must be present (as per RSS 2.0 spec)
    string? title = item.Title.Text;
    string? description = item.Summary?.Text;
    if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(description))
    {
      itemDto = null;
      warning = null;
      error = "Syndication item is missing both title and description.";
      return false;
    }

    string? summary = item.Summary?.Text;
    string? author = item.Authors.FirstOrDefault()?.Name;
    DateTimeOffset? publishedAt = item.PublishDate != DateTimeOffset.MinValue ? item.PublishDate : null;
    string? link = item.Links.FirstOrDefault(x => x.RelationshipType == "alternate")?.Uri.ToString();
    string? url = item.ElementExtensions.FirstOrDefault(x => x.OuterName == "content" && x.OuterNamespace == "http://search.yahoo.com/mrss/")?.GetObject<XElement>()?.Attribute("url")?.Value;

    Uri? imageUrl = null;
    string? urlWarning = null;
    if (Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
    {
      imageUrl = uri;
    }
    else
    {
      urlWarning = "Syndication item has an invalid media:content URL.";
    }

    error = null;
    warning = urlWarning; // NOTE if we had other warnings, we could concatenate them here
    itemDto = new ItemDto(title, description, summary, author, publishedAt, link, imageUrl);
    return true;
  }
}
