using HW_FileParser.Models;

namespace HW_FileParser.Contracts;

public interface IDataContext
{
    public Task<Guid> WriteDataAsync(DownloadResult downloadResult, CancellationToken ct = default);
}
