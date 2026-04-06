using HW_FileParser.Entities.DTO;

namespace HW_FileParser.Service.Abstractions;
public interface IDownloaderService
{
    Task<IReadOnlyCollection<DownloadResult>> DownloadFileAsync(UrlsRequest addresses, CancellationToken ct);
}