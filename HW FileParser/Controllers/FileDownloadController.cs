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
        var request = addresses with {
            RequestId = addresses.RequestId ?? HttpContext.TraceIdentifier
        };
        return await downloadService.DownloadFileAsync(request, ct);
    }
}
