using System.Collections.Concurrent;
using System.Text;
using HW_FileParser.Entities.DTO;
using HW_FileParser.Entities.Enums;
using HW_FileParser.Exceptions;
using HW_FileParser.Options;
using HW_FileParser.Service.Abstractions;
using Microsoft.Extensions.Options;

namespace HW_FileParser.Service;
public class DownloaderService(
    IEventBus eventBus,
    IOptionsSnapshot<DownloaderServiceOptions> serviceOptionsAccessor)
    : IDownloaderService
{
    private readonly DownloaderServiceOptions _serviceOptions = serviceOptionsAccessor.Value;
    private const long Mb = 1024 * 1024;
    private const long Kb = 1024;


    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(
        UrlsRequest addresses, CancellationToken ct) {
        var outputPath = _serviceOptions.OutputPath;
        ConcurrentBag<DownloadResult> results = [];
        var options = new ParallelOptions { CancellationToken = ct };
        using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(_serviceOptions.TimeoutSeconds) };
        var requestId = addresses.RequestId;
        await Parallel.ForEachAsync(addresses.Urls,
            options,
            async (address, token) =>
                {
                    try {
                        var result = await DownloadFileAsync(client, address, outputPath, requestId, token);
                        results.Add(result);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                        var failed = new DownloadResult(
                            Url: address,
                            FilePath: outputPath,
                            BeginTime: TimeProvider.System.GetLocalNow(),
                            EndTime: TimeProvider.System.GetLocalNow(),
                            FileSize: 0,
                            AVGDownloadSpeed: "",
                            Status: nameof(Status.Failed),
                            ErrorMSG: ex.Message,
                            RequestId: requestId);
                        await eventBus.PublishAsync(failed);
                        results.Add(failed);
                    }
                });

        return results;
    }

    private async Task<DownloadResult> DownloadFileAsync(
        HttpClient client,
        string url,
        string outputPath,
        string? requestId,
        CancellationToken ct) {
        DateTimeOffset begin = TimeProvider.System.GetLocalNow();
        DateTimeOffset end = TimeProvider.System.GetLocalNow();
        long? totalFileSize = null;
        try {
            var data = await client.GetAsync(url, ct);
            totalFileSize = data.Content.Headers.ContentLength;

            if (totalFileSize > _serviceOptions.FileSizeMb * Mb)
                throw new FileSizeException($"Размер файла больше установленного лимита {_serviceOptions.FileSizeMb}Mb");

            var fileNAme = new StringBuilder()
                          .Append(Guid.NewGuid())
                          .Append(data.Content.Headers.ContentDisposition!.FileName!.Trim('"').Split('.')[1])
                          .ToString();


            if (!Directory.Exists(outputPath)) {
                Directory.CreateDirectory(outputPath);
            }

            var filePath = Path.Combine(outputPath, fileNAme);
        
            byte[] fileBytes = await client.GetByteArrayAsync(url, ct);

            await File.WriteAllBytesAsync(filePath, fileBytes, ct);
            end = TimeProvider.System.GetLocalNow();
            var downloadSpeed = FormatSpeed((long)totalFileSize!, (end - begin).TotalSeconds);
            var compleetDownloadResault = new DownloadResult(
                Url: url,
                FilePath: filePath,
                BeginTime: begin,
                EndTime: end,
                FileSize: (long)totalFileSize,
                AVGDownloadSpeed: downloadSpeed,
                Status: nameof(Status.Success),
                ErrorMSG: "",
                RequestId: requestId);

            await eventBus.PublishAsync(compleetDownloadResault);
            return compleetDownloadResault;

        }
        catch (OperationCanceledException e) {
            var cancelled = new DownloadResult(
                Url: url,
                FilePath: outputPath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize ?? 0,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Cancelled),
                ErrorMSG: e.Message,
                RequestId: requestId);
            await eventBus.PublishAsync(cancelled);
            return cancelled;
        }
        catch (FileSizeException e) {
            Console.WriteLine(e);
            var failed = new DownloadResult(
                Url: url,
                FilePath: outputPath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize ?? 0,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Failed),
                ErrorMSG: e.Message,
                RequestId: requestId);
            await eventBus.PublishAsync(failed);
            return failed;
        }
        catch (Exception e) {
            Console.WriteLine(e);
            var failed = new DownloadResult(
                Url: url,
                FilePath: outputPath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize ?? 0,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Failed),
                ErrorMSG: e.Message,
                RequestId: requestId);
            await eventBus.PublishAsync(failed);
            return failed;
        }
    }


    string FormatSpeed(long size, double tspan) {
        string message = "Average download speed: {0:N0} {1}";
        return (size / tspan) switch {
                   > Mb => string.Format(message, size / Mb / tspan, "MB/s"),
                   > Kb => string.Format(message, size / Kb / tspan, "KB/s"),
                   _ => string.Format(message, size / tspan, "B/s")
               };
    }
}