namespace HW_FileParser.Options;
public class DownloaderServiceOptions
{
    public string OutputPath { get; set; } = "C:/downloadPicture/";
    public long FileSizeMb { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public int Retry { get; set; } = 2;
}