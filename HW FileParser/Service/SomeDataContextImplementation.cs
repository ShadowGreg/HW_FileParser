using HW_FileParser.Entities.DTO;
using HW_FileParser.Service;

public class SomeDataContextImplementation : IDataContext
{
    public Task<Guid> WriteData(DownloadResult downloadResult) {
        throw new NotImplementedException();
    }
}