using HW_FileParser.Service.Abstractions;
using Microsoft.Extensions.Logging;

namespace HW_FileParser.Service;
public class EventBus(IServiceScopeFactory scopeFactory, ILogger<EventBus> logger): IEventBus
{
    public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        using var scope = scopeFactory.CreateScope();

        var handlers = scope.ServiceProvider
                            .GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                await handler.HandleAsync(@event);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in event handler {HandlerType}", handler.GetType().Name);
            }
        }
    }
}