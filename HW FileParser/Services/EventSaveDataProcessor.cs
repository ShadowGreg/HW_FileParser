using HW_FileParser.Contracts;
using HW_FileParser.Models;

namespace HW_FileParser.Services;

public class EventSaveDataProcessor(IRepository db): IEventHandler<DownloadResult>
{
    public async Task HandleAsync(DownloadResult @event) => await db.WriteDataAsync(@event, CancellationToken.None);
}
