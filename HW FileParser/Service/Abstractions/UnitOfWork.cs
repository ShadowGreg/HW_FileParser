using HW_FileParser.Data;
using HW_FileParser.Entities;
using HW_FileParser.Entities.DTO;

namespace HW_FileParser.Service.Abstractions;
public class UnitOfWork(AppDataContext context): IDataContext
{
    public async Task<Guid> WriteDataAsync(DownloadResult downloadResult) {
        var id = Guid.NewGuid();
        var downloadData = new DownloadData() {
                                                  Id = id,
                                                  Url = downloadResult.Url,
                                                  FilePath = downloadResult.FilePath,
                                                  BeginTime = downloadResult.BeginTime?.UtcDateTime ?? DateTime.UtcNow,
                                                  EndTime = downloadResult.EndTime?.UtcDateTime ?? DateTime.UtcNow,
                                                  FileSize = downloadResult.FileSize,
                                                  AVGDownloadSpeed = downloadResult.AVGDownloadSpeed,
                                                  Status = downloadResult.Status,
                                                  ErrorMSG = downloadResult.ErrorMSG
                                              };

        context.DownloadDatas.Add(downloadData);
        await context.SaveChangesAsync();

        return id;
    }
}