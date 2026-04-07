using HW_FileParser.Entities.DTO;

namespace HW_FileParser.Service.Abstractions;
public interface IDataContext
{
    public Task<Guid> WriteDataAsync(DownloadResult downloadResult, CancellationToken ct = default);
}