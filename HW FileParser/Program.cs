using HW_FileParser.Data;
using HW_FileParser.Entities.DTO;
using HW_FileParser.Options;
using HW_FileParser.Service;
using HW_FileParser.Service.Abstractions;
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

builder.Services.AddDbContext<AppDataContext>(options =>
    options.UseSqlite(connectionStringsOptionsOptions.DefaultConnection));

builder.Services.Configure<DownloaderServiceOptions>(
    builder.Configuration.GetSection(nameof(DownloaderServiceOptions)));

builder.Services.AddScoped<IDataContext, UnitOfWork>();
builder.Services.AddScoped<IDownloaderService, DownloaderService>();
builder.Services.AddTransient<IEventHandler<DownloadResult>, EventSaveDataProsessor>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddControllers();

var app = builder.Build();

using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<AppDataContext>();

try {
    await dbContext.Database.MigrateAsync();
    Console.WriteLine("✅ База данных успешно обновлена (миграции применены)");
}
catch (Exception ex) {
    Console.WriteLine($"❌ Ошибка при применении миграций: {ex.Message}");
}


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();