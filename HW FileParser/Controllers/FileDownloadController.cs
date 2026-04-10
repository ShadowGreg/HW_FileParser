using HW_FileParser.Contracts;
using HW_FileParser.Models;
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
        CancellationToken ct) {
        if (hostApplicationLifetime.ApplicationStopping.IsCancellationRequested) {
            return Problem(
                detail: "Сервис завершает работу и не принимает новые задачи.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var request = addresses with { RequestId = addresses.RequestId ?? HttpContext.TraceIdentifier };

        return Ok(await downloadService.DownloadFileAsync(request, ct));
    }
}