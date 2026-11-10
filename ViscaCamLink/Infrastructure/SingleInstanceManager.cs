namespace ViscaCamLink.Infrastructure;

using System;
using System.Security.Principal;
using System.Threading;

public sealed class SingleInstanceManager : IDisposable
{
    private const string MutexNamePrefix = @"Local\ViscaCamLink.App.";
    private const string ActivationEventNamePrefix = @"Local\ViscaCamLink.Activate.";

    private readonly Mutex _mutex;
    private readonly EventWaitHandle _activationEvent;
    private readonly bool _ownsMutex;
    private Thread? _activationThread;
    private bool _disposed;

    public SingleInstanceManager()
    {
        var userId = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        var mutexName = MutexNamePrefix + userId;

        _activationEvent = new EventWaitHandle(
            initialState: false,
            EventResetMode.AutoReset,
            ActivationEventNamePrefix + userId);
        _mutex = new Mutex(initiallyOwned: false, mutexName);

        try
        {
            _ownsMutex = _mutex.WaitOne(0);
        }
        catch (AbandonedMutexException)
        {
            _ownsMutex = true;
        }

        if (!_ownsMutex)
        {
            _activationEvent.Set();
        }
    }

    public event EventHandler? ActivationRequested;

    public bool IsPrimaryInstance => _ownsMutex;

    public void StartListening()
    {
        if (!_ownsMutex || _activationThread is not null)
        {
            return;
        }

        _activationThread = new Thread(WaitForActivation)
        {
            IsBackground = true,
            Name = "ViscaCamLink single-instance activation"
        };
        _activationThread.Start();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_ownsMutex)
        {
            _activationEvent.Set();
        }

        _activationThread?.Join();
        _activationEvent.Dispose();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }

    private void WaitForActivation()
    {
        while (true)
        {
            _activationEvent.WaitOne();

            if (_disposed)
            {
                return;
            }

            ActivationRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
