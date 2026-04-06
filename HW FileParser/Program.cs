using HW_FileParser.Data;
using HW_FileParser.Entities.DTO;
using HW_FileParser.Service;
using HW_FileParser.Service.Abstractions;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                    ?? "Data Source=app.db";
builder.Services.AddDbContext<AppDataContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddScoped<IDataContext, UnitOfWork>();
builder.Services.AddScoped<IDownloaderService, DownloaderService>();
builder.Services.AddTransient<IEventHandler<DownloadResult>, EventSaveDataProsessor>();
builder.Services.AddSingleton<IEventBus, EventBus>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();