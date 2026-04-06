namespace HW_FileParser.Service.Abstractions;
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event);
}