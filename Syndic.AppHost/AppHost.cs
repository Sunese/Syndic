using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

builder.AddDockerComposeEnvironment("syndic")
  .WithDashboard(c =>
  {
    c.WithHostPort(8888);
    c.WithExternalHttpEndpoints();
  });

var syndicAuthentikClientId = builder.AddParameter("Syndic-Frontend-Authentik-Client-ID", secret: true);
var syndicAuthentikClientSecert = builder.AddParameter("Syndic-Frontend-Authentik-Client-Secret", secret: true);
var authentikIssuerUrl = builder.AddParameter("Authentik-Issuer-Url");
var authJsSecret = builder.AddParameter("AuthJS-Secret", secret: true);

var postgresServer = builder.AddPostgres("postgres")
  .WithPgWeb(x => x.WithHostPort(5555))
  .WithDataVolume()
  // as per Aspire 13.0.1, no actual Postgres service health check is generated in the docker compose
  // so we set it up here manually
  // however it works as expected when running locally
  .PublishAsDockerComposeService((res, service) =>
  {
    service.Healthcheck = new()
    {
      Test = ["CMD-SHELL", "pg_isready -U postgres -d reader"],
      Interval = "10s",
      Retries = 5,
      StartPeriod = "30s",
      Timeout = "10s"
    };
  });

var postgresReaderDb = postgresServer.AddDatabase("syndicdb", "reader");

var readerDbManager = builder.AddProject<Projects.Syndic_ReaderDbManager>("syndicworker")
  .WithReference(postgresReaderDb)
  .WaitFor(postgresServer)
  .WaitFor(postgresReaderDb, WaitBehavior.WaitOnResourceUnavailable)
  // As per Aspire 13.0.1, a simple "service_started" is always generated
  // so we overwrite it here
  .PublishAsDockerComposeService((resource, service) =>
  {
    service.DependsOn["postgres"] = new() { Condition = "service_healthy" };
  });

var readerService = builder.AddProject<Projects.Syndic_ReaderService>("syndicapi")
  .WithReference(postgresReaderDb)
  .WaitFor(postgresReaderDb)
  .WaitForCompletion(readerDbManager)
  .PublishAsDockerComposeService((resource, service) =>
  {
  });

// It is not possible as of now to spin ud a "AddJavaScriptApp"
// kind application as a container when deploying. 
// It will simply not show up in the docker compose config, 
// no matter what (as of Aspire 13.0.1)
// However, it does work with a "AddNodeApp" kind. 
// So we will use that, when building/deploying
if (builder.ExecutionContext.IsRunMode)
{
  var frontend = builder.AddJavaScriptApp("syndicfrontend", "../Syndic.Frontend")
                  .WithUrl("http://localhost:5173")
                  .WaitFor(readerService)
                  .WithReference(readerService)
                  .WithEnvironment("AUTH_AUTHENTIK_ID", syndicAuthentikClientId)
                  .WithEnvironment("AUTH_AUTHENTIK_CLIENT_SECRET", syndicAuthentikClientSecert)
                  .WithEnvironment("AUTH_SECRET", authJsSecret)
                  .WithEnvironment("AUTH_AUTHENTIK_ISSUER", authentikIssuerUrl);
}
else if (builder.ExecutionContext.IsPublishMode)
{
  var frontend = builder.AddNodeApp("syndicfrontend", "../Syndic.Frontend", "build")
                            .WaitFor(readerService)
                            .WithReference(readerService)
                            .WithBuildScript("build")
                            .WithHttpEndpoint(8080, env: "PORT")
                            .WithExternalHttpEndpoints()
                            .PublishAsDockerComposeService((res, ser) => { })
                            .WithEnvironment("AUTH_AUTHENTIK_ID", syndicAuthentikClientId)
                            .WithEnvironment("AUTH_AUTHENTIK_CLIENT_SECRET", syndicAuthentikClientSecert)
                            .WithEnvironment("AUTH_SECRET", authJsSecret)
                            .WithEnvironment("AUTH_AUTHENTIK_ISSUER", authentikIssuerUrl);
}
else
{
  throw new Exception("Soo uhm ExecutionContext was neither RunMode or PublishMode. I dont know what frontend to create");
}

builder
  .Build()
  .Run();
