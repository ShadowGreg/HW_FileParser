namespace HW_FileParser.Options;
public class DownloaderServiceOptions
{
    public string OutputPath { get; set; } = "C:/downloadPicture/";
    public long FileSizeMb { get; set; } = 10;
    public int TimeoutSeconds { get; set; } = 30;
    public int Retry { get; set; } = 2;
    public string ClientName { get; set; } = "DownloaderClient";
    public int MaxConnections { get; set; } = 100;
    public int MaxUrlsPerRequest { get; set; } = 100;
    public int MaxConcurrentDownloads { get; set; } = 8;
}