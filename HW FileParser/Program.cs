using Downloader;
using HW_FileParser.Service;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddScoped<IDataContext, SomeDataContextImplementation>();
builder.Services.AddScoped<IDownloaderService, DownloaderService>();

var app = builder.Build();

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();