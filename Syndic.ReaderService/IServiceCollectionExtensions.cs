using Microsoft.OpenApi;

namespace Syndic.ReaderService;

public static class IServiceCollectionExtensions
{
  public static WebApplicationBuilder AddOpenApi(this WebApplicationBuilder builder)
  {
    builder.Services.AddOpenApi(options =>
    {
      options.AddDocumentTransformer((document, context, cancellationToken) =>
      {
        var oauth2Scheme = new OpenApiSecurityScheme
        {
          Type = SecuritySchemeType.OAuth2,
          Flows = new OpenApiOAuthFlows()
          {

            AuthorizationCode = new OpenApiOAuthFlow()
            {
              AuthorizationUrl = new Uri("https://auth.suneslilleserver.dk/application/o/authorize/"),
              TokenUrl = new Uri("https://auth.suneslilleserver.dk/application/o/token/"),
              RefreshUrl = new Uri("https://auth.suneslilleserver.dk/application/o/token/"),
              // When a client does not request any scopes, authentik will treat the request as if all configured scopes were requested.
              Scopes = new Dictionary<string, string>
              {
              }
            }
          }
        };

        // Add the security scheme at the document level
        var securitySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
          ["Oauth2"] = oauth2Scheme
        };
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes = securitySchemes;

        // Apply it as a requirement for all operations
        foreach (var operation in document.Paths.Values.SelectMany(path => path.Operations))
        {
          operation.Value.Security ??= [];
          operation.Value.Security.Add(new OpenApiSecurityRequirement
          {
            [new OpenApiSecuritySchemeReference("Oauth2", document)] = []
          });
        }

        return Task.CompletedTask;
      });
    });

    return builder;
  }
}
