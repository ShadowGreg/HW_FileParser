namespace HW_FileParser.Service;
public interface IEventHandler<TEvent>
{
    Task HandleAsync(TEvent @event);
}