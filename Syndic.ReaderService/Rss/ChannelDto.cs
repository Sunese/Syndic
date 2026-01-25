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

  private ChannelDto(string title, Uri link, string description, DateTimeOffset? published, IEnumerable<ItemDto> items, Uri? imageUrl)
  {
    Title = title;
    Link = link;
    Description = description;
    Published = published;
    Items = items;
    ImageUrl = imageUrl;
  }

  public static bool TryCreate(SyndicationFeed feed, out string? warning, out string? error, out ChannelDto? channelDto)
  {
    var title = feed.Title.Text;
    var link = feed.Links.FirstOrDefault()?.Uri ?? throw new InvalidOperationException("Feed has no link.");
    var description = feed.Description?.Text ?? string.Empty;
    DateTimeOffset? published = feed.LastUpdatedTime != DateTimeOffset.MinValue ? feed.LastUpdatedTime : null;
    Uri? imageUrl = feed.ImageUrl != null && Uri.IsWellFormedUriString(feed.ImageUrl.ToString(), UriKind.Absolute) ? feed.ImageUrl : null;
    // allocate list for items
    var items = new List<ItemDto>(feed.Items.Count());

    foreach (var item in feed.Items)
    {
      if (!ItemDto.TryCreate(item, out var itemWarning, out var itemError, out ItemDto? itemDto))
      {
        channelDto = null;
        warning = itemWarning;
        error = itemError;
        return false;
      }

      items.Add(itemDto!); // "trust me bro", itemDto should never be null here
    }

    warning = null;
    error = null;
    channelDto = new ChannelDto(title, link, description, published, items, imageUrl);
    return true;
  }
}
