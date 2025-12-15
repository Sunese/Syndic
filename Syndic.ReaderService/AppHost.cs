using Syndic.ServiceDefaults;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Syndic.ReaderService.Endpoints;
using Microsoft.IdentityModel.Tokens;
using Syndic.ReaderService;
using Syndic.ReaderService.Middleware;
using Syndic.ReaderDb;
using Syndic.ReaderService.Rss;

var builder = WebApplication.CreateBuilder(args);

// var logger = LoggerFactory.Create(config =>
// {
//   config.AddConsole();
//   config.SetMinimumLevel(LogLevel.Debug);
// }).CreateLogger("Program");

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ReaderDbContext>("readerdb");

builder.Services.AddSingleton<RssParser>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
      options.Authority = "https://auth.suneslilleserver.dk/application/o/rss-reader-test/";
      options.RequireHttpsMetadata = true;
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidateAudience = false, // dont need to for now. but maybe consider it.
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = "groups" // groups in authentik == roles as I have set it up for now
      };
      options.SaveToken = true;
      options.Events = new JwtBearerEvents()
      {
        OnTokenValidated = c =>
        {
          var logger = c.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JWT Auth");
          var authHeader = c.Request.Headers.Authorization.Single();
          logger.LogInformation("Validated token from Authorization header: {AuthorizationHeader}", authHeader);
          return Task.CompletedTask;
        }
      };
    });

builder.Services.AddAuthorization(x =>
{
  x.AddPolicy("MustBeReader", policy =>
  {
    policy.RequireAuthenticatedUser();
    policy.RequireRole("rss_users");
  });

  x.AddPolicy("MustBeAdmin", policy =>
  {
    policy.RequireAuthenticatedUser();
    policy.RequireRole("rss_admins");
  });

  x.DefaultPolicy = x.GetPolicy("MustBeReader") ?? throw new InvalidOperationException("Default policy 'MustBeReaderUser' not found");
  x.FallbackPolicy = x.DefaultPolicy;
});

// Web/Presentation
builder.AddOpenApi();

var app = builder.Build();

app.MapFeedEndpoints();
app.MapSubscriptionsEndpoints();

if (app.Environment.IsDevelopment())
{
  // We use only SwaggerUI to generate the UI/client
  // No swashbuckle middleware is used
  // The OpenAPI document is generated with services.AddOpenApi();
  app.UseSwaggerUI(opts =>
  {
    opts.SwaggerEndpoint("/openapi/v1.json", "v1");
  });
  app.MapOpenApi().AllowAnonymous();
}

app.UseAuthentication();
app.UseAuthorization();

app.UseUserMiddleware();

app.MapDefaultEndpoints();

app.Run();
