using System.Collections.Concurrent;
using HW_FileParser.Entities.DTO;
using HW_FileParser.Entities.Enums;
using HW_FileParser.Service.Abstractions;

namespace HW_FileParser.Service;
public class DownloaderService(
    IEventBus eventBus
): IDownloaderService
{
    private string _outputPath = "C:/downloadPicture/";

    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(
        UrlsRequest addresses, CancellationToken ct) {
        ConcurrentBag<DownloadResult> results = [];
        var options = new ParallelOptions { MaxDegreeOfParallelism = 10, CancellationToken = ct };
        using HttpClient client = new HttpClient();
        await Parallel.ForEachAsync(addresses.Urls,
            options,
            async (address, token) =>
                {
                    try {
                        var result = await DownloadFileAsync(client, address, _outputPath, token);
                        results.Add(result);
                    }
                    catch (Exception ex) {
                        Console.WriteLine(ex);
                    }
                });

        return results;
    }

    private async Task<DownloadResult> DownloadFileAsync(
        HttpClient client,
        string url,
        string outputPath,
        CancellationToken ct) {
        DateTimeOffset begin = TimeProvider.System.GetLocalNow();
        DateTimeOffset end = TimeProvider.System.GetLocalNow();
        long? totalFileSize = null;
        try {
            var data = await client.GetAsync(url, ct);
            totalFileSize = data.Content.Headers.ContentLength;
            var fileNAme = data.Content.Headers.ContentDisposition?.FileName;
            byte[] fileBytes = await client.GetByteArrayAsync(url, ct);

            if (!Directory.Exists(outputPath)) {
                Directory.CreateDirectory(outputPath);
            }

            string outputFile = "";
            if (fileNAme != null) {
                outputFile = Path.Combine(outputPath, fileNAme.Trim('"'));
            }

            await File.WriteAllBytesAsync(outputFile, fileBytes, ct);
            end = TimeProvider.System.GetLocalNow();
            var downloadSpeed = FormatSpeed((long)totalFileSize!, (end - begin).TotalSeconds);
            var compleetDownloadResault = new DownloadResult {
                                                                 Url = url,
                                                                 FilePath = outputFile,
                                                                 BeginTime = begin,
                                                                 EndTime = end,
                                                                 FileSize = (long)totalFileSize,
                                                                 AVGDownloadSpeed = downloadSpeed,
                                                                 Status = nameof(Status.Success)
                                                             };

            await eventBus.PublishAsync(compleetDownloadResault);
            return compleetDownloadResault;

        }
        catch (OperationCanceledException e) {
            return new DownloadResult {
                                          Url = url,
                                          FilePath = outputPath,
                                          BeginTime = begin,
                                          EndTime = end,
                                          FileSize = totalFileSize ?? 0,
                                          Status = nameof(Status.Cancelled),
                                          ErrorMSG = e.Message
                                      };
        }
        catch (Exception e) {
            Console.WriteLine(e);
            return new DownloadResult {
                                          Url = url,
                                          FilePath = outputPath,
                                          BeginTime = begin,
                                          EndTime = end,
                                          FileSize = totalFileSize ?? 0,
                                          Status = nameof(Status.Error),
                                          ErrorMSG = e.Message
                                      };
        }


    }


    string FormatSpeed(long size, double tspan) {
        string message = "Average download speed: {0:N0} {1}";
        return (size / tspan) switch {
                   // MB
                   > 1024 * 1024 => string.Format(message, size / (1024 * 1204) / tspan, "MB/s"),
                   // KB
                   > 1024 => string.Format(message, size / (1024) / tspan, "KB/s"),
                   _ => string.Format(message, size / tspan, "B/s")
               };
    }
}