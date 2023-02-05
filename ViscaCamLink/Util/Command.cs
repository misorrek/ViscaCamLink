namespace ViscaCamLink.Util;

using System;
using System.Windows.Input;

public class Command : ICommand
{
    public event EventHandler? CanExecuteChanged;

    public Command(Action executeAction, Func<Boolean>? canExecuteFunc = null)
    {
        void wrapper(Object? parameter) => executeAction.Invoke();

        ExecuteAction = new Action<Object?>(wrapper);
        CanExecuteFunc = canExecuteFunc;
    }

    public Command(Action<Object?> executeAction, Func<Boolean>? canExecuteFunc = null)
    {
        ExecuteAction = executeAction;
        CanExecuteFunc = canExecuteFunc;
    }

    private Action<Object?> ExecuteAction { get; }

    private Func<Boolean>? CanExecuteFunc { get; }

    public Boolean CanExecute(Object? parameter)
    {
        return CanExecuteFunc == null || CanExecuteFunc();
    }

    public void Execute(Object? parameter)
    {
        ExecuteAction(parameter);
    }

    public void Invalidate()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
