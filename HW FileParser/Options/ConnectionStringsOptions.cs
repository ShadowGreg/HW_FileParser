namespace HW_FileParser.Options;
public class ConnectionStringsOptions
{
    public string DefaultConnection { get; set; } = "Host=localhost;Port=5432;Database=hwfileparser;Username=postgres;Password=postgres";
}