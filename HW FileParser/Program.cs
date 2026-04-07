using HW_FileParser.Contracts;
using HW_FileParser.Data;
using HW_FileParser.Models;
using HW_FileParser.Options;
using HW_FileParser.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.Sources.Clear();

IHostEnvironment env = builder.Environment;

builder.Configuration
       .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
       .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true, true);

ConnectionStringsOptions connectionStringsOptionsOptions = new();
builder.Configuration.GetSection(nameof(ConnectionStringsOptions))
       .Bind(connectionStringsOptionsOptions);

if (string.IsNullOrWhiteSpace(connectionStringsOptionsOptions.DefaultConnection)) {
    throw new InvalidOperationException(
        "Задайте ConnectionStringsOptions:DefaultConnection.");
}

builder.Services.AddDbContext<AppDataContext>(options =>
    options.UseNpgsql(connectionStringsOptionsOptions.DefaultConnection));

builder.Services.Configure<DownloaderServiceOptions>(
    builder.Configuration.GetSection(nameof(DownloaderServiceOptions)));
var downloaderOptions = builder.Configuration
                               .GetSection(nameof(DownloaderServiceOptions))
                               .Get<DownloaderServiceOptions>() ?? new DownloaderServiceOptions();

builder.Services.AddHttpClient(downloaderOptions.ClientName,
            client =>
                {
                    client.Timeout = TimeSpan.FromSeconds(downloaderOptions.TimeoutSeconds);
                })
       .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                                                                            MaxConnectionsPerServer = downloaderOptions.MaxConnections,
                                                                            PooledConnectionLifetime =
                                                                                TimeSpan.FromMinutes(5),
                                                                            PooledConnectionIdleTimeout =
                                                                                TimeSpan.FromMinutes(2)
                                                                        })
       .AddStandardResilienceHandler(options =>
            {
                options.Retry.MaxRetryAttempts = downloaderOptions.Retry;
                var t = TimeSpan.FromSeconds(downloaderOptions.TimeoutSeconds);
                options.AttemptTimeout.Timeout = t;
                options.CircuitBreaker.SamplingDuration = t + t; // >= 2× attempt timeout (library rule)
                options.TotalRequestTimeout.Timeout =
                    t * (downloaderOptions.Retry + 1) * 2;
            });

builder.Services.AddScoped<IRepository, Repository>();
builder.Services.AddScoped<IDownloaderService, DownloaderService>();
builder.Services.AddTransient<IEventHandler<DownloadResult>, EventSaveDataProcessor>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddControllers();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDataContext>();

try {
    await dbContext.Database.MigrateAsync();
    app.Logger.LogInformation("Database migrations applied successfully");
}
catch (Exception ex) {
    app.Logger.LogError(ex, "Failed to apply database migrations");
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();