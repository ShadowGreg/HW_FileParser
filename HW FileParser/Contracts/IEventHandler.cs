namespace HW_FileParser.Contracts;

public interface IEventHandler<TEvent>
{
    Task HandleAsync(TEvent @event);
}
