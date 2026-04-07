using HW_FileParser.Entities.DTO;
using HW_FileParser.Service.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace HW_FileParser.Controllers;
[ApiController]
[Route("[controller]")]
public class FileDownloadController(
    IDownloaderService downloadService,
    IHostApplicationLifetime hostApplicationLifetime): ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<IReadOnlyCollection<DownloadResult>>> DownloadFiles(
        [FromBody] UrlsRequest addresses,
        CancellationToken ct)
    {
        if (hostApplicationLifetime.ApplicationStopping.IsCancellationRequested) {
            return Problem(
                detail: "Сервис завершает работу и не принимает новые задачи.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var request = addresses with {
            RequestId = addresses.RequestId ?? HttpContext.TraceIdentifier
        };
        var result = await downloadService.DownloadFileAsync(request, ct);
        return Ok(result);
    }
}
