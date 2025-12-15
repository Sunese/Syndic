using System.ServiceModel.Syndication;
using System.Xml;
using Microsoft.IdentityModel.Tokens;
using Syndic.ReaderDb.Entities;

namespace Syndic.ReaderService.Rss;

public class RssParser
{
  private readonly ILogger<RssParser> logger;
  private readonly HttpClient httpClient = new();

  public RssParser(ILogger<RssParser> logger)
  {
    this.logger = logger;
  }

  public async Task<FetchChannelResult> Parse(Uri uri, Subscription subscription)
  {
    try
    {
      logger.LogDebug("Parsing RSS Feed: {Uri}", uri);
      using var reader = XmlReader.Create(uri.ToString());
      var feed = SyndicationFeed.Load(reader);
      if (feed is null)
      {
        throw new InvalidOperationException("Failed to parse RSS feed.");
      }

      return FetchChannelResult.CreateSuccess(new ChannelDto(feed, await GetImageUrl(uri, feed)), subscription);
    }
    catch (Exception ex)
    {
      return FetchChannelResult.CreateFailure(ex, subscription);
    }
  }

  private async Task<Uri?> GetImageUrl(Uri feedUrl, SyndicationFeed feed) => feed.ImageUrl ?? await GetFavIconUrl(feedUrl);

  private async Task<Uri?> GetFavIconUrl(Uri uri)
  {
    var iconUrl = new Uri(uri, "favicon.ico");
    // test that the icon exists
    var res = await httpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, iconUrl));
    if (res.StatusCode != System.Net.HttpStatusCode.OK)
    {
      return null;
    }
    return iconUrl;
  }
}


public class FetchChannelResult
{
  public bool Success { get; }
  public string? ChannelTitle { get; private set; }
  public string? CustomTitle { get; private set; }
  public ChannelDto? Channel { get; }
  public string? ErrorMessage { get; }

  private FetchChannelResult(bool success, string? channelTitle, string? customTitle, ChannelDto? channel, string? errorMessage)
  {
    Success = success;
    ChannelTitle = channelTitle;
    CustomTitle = customTitle;
    Channel = channel;
    ErrorMessage = errorMessage;
  }

  public static FetchChannelResult CreateSuccess(ChannelDto channel, Subscription subscription) => new(true, subscription.ChannelTitle, subscription.CustomTitle, channel, null);
  public static FetchChannelResult CreateFailure(Exception exception, Subscription subscription) => new(false, subscription.ChannelTitle, subscription.CustomTitle, null, exception.Message);
}
