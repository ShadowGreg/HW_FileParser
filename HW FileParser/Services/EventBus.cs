using HW_FileParser.Contracts;
using HW_FileParser.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HW_FileParser.Services;

public class EventBus(
    IServiceScopeFactory scopeFactory,
    ILogger<EventBus> logger,
    IOptions<EventBusOptions> optionsAccessor): IEventBus
{
    private readonly EventBusOptions _options = optionsAccessor.Value;

    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var scope = scopeFactory.CreateScope();

        var handlers = scope.ServiceProvider
                            .GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            for (var attempt = 0;; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    await handler.HandleAsync(@event);
                    break;
                }
                catch (Exception ex)
                {
                    if (attempt >= _options.HandlerRetryCount)
                    {
                        logger.LogError(
                            ex,
                            "Event handler {HandlerType} failed after {AttemptCount} attempts",
                            handler.GetType().Name,
                            attempt + 1);
                        throw;
                    }

                    logger.LogWarning(
                        ex,
                        "Event handler {HandlerType} attempt {Attempt} failed, retrying",
                        handler.GetType().Name,
                        attempt + 1);
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(_options.RetryDelayMilliseconds * (attempt + 1)),
                        ct);
                }
            }
        }
    }
}
