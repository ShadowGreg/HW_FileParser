using HW_FileParser.Contracts;
using HW_FileParser.Entities;
using HW_FileParser.Models;

namespace HW_FileParser.Data;

public class Repository(AppDataContext context): IRepository
{
    public async Task<Guid> WriteDataAsync(DownloadResult downloadResult, CancellationToken ct = default) {
        var id = Guid.NewGuid();
        var downloadData = new DownloadData() {
                                                  Id = id,
                                                  Url = downloadResult.Url,
                                                  FilePath = downloadResult.FilePath,
                                                  BeginTime = (downloadResult.BeginTime ?? DateTimeOffset.UtcNow).ToUniversalTime(),
                                                  EndTime = (downloadResult.EndTime ?? DateTimeOffset.UtcNow).ToUniversalTime(),
                                                  FileSize = downloadResult.FileSize,
                                                  AVGDownloadSpeed = downloadResult.AVGDownloadSpeed,
                                                  Status = downloadResult.Status,
                                                  ErrorMSG = downloadResult.ErrorMSG,
                                                  RequestId = downloadResult.RequestId
                                              };

        context.DownloadDatas.Add(downloadData);
        await context.SaveChangesAsync(ct);

        return id;
    }
}
