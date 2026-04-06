using HW_FileParser.Entities.DTO;

namespace HW_FileParser.Service;
public interface IDownloaderService
{
    Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(UrlsRequest addresses, CancellationToken ct);
}