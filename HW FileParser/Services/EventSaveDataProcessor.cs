using HW_FileParser.Contracts;
using HW_FileParser.Models;

namespace HW_FileParser.Services;

public class EventSaveDataProcessor(
    IRepository db
    ): IEventHandler<DownloadResult>
{
    public async Task<Guid> WriteDataAsync(DownloadResult downloadResult, CancellationToken ct = default) {
        return await db.WriteDataAsync(downloadResult, ct);
    }

    public async Task HandleAsync(DownloadResult @event) => await WriteDataAsync(@event, CancellationToken.None);
}
