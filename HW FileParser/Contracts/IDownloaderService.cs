using HW_FileParser.Models;

namespace HW_FileParser.Contracts;

public interface IDownloaderService
{
    Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(UrlsRequest addresses, CancellationToken ct);
}
