namespace HW_FileParser.Exceptions;
public class FileSizeException: Exception
{
    public FileSizeException(): base() { }
    public FileSizeException(string message): base(message) { }
    public FileSizeException(string message, Exception inner): base(message, inner) { }
}