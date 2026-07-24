namespace ViscaCamLink.Infrastructure.Interface;

using System;
using System.Threading.Tasks;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);

    void Post(Action action);
}
