using System.Net;
using System.Text;

namespace Syndic.Tests.Integration.Helpers;

/// <summary>
/// A fake <see cref="HttpMessageHandler"/> that serves configurable RSS/Atom XML without
/// making any real network requests. Used to isolate integration tests from external feeds.
/// </summary>
public class FakeRssHandler : HttpMessageHandler
{
    public static readonly string ValidRssXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0">
          <channel>
            <title>Test Channel</title>
            <link>https://example.com</link>
            <description>A fake RSS feed for testing</description>
            <lastBuildDate>Wed, 26 Feb 2025 12:00:00 GMT</lastBuildDate>
            <item>
              <title>Test Item One</title>
              <description>Description for item one</description>
              <link>https://example.com/item-1</link>
              <pubDate>Wed, 26 Feb 2025 10:00:00 GMT</pubDate>
            </item>
            <item>
              <title>Test Item Two</title>
              <description>Description for item two</description>
              <link>https://example.com/item-2</link>
              <pubDate>Wed, 26 Feb 2025 09:00:00 GMT</pubDate>
            </item>
          </channel>
        </rss>
        """;

    public static readonly string InvalidXml = "this is not xml at all";

    /// <summary>Content returned for all requests. Defaults to valid RSS.</summary>
    public string ResponseContent { get; set; } = ValidRssXml;

    /// <summary>HTTP status code returned. Defaults to 200 OK.</summary>
    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(StatusCode)
        {
            Content = new StringContent(ResponseContent, Encoding.UTF8, "application/rss+xml")
        };
        return Task.FromResult(response);
    }
}
