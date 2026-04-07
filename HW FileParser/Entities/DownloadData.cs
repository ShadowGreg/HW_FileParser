namespace HW_FileParser.Entities;

public class DownloadData
{
    public Guid Id { get; set; }
    public string? Url { get; set; }
    public string? FilePath { get; set; }
    public DateTimeOffset? BeginTime { get; set; }
    public DateTimeOffset? EndTime { get; set; }
    public long FileSize { get; set; }
    public string AVGDownloadSpeed { get; set; } = "";
    public string Status { get; set; } = "";
    public string ErrorMSG { get; set; } = "";
    public string? RequestId { get; set; }
}