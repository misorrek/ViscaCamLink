namespace ViscaCamLink.Common;

using System;
using System.Windows.Input;

public class Command : ICommand
{
    public Command(Action executeAction, Func<Boolean>? canExecuteFunc = null)
    {
        void wrapper(Object? parameter) => executeAction.Invoke();

        _executeAction = new Action<Object?>(wrapper);
        _canExecuteFunc = canExecuteFunc;
    }

    public Command(Action<Object?> executeAction, Func<Boolean>? canExecuteFunc = null)
    {
        _executeAction = executeAction;
        _canExecuteFunc = canExecuteFunc;
    }

    public event EventHandler? CanExecuteChanged;

    private readonly Action<Object?> _executeAction;

    private readonly Func<Boolean>? _canExecuteFunc;

    public Boolean CanExecute(Object? parameter)
    {
        return _canExecuteFunc == null || _canExecuteFunc();
    }

    public void Execute(Object? parameter)
    {
        _executeAction(parameter);
    }

    public void Invalidate()
    {
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
