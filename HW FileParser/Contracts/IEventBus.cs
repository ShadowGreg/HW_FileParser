namespace HW_FileParser.Contracts;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default);
}
