namespace ViscaCamLink.Infrastructure.Interface;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);
    
    void Post(Action action);
}