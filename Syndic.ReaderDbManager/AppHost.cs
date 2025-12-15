using Microsoft.EntityFrameworkCore;
using ReaderService.MigrationService;
using Syndic.ReaderDb;
using Syndic.ServiceDefaults;

var builder = Host.CreateApplicationBuilder(args);
builder.AddServiceDefaults();
builder.Services.AddHostedService<Worker>();

builder.AddNpgsqlDbContext<ReaderDbContext>("readerdb", null,
    optionsBuilder => optionsBuilder.UseNpgsql(npgsqlBuilder =>
        npgsqlBuilder.MigrationsAssembly(typeof(Program).Assembly.GetName().Name)));

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

var host = builder.Build();
host.Run();
