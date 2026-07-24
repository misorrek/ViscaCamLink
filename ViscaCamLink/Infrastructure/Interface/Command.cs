namespace ViscaCamLink.Infrastructure.Interface;

using System;
using System.Windows.Input;

public class Command(Action<object?> executeAction, Func<bool>? canExecuteFunc = null) : ICommand
{
    public Command(Action executeAction, Func<bool>? canExecuteFunc = null)
        : this(_ => executeAction(), canExecuteFunc)
    {
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        return canExecuteFunc is null || canExecuteFunc();
    }

    public void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        executeAction(parameter);
    }

    public void Invalidate()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
