using HW_FileParser.Service.Abstractions;

namespace HW_FileParser.Service;
public class EventBus: IEventBus
{
    private readonly IServiceScopeFactory _scopeFactory;

    public EventBus(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public async Task PublishAsync<TEvent>(TEvent @event)
    {
        using var scope = _scopeFactory.CreateScope();

        var handlers = scope.ServiceProvider
                            .GetServices<IEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(@event);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in event handler {handler.GetType().Name}: {ex.Message}");
            }
        }
    }
}