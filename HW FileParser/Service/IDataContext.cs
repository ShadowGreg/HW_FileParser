using HW_FileParser.Entities.DTO;

namespace HW_FileParser.Service;
public interface IDataContext
{
    public Task<Guid> WriteData(DownloadResult downloadResult);
}