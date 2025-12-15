using System.ServiceModel.Syndication;

namespace Syndic.ReaderService.Rss;

/// <summary>
/// Application DTO representing an RSS/Atom channel/feed source.
/// </summary>
public class ChannelDto
{
  /// <summary>
  /// The name of the channel. It's how people refer to your service. 
  /// If you have an HTML website that contains the same information as your RSS file, 
  /// the title of your channel should be the same as the title of your website.
  /// </summary>
  public string Title { get; }

  /// <summary>
  /// The URL to the HTML website corresponding to the channel.
  /// </summary>
  public Uri Link { get; }

  /// <summary>
  /// Phrase or sentence describing the channel.
  /// </summary>
  public string Description { get; }

  public DateTimeOffset? Published { get; }
  public IEnumerable<ItemDto> Items { get; } = [];
  public Uri? ImageUrl { get; }

  public ChannelDto(SyndicationFeed feed, Uri? imageUrl)
  {
    Title = feed.Title.Text;
    Link = feed.Links.FirstOrDefault()?.Uri ?? throw new InvalidOperationException("Feed has no link.");
    Description = feed.Description?.Text ?? string.Empty;
    Published = feed.LastUpdatedTime != DateTimeOffset.MinValue ? feed.LastUpdatedTime : null;
    ImageUrl = imageUrl;
    Items = feed.Items.Select(item => new ItemDto(item));
  }
}
