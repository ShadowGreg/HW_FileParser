namespace HW_FileParser.Options;

public class EventBusOptions
{

    public int HandlerRetryCount { get; set; } = 2;

    public int RetryDelayMilliseconds { get; set; } = 100;
}
