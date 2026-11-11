namespace ViscaCamLink.Infrastructure.Abstractions;

using System;
using System.Threading.Tasks;

public interface IUiDispatcher
{
    Task InvokeAsync(Action action);

    void Post(Action action);
}
