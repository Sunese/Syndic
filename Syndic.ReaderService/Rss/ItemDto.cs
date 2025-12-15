using System.ServiceModel.Syndication;

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

  public ItemDto(SyndicationItem item)
  {
    Title = item.Title.Text;
    Description = item.Summary?.Text;
    Summary = item.Summary?.Text;
    Author = item.Authors.FirstOrDefault()?.Name;
    PublishedAt = item.PublishDate != DateTimeOffset.MinValue ? item.PublishDate : null;
    Link = item.Links.FirstOrDefault(x => x.RelationshipType == "alternate")?.Uri.ToString();
  }
}
