namespace ViscaCamLink.Util;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);
    void Post(Action action);
}