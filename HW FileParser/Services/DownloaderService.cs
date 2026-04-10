using HW_FileParser.Contracts;
using HW_FileParser.Entities.Enums;
using HW_FileParser.Exceptions;
using HW_FileParser.Models;
using HW_FileParser.Options;
using Microsoft.Extensions.Options;

namespace HW_FileParser.Services;
public class DownloaderService(
    IEventBus eventBus,
    IHttpClientFactory httpClientFactory,
    ILogger<DownloaderService> logger,
    IOptionsSnapshot<DownloaderServiceOptions> serviceOptionsAccessor)
    : IDownloaderService
{
    private readonly DownloaderServiceOptions _serviceOptions = serviceOptionsAccessor.Value;
    private const long Mb = 1024 * 1024;
    private const long Kb = 1024;

    private const int StreamCopyBufferSize = 80 * 1024;


    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(UrlsRequest addresses,
                                                                             CancellationToken ct) {
        var outputPath = _serviceOptions.OutputPath;
        var client = httpClientFactory.CreateClient(_serviceOptions.ClientName);
        var requestId = addresses.RequestId;

        using (logger.BeginScope(new Dictionary<string, object?> {
                                                                     ["RequestId"] = requestId,
                                                                     ["UrlCount"] = addresses.Urls.Count
                                                                 }))
            logger.LogInformation(
                "Download batch started: {UrlCount} URLs, MaxDegreeOfParallelism {MaxDegree}",
                addresses.Urls.Count,
                _serviceOptions.MaxConcurrentDownloads);

        using var semaphore = new SemaphoreSlim(_serviceOptions.MaxConcurrentDownloads);

        var tasks = addresses.Urls.Select(address => DownloadWithThrottleAsync(
            semaphore,
            client,
            address,
            outputPath,
            requestId,
            ct));

        var results = (await Task.WhenAll(tasks)).ToList();

        logger.LogInformation("Download batch finished: {ResultCount} results", results.Count);

        return results;
    }

    private async Task<DownloadResult> DownloadWithThrottleAsync(SemaphoreSlim semaphore,
                                                                 HttpClient client,
                                                                 string address,
                                                                 string outputPath,
                                                                 string? requestId,
                                                                 CancellationToken ct) {
        await semaphore.WaitAsync(ct);
        try {
            return await DownloadFileAsync(client, address, outputPath, requestId, ct);
        }
        finally {
            semaphore.Release();
        }
    }

    private async Task<DownloadResult> DownloadFileAsync(
        HttpClient client,
        string url,
        string outputPath,
        string? requestId,
        CancellationToken ct) {
        DateTimeOffset begin = TimeProvider.System.GetLocalNow();
        DateTimeOffset end = TimeProvider.System.GetLocalNow();
        long totalFileSize = 0;
        string filePath = outputPath;
        var maxFileSizeBytes = _serviceOptions.FileSizeMb * Mb;
        try {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(_serviceOptions.TimeoutSeconds));
            var token = timeoutCts.Token;

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                token);

            if (response.Content.Headers.ContentLength > maxFileSizeBytes)
                throw new FileSizeException(
                    $"Размер файла больше установленного лимита {_serviceOptions.FileSizeMb}Mb");

            response.EnsureSuccessStatusCode();
            var fileName = $"{Guid.NewGuid():N}{ResolveExtension(url, response)}".Trim('"');

            Directory.CreateDirectory(outputPath);
            filePath = Path.Combine(outputPath, fileName);

            await using var responseStream = await response.Content.ReadAsStreamAsync(token);
            await using var fileStream = new FileStream(
                filePath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None,
                StreamCopyBufferSize,
                useAsync: true);

            var buffer = new byte[StreamCopyBufferSize];
            int read;
            while ((read = await responseStream.ReadAsync(buffer, token)) > 0) {
                totalFileSize += read;
                if (totalFileSize > maxFileSizeBytes) {
                    throw new FileSizeException(
                        $"Размер файла больше установленного лимита {_serviceOptions.FileSizeMb}Mb");
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read), token);
            }

            end = TimeProvider.System.GetLocalNow();
            var downloadSpeed = FormatSpeed(totalFileSize, (end - begin).TotalSeconds);
            var completedDownloadResult = new DownloadResult(
                Url: url,
                FilePath: filePath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize,
                AVGDownloadSpeed: downloadSpeed,
                Status: nameof(Status.Success),
                ErrorMSG: "",
                RequestId: requestId);

            return await PublishDownloadResultAsync(completedDownloadResult, CancellationToken.None);

        }
        catch (OperationCanceledException e) {
            end = TimeProvider.System.GetLocalNow();
            var message = ct.IsCancellationRequested
                ? e.Message
                : $"Превышен таймаут {_serviceOptions.TimeoutSeconds}с на скачивание файла";

            var cancelled = new DownloadResult(
                Url: url,
                FilePath: filePath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Cancelled),
                ErrorMSG: message,
                RequestId: requestId);
            return await PublishDownloadResultAsync(cancelled, CancellationToken.None);
        }
        catch (FileSizeException e) {
            end = TimeProvider.System.GetLocalNow();
            logger.LogWarning(e, "File size limit exceeded for url {Url}", url);
            DeletePartialFile(filePath, outputPath);
            var failed = new DownloadResult(
                Url: url,
                FilePath: filePath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Failed),
                ErrorMSG: e.Message,
                RequestId: requestId);
            return await PublishDownloadResultAsync(failed, CancellationToken.None);
        }
        catch (Exception e) {
            end = TimeProvider.System.GetLocalNow();
            logger.LogError(e, "Failed to download url {Url}", url);
            DeletePartialFile(filePath, outputPath);
            var failed = new DownloadResult(
                Url: url,
                FilePath: filePath,
                BeginTime: begin,
                EndTime: end,
                FileSize: totalFileSize,
                AVGDownloadSpeed: "",
                Status: nameof(Status.Failed),
                ErrorMSG: e.Message,
                RequestId: requestId);
            return await PublishDownloadResultAsync(failed, CancellationToken.None);
        }
    }

    private async Task<DownloadResult> PublishDownloadResultAsync(DownloadResult result, CancellationToken ct) {
        try {
            await eventBus.PublishAsync(result, ct);
            return result;
        }
        catch (Exception ex) {
            logger.LogError(ex, "Не удалось сохранить результат загрузки в БД для {Url}", result.Url);
            var dbPart = $"Не удалось сохранить запись в БД: {ex.Message}";
            var (status, errorMsg) = result.Status switch {
                nameof(Status.Success) => (
                    nameof(Status.Failed),
                    $"Файл сохранён на диск, но метаданные не записаны. {dbPart}"),
                nameof(Status.Cancelled) => (
                    nameof(Status.Failed),
                    string.IsNullOrEmpty(result.ErrorMSG) ? dbPart : $"{result.ErrorMSG} {dbPart}"),
                _ => (
                    nameof(Status.Failed),
                    string.IsNullOrEmpty(result.ErrorMSG) ? dbPart : $"{result.ErrorMSG} {dbPart}")
            };
            return result with { Status = status, ErrorMSG = errorMsg };
        }
    }


    private static void DeletePartialFile(string filePath, string outputPath) {
        if (string.IsNullOrWhiteSpace(filePath) || filePath == outputPath || !File.Exists(filePath)) {
            return;
        }

        File.Delete(filePath);
    }

    private static string ResolveExtension(string url, HttpResponseMessage response) {
        var extension = Path.GetExtension(
            response.Content.Headers.ContentDisposition?.FileNameStar
         ?? response.Content.Headers.ContentDisposition?.FileName);
        if (!string.IsNullOrWhiteSpace(extension)) {
            return extension;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
            extension = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(extension)) {
                return extension;
            }
        }

        return response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant() switch {
                   "image/jpeg" => ".jpg",
                   "image/png" => ".png",
                   "image/gif" => ".gif",
                   "text/plain" => ".txt",
                   "application/pdf" => ".pdf",
                   _ => string.Empty
               };
    }

    string FormatSpeed(long size, double tspan) {
        if (tspan <= 0) {
            return "Average download speed: n/a";
        }

        var bytesPerSecond = size / tspan;
        string message = "Average download speed: {0:N0} {1}";
        return bytesPerSecond switch {
                   > Mb => string.Format(message, bytesPerSecond / Mb, "MB/s"),
                   > Kb => string.Format(message, bytesPerSecond / Kb, "KB/s"),
                   _ => string.Format(message, bytesPerSecond, "B/s")
               };
    }
}