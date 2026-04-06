using HW_FileParser.Entities.DTO;
using HW_FileParser.Service.Abstractions;

namespace HW_FileParser.Service;
public class EventSaveDataProsessor(
    IDataContext db
    ): IEventHandler<DownloadResult>
{
    public async Task<Guid> WriteDataAsync(DownloadResult downloadResult) {
        return await db.WriteDataAsync(downloadResult);
    }

    public async Task HandleAsync(DownloadResult @event) => await WriteDataAsync(@event);
}