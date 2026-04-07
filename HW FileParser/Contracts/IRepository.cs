using HW_FileParser.Models;

namespace HW_FileParser.Contracts;

public interface IRepository
{
    public Task<Guid> WriteDataAsync(DownloadResult downloadResult, CancellationToken ct = default);
}
