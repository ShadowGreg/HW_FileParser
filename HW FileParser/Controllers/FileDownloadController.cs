using HW_FileParser.Entities.DTO;
using HW_FileParser.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HW_FileParser.Controllers;
[ApiController]
[Route("[controller]")]
public class FileDownloadController(IDownloaderService downloadService): ControllerBase
{
    [HttpPost]
    public async Task<IReadOnlyCollection<DownloadResult>> DownloadFiles(
        [FromBody] UrlsRequest addresses,
        CancellationToken ct) 
    {
        return await downloadService.DownloadFileAsync(addresses, ct);
    }
}