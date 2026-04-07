namespace HW_FileParser.Entities.DTO;

public record DownloadResult(
    string? Url = null,
    string? FilePath = null,
    DateTimeOffset? BeginTime = null,
    DateTimeOffset? EndTime = null,
    long FileSize = 0,
    string AVGDownloadSpeed = "",
    string Status = "",
    string ErrorMSG = "",
    string? RequestId = null
    );
