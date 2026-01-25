using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Syndic.ReaderDb;
using Syndic.ReaderService.Endpoints;
using Syndic.ReaderService.Middleware;
using Syndic.ReaderService.Rss;
using Syndic.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: false, reloadOnChange: true);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<ReaderDbContext>("syndicdb");

builder.Services.AddSingleton<RssParser>();

var internalJwtSecret = builder.Configuration["INTERNAL_JWT_SECRET"];
System.Console.WriteLine("INTERNAL_JWT_SECRET: " + internalJwtSecret);
if (string.IsNullOrEmpty(internalJwtSecret))
{
  throw new InvalidOperationException("INTERNAL_JWT_SECRET is not set ");
}

var key = new SymmetricSecurityKey(
  Encoding.UTF8.GetBytes(internalJwtSecret)
);
key.KeyId = "internal-auth";

IdentityModelEventSource.ShowPII = true;

builder.Services
  .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
  .AddJwtBearer(options =>
  {
    options.TokenValidationParameters = new TokenValidationParameters
    {
      ValidateIssuer = true,
      ValidIssuer = "internal-auth",
      ValidateAudience = true,
      ValidAudience = "aspnet-api",
      ValidateLifetime = true,
      ValidateIssuerSigningKey = true,
      IssuerSigningKey = key,
      ClockSkew = TimeSpan.FromSeconds(30)
    };
    options.SaveToken = true;
    options.Events = new JwtBearerEvents
    {
      OnTokenValidated = c =>
      {
        var logger = c.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JWT Auth");
        var authHeader = c.Request.Headers.Authorization.Single();
        logger.LogInformation("Validated token from Authorization header: {AuthorizationHeader}", authHeader);
        return Task.CompletedTask;
      },
      OnAuthenticationFailed = c =>
      {
        var logger = c.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JWT Auth");
        var authHeader = c.Request.Headers.Authorization.Single();
        logger.LogWarning("Authentication failed: {AuthorizationHeader} {ExceptionMessage}", authHeader,
          c.Exception.Message);
        return Task.CompletedTask;
      },
      OnForbidden = c =>
      {
        var logger = c.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JWT Auth");
        var authHeader = c.Request.Headers.Authorization.Single();
        logger.LogWarning("Forbidden request: {AuthorizationHeader}", authHeader);
        return Task.CompletedTask;
      }
    };
  });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

var app = builder.Build();

app.MapFeedEndpoints();
app.MapSubscriptionsEndpoints();
app.MapDefaultEndpoints();

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

app.Run();
