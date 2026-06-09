namespace ViscaCamLink.Updater;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Updater.Common;
using ViscaCamLink.Updater.Factories;
using ViscaCamLink.Updater.Manager;

public class ApplicationUpdater : IApplicationUpdater
{
    public ApplicationUpdater(
        IApplicationUpdaterOptions options,
        IApplicationUpdaterClientFactory clientFactory,
        IUpdaterViewFactory updaterViewFactory, 
        ILogger<IApplicationUpdater> logger)
    {
        _options = options;
        _clientFactory = clientFactory;
        _updaterViewFactory = updaterViewFactory;
        _logger = logger;
        _isRunning = false;
    }

    public event EventHandler<UpdateInfoEventArgs>? CheckForUpdateEvent;

    private readonly IApplicationUpdaterOptions _options;

    private readonly IApplicationUpdaterClientFactory _clientFactory;

    private readonly IUpdaterViewFactory _updaterViewFactory;

    private readonly ILogger<IApplicationUpdater> _logger;

    private Boolean _isRunning;

    private Version? _assemblyVersion;

    private void Start()
    {
        if (_isRunning)
        {
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;
        }
        catch (NotSupportedException exception)
        {
            _logger.LogError(exception, "None of the following SecurityProtocolTypes are supported: Tls, Tls11, Tls12. Update process cancelled.");
        }

        _isRunning = true;
        _assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;

        if (_options.RunSynchronous)
        {
            try
            {
                var updateInfoEventArgs = CheckUpdate();

                if (updateInfoEventArgs == null || !HandleUpdateInfo(updateInfoEventArgs))
                {
                    _isRunning = false;
                }
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, String.Empty);
            }

            return;
        }

        using (var backgroundWorker = new BackgroundWorker())
        {
            backgroundWorker.DoWork += delegate (object? _, DoWorkEventArgs args)
            {
                args.Result = CheckUpdate();
            };

            backgroundWorker.RunWorkerCompleted += delegate (object? _, RunWorkerCompletedEventArgs args)
            {
                if (args.Error != null)
                {
                    _logger.LogError(args.Error, String.Empty);
                }
                else if (!args.Cancelled && args.Result is UpdateInfoEventArgs updateInfoEventArgs && HandleUpdateInfo(updateInfoEventArgs))
                {
                    return;
                }
                _isRunning = false;
            };

            backgroundWorker.RunWorkerAsync();
        }
    }

    private UpdateInfoEventArgs? CheckUpdate()
    {
        var client = _clientFactory.CreateClient(_options.UpdateXmlUrl);
        var updateXml = client.GetUpdateXmlAsync(CancellationToken.None).Result;

        if (updateXml == null)
        {
            _logger.LogError("No valid update infos recieved from '{url}'", _options.UpdateXmlUrl);

            return null;
        }

        try
        {
            var updateInfoEventArgs = new UpdateInfoEventArgs(updateXml, _assemblyVersion);

            return updateInfoEventArgs;
        }
        catch (MissingFieldException exception)
        {
            _logger.LogError(exception, "Missing available version or download url in update infos pulled from '{url}'", _options.UpdateXmlUrl);
        }

        return null;
    }

    private Boolean HandleUpdateInfo(UpdateInfoEventArgs updateInfoEventArgs)
    {
        if (CheckForUpdateEvent != null)
        {
            CheckForUpdateEvent.Invoke(this, updateInfoEventArgs);
        }
        else
        {
            if (updateInfoEventArgs.IsUpdateAvailable)
            {
                
                //TODO Exit();

                return ShowUpdateDialog(updateInfoEventArgs);
            }
        }

        return false;
    }

    private Boolean ShowUpdateDialog(UpdateInfoEventArgs updateInfoEventArgs)
    {
        var updaterView = _updaterViewFactory.CreateViewAndAttachViewModel(null, updateInfoEventArgs);

        return updaterView.ShowDialog() ?? false;
    }

    /// <summary>
    ///     Detects and exits all instances of running assembly, including current.
    /// </summary>
    internal static void Exit()
    {
        var currentProcess = Process.GetCurrentProcess();
        foreach (Process process in Process.GetProcessesByName(currentProcess.ProcessName))
        {
            string processPath;
            try
            {
                processPath = process.MainModule?.FileName;
            }
            catch (Win32Exception)
            {
                // Current process should be same as processes created by other instances of the application so it should be able to access modules of other instances. 
                // This means this is not the process we are looking for so we can safely skip this.
                continue;
            }

            // Get all instances of assembly except current
            if (process.Id == currentProcess.Id || currentProcess.MainModule?.FileName != processPath)
            {
                continue;
            }

            if (process.CloseMainWindow())
            {
                process.WaitForExit((int)TimeSpan.FromSeconds(10)
                    .TotalMilliseconds); // Give some time to process message
            }

            if (!process.HasExited)
            {
                process.Kill(); //TODO: Show UI message asking user to close program himself instead of silently killing it
            }
        }

        /*
        if (ApplicationExitEvent != null)
        {
            ApplicationExitEvent();
        }
        else
        {
            if (_isWinFormsApplication)
            {
                MethodInvoker methodInvoker = Application.Exit;
                methodInvoker.Invoke();
            }
            else if (System.Windows.Application.Current != null)
            {
                System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    System.Windows.Application.Current.Shutdown()));
            }
            else
            {
                Environment.Exit(0);
            }
        }*/
    }
}
