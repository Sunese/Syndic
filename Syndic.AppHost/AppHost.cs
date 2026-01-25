using Microsoft.Extensions.DependencyInjection;
using Aspire.Hosting.Publishing;
using Aspire.Hosting.Pipelines;

#pragma warning disable ASPIREPIPELINES003
#pragma warning disable ASPIRECOMPUTE003

var builder = DistributedApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();

var containerRegistry = builder.AddContainerRegistry("ghcr-syndic", "ghcr.io", "sunese/syndic");

builder.AddDockerComposeEnvironment("syndic")
  .WithDashboard(c =>
  {
    c.WithHostPort(8888);
    c.WithExternalHttpEndpoints();
  });
var paramAuthGoogleId = builder.AddParameter("AUTH-GOOGLE-ID", secret: true);
var paramAuthGoogleSecret = builder.AddParameter("AUTH-GOOGLE-SECRET", secret: true);
var paramAuthGithubId = builder.AddParameter("AUTH-GITHUB-ID", secret: true);
var paramAuthGithubSecret = builder.AddParameter("AUTH-GITHUB-SECRET", secret: true);
var paramAuthJsSecret = builder.AddParameter("AUTH-SECRET", secret: true);
var paramInternalJwtSecret = builder.AddParameter("INTERNAL-JWT-SECRET", secret: true);

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
  })
  .WithContainerBuildOptions(opts =>
  {
    opts.TargetPlatform = ContainerTargetPlatform.AllLinux;
  })
  .WithContainerRegistry(containerRegistry)
  .WithRemoteImageName("worker")
  .WithRemoteImageTag("latest");

var readerService = builder.AddProject<Projects.Syndic_ReaderService>("syndicapi")
  .WithReference(postgresReaderDb)
  .WaitFor(postgresReaderDb)
  .WaitForCompletion(readerDbManager)
  .WithEnvironment("INTERNAL_JWT_SECRET", paramInternalJwtSecret)
  .PublishAsDockerComposeService((resource, service) =>
  {
  })
  .WithContainerBuildOptions(opts =>
  {
    opts.TargetPlatform = ContainerTargetPlatform.AllLinux;
  })
  .WithContainerRegistry(containerRegistry)
  .WithRemoteImageName("api")
  .WithRemoteImageTag("latest");

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
                  .WithEnvironment("AUTH_GOOGLE_ID", paramAuthGoogleId)
                  .WithEnvironment("AUTH_GOOGLE_SECRET", paramAuthGoogleSecret)
                  .WithEnvironment("AUTH_GITHUB_ID", paramAuthGithubId)
                  .WithEnvironment("AUTH_GITHUB_SECRET", paramAuthGithubSecret)
                  .WithEnvironment("AUTH_SECRET", paramAuthJsSecret)
                  .WithEnvironment("INTERNAL_JWT_SECRET", paramInternalJwtSecret);
}
else if (builder.ExecutionContext.IsPublishMode)
{
  var frontend = builder.AddNodeApp("syndicfrontend", "../Syndic.Frontend", "build")
                            .WaitFor(readerService)
                            .WithReference(readerService)
                            .WithBuildScript("build")
                            .WithHttpEndpoint(8080, 8080, env: "PORT")
                            .WithExternalHttpEndpoints()
                            .PublishAsDockerComposeService((res, ser) =>
                            {
                            })
                            .PublishAsDockerFile(c => { c.WithImageTag("latest"); })
                            .WithEnvironment("AUTH_GOOGLE_ID", paramAuthGoogleId)
                            .WithEnvironment("AUTH_GOOGLE_SECRET", paramAuthGoogleSecret)
                            .WithEnvironment("AUTH_GITHUB_ID", paramAuthGithubId)
                            .WithEnvironment("AUTH_GITHUB_SECRET", paramAuthGithubSecret)
                            .WithEnvironment("AUTH_SECRET", paramAuthJsSecret)
                            .WithEnvironment("INTERNAL_JWT_SECRET", paramInternalJwtSecret)
                            .WithContainerBuildOptions(opts =>
                            {
                              opts.TargetPlatform = ContainerTargetPlatform.AllLinux;
                              opts.ImageFormat = ContainerImageFormat.Docker;
                            })
                            .WithRemoteImageName("frontend")
                            .WithRemoteImageTag("latest")
                            .WithContainerRegistry(containerRegistry);
}
else
{
  throw new Exception("Soo uhm ExecutionContext was neither RunMode or PublishMode. I dont know what frontend to create");
}

builder
  .Build()
  .Run();
