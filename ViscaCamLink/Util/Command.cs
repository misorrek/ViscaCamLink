namespace ViscaCamLink.Util;

using System;
using System.Windows.Input;

public class Command : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public Command(Action executeAction, Func<bool>? canExecuteFunc = null)
    {
        void wrapper(object? parameter) => executeAction.Invoke();

        ExecuteAction = new Action<object?>(wrapper);
        CanExecuteFunc = canExecuteFunc;
    }

    public Command(Action<object?> executeAction, Func<bool>? canExecuteFunc = null)
    {
        ExecuteAction = executeAction;
        CanExecuteFunc = canExecuteFunc;
    }

    private Action<object?> ExecuteAction { get; }

    private Func<bool>? CanExecuteFunc { get; }

    public bool CanExecute(object? parameter)
    {
        return CanExecuteFunc == null || CanExecuteFunc();
    }

    public void Execute(object? parameter)
    {
        ExecuteAction(parameter);
    }

    public void Invalidate()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
