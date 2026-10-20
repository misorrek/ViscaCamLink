namespace ViscaCamLink;

using System;
using System.Threading;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.Infrastructure.HotKeys;
using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Logging;
using ViscaCamLink.Infrastructure.Theming;
using ViscaCamLink.Repositories.AppSettings;
using ViscaCamLink.Repositories.HotKeys;
using ViscaCamLink.Repositories.Presets;
using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;
using ViscaCamLink.Views;
using ViscaCamLink.Visca;

public partial class App : Application
{
    // Used until settings are loaded; afterwards FileLoggerProvider.MinimumLevel is the only filter and follows the Options dialog live.
    private const LogLevel BootstrapLogLevel = LogLevel.Information;

    private FileLoggerProvider? _fileLoggerProvider;
    private ILoggerFactory? _bootstrapLoggerFactory;
    private ILogger _logger = NullLogger.Instance;
    private IServiceProvider? _serviceProvider;
    private CancellationTokenSource? _updateCheckCancellation;

    private void Application_Startup(object sender, StartupEventArgs startupEventArgs)
    {
        _fileLoggerProvider = new FileLoggerProvider(AppPaths.LogsDirectory, TimeProvider.System) { MinimumLevel = BootstrapLogLevel };
        _bootstrapLoggerFactory = LoggerFactory.Create(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(_fileLoggerProvider));
        _logger = _bootstrapLoggerFactory.CreateLogger<App>();

        RegisterUnhandledExceptionHandlers();

        var settingsRepository = new AppSettingsRepository(
            AppPaths.SettingsFile,
            AppPaths.LegacyUserDataRoot,
            _bootstrapLoggerFactory.CreateLogger<AppSettingsRepository>());
        var appSettings = settingsRepository.Load();
        var themeLogger = _bootstrapLoggerFactory.CreateLogger(typeof(ThemeHelper));

        _fileLoggerProvider.MinimumLevel = appSettings.LogLevel;

        EnsureCameraProfileExists(appSettings, settingsRepository);
        LocalizationHelper.ApplyLocalization(appSettings.Language);
        ThemeHelper.ApplyTheme(appSettings.Theme, themeLogger);

        _serviceProvider = ConfigureServices(appSettings, settingsRepository, _fileLoggerProvider, themeLogger);
        _logger = _serviceProvider.GetRequiredService<ILogger<App>>();

        LogStartup(appSettings.LogLevel);

        var viscaCamLinkViewModel = _serviceProvider.GetRequiredService<ViscaCamLinkViewModel>();
        var viscaCamLinkView = _serviceProvider.GetRequiredService<ViscaCamLinkView>();
        var compactWindow = _serviceProvider.GetRequiredService<CompactWindow>();
        var windowModeCoordinator = _serviceProvider.GetRequiredService<IWindowModeCoordinator>();
        var hotKeyManager = _serviceProvider.GetRequiredService<IHotKeyManager>();
        var startupUpdateCheckService = _serviceProvider.GetRequiredService<IStartupUpdateCheckService>();

        viscaCamLinkViewModel.UpdateAvailable += viscaCamLinkView.ShowUpdateButton;
        viscaCamLinkView.DataContext = viscaCamLinkViewModel;
        viscaCamLinkView.Closed += OnClosed;

        compactWindow.DataContext = viscaCamLinkViewModel;
        windowModeCoordinator.Initialize(viscaCamLinkView, compactWindow);
        hotKeyManager.AddLocalKeyTarget(compactWindow);

        _updateCheckCancellation = new CancellationTokenSource();
        _ = startupUpdateCheckService.RunAsync(_updateCheckCancellation.Token);

        viscaCamLinkView.Show();

        ShowMigrationDialogIfNeeded(appSettings, settingsRepository,
            _serviceProvider.GetRequiredService<IDialogService>());
    }

    private static ServiceProvider ConfigureServices(
        AppSettings appSettings,
        AppSettingsRepository settingsRepository,
        FileLoggerProvider fileLoggerProvider,
        ILogger themeLogger)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .SetMinimumLevel(LogLevel.Trace)
            .AddProvider(fileLoggerProvider));

        services.AddSingleton(settingsRepository);
        services.AddSingleton(appSettings);

        services.AddSingleton<ISettingsService>(provider => new SettingsService(
            provider.GetRequiredService<AppSettings>(),
            provider.GetRequiredService<AppSettingsRepository>(),
            LocalizationHelper.ApplyLocalization,
            theme => ThemeHelper.ApplyTheme(theme, themeLogger),
            logLevel => fileLoggerProvider.MinimumLevel = logLevel,
            provider.GetRequiredService<ILogger<SettingsService>>()));

        services.AddSingleton<IViscaClient>(provider =>
        {
            var settings = provider.GetRequiredService<ISettingsService>();
            var camera = settings.ActiveCameraProfile;

            return new TcpViscaClient(
                camera?.Ip ?? CameraProfile.DefaultIp,
                camera?.Port ?? CameraProfile.DefaultPort,
                provider.GetRequiredService<ILogger<TcpViscaClient>>());
        });
        services.AddSingleton<IViscaController, ViscaController>();

        services.AddSingleton<IHotKeyManager>(provider => new HotKeyManager(provider.GetRequiredService<ViscaCamLinkView>()));
        services.AddSingleton<IHotKeyRepository, HotKeyRepository>();
        services.AddSingleton<IPresetRepository, PresetRepository>();

        services.AddSingleton<ICameraConnectionService, CameraConnectionService>();
        services.AddSingleton<ICameraMovementService, CameraMovementService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<IHotKeyService, HotKeyService>();
        services.AddSingleton<IPowerService, PowerService>();
        services.AddSingleton<IPresetService, PresetService>();
        services.AddSingleton<IStartupUpdateCheckService, StartupUpdateCheckService>();
        services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
        services.AddSingleton<IUpdateService>(provider => new VelopackUpdateService(
            provider.GetRequiredService<IVelopackUpdateManager>(),
            () => RequestShutdownForUpdate(provider),
            provider.GetRequiredService<ILogger<VelopackUpdateService>>()));
        services.AddSingleton<IVelopackUpdateManager, VelopackUpdateManager>();
        services.AddSingleton<IWindowModeCoordinator, WindowModeCoordinator>();

        services.AddSingleton<ViscaCamLinkViewModel>();
        services.AddSingleton<ConnectionViewModel>();
        services.AddSingleton<PresetsViewModel>();
        services.AddSingleton<MovementViewModel>();
        services.AddSingleton<ZoomViewModel>();
        services.AddSingleton<ViscaCamLinkView>();
        services.AddSingleton<CompactWindow>();

        return services.BuildServiceProvider();
    }

    // Called from the update service after the Velopack updater was launched (any thread). Closing the main window
    // runs the normal shutdown path (window placement and settings saved in Closing, App.OnClosed disposal).
    private static void RequestShutdownForUpdate(IServiceProvider provider) =>
        Current.Dispatcher.BeginInvoke(() => provider.GetRequiredService<ViscaCamLinkView>().Close());

    private static void EnsureCameraProfileExists(AppSettings appSettings, AppSettingsRepository settingsRepository)
    {
        if (appSettings.CameraProfiles.Count > 0)
        {
            return;
        }

        var defaultCamera = new CameraProfile();

        appSettings.CameraProfiles.Add(defaultCamera);
        appSettings.ActiveCameraProfileId = defaultCamera.Id;
        settingsRepository.Save(appSettings);
    }

    private void RegisterUnhandledExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    private void UnregisterUnhandledExceptionHandlers()
    {
        DispatcherUnhandledException -= OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException -= OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs eventArgs) =>
        LogDispatcherUnhandledException(_logger, eventArgs.Exception);

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs eventArgs) =>
        LogAppDomainUnhandledException(_logger, eventArgs.IsTerminating, eventArgs.ExceptionObject as Exception);

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs eventArgs) =>
        LogUnobservedTaskException(_logger, eventArgs.Exception);

    private void LogStartup(LogLevel configuredLogLevel)
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? UnknownVersion;
        var isVelopackInstalled = VelopackLocator.IsCurrentSet && VelopackLocator.Current.CurrentlyInstalledVersion is not null;

        LogApplicationStarting(_logger, version, isVelopackInstalled, configuredLogLevel);
        LogDataPaths(_logger, AppPaths.SettingsFile, AppPaths.PresetsDirectory, AppPaths.HotKeysFile, AppPaths.LogsDirectory);
    }

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        LogApplicationShuttingDown(_logger);
        ThemeHelper.StopFollowingSystemTheme();

        _updateCheckCancellation?.Cancel();
        _updateCheckCancellation?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

        UnregisterUnhandledExceptionHandlers();

        // Neither logger factory disposes a provider instance passed via AddProvider, so the App owns it.
        _logger = NullLogger.Instance;
        _bootstrapLoggerFactory?.Dispose();
        _fileLoggerProvider?.Dispose();

        Current.Shutdown();
    }

    private static void ShowMigrationDialogIfNeeded(
        AppSettings appSettings,
        AppSettingsRepository settingsRepository,
        IDialogService dialogService)
    {
        if (appSettings.WixUninstallPrompted)
        {
            return;
        }

        var uninstallString = FindWixUninstallString();

        // Mark as prompted regardless of whether WiX was found so this
        // code path only runs once per installation.
        appSettings.WixUninstallPrompted = true;
        settingsRepository.Save(appSettings);

        if (uninstallString is not null)
        {
            dialogService.ShowMigrationDialog(uninstallString);
        }
    }

    private static string? FindWixUninstallString()
    {
        string[] searchPaths =
        [
            @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
            @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall",
        ];

        foreach (var searchPath in searchPaths)
        {
            using var parentKey = Registry.LocalMachine.OpenSubKey(searchPath);

            if (parentKey is null)
            {
                continue;
            }

            foreach (var subKeyName in parentKey.GetSubKeyNames())
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);

                if (subKey?.GetValue("DisplayName") is string displayName
                    && displayName.Equals("ViscaCamLink", StringComparison.OrdinalIgnoreCase)
                    && subKey.GetValue("UninstallString") is string uninstallString)
                {
                    return uninstallString;
                }
            }
        }

        return null;
    }

    [LoggerMessage(EventId = 5001, Level = LogLevel.Information, Message = "ViscaCamLink {Version} starting (Velopack installed: {IsVelopackInstalled}, log level: {LogLevel})")]
    private static partial void LogApplicationStarting(ILogger logger, string version, bool isVelopackInstalled, LogLevel logLevel);

    [LoggerMessage(EventId = 5002, Level = LogLevel.Information, Message = "Data paths: settings '{SettingsFile}', presets '{PresetsDirectory}', hotkeys '{HotKeysFile}', logs '{LogsDirectory}'")]
    private static partial void LogDataPaths(ILogger logger, string settingsFile, string presetsDirectory, string hotKeysFile, string logsDirectory);

    [LoggerMessage(EventId = 5003, Level = LogLevel.Information, Message = "ViscaCamLink shutting down")]
    private static partial void LogApplicationShuttingDown(ILogger logger);

    [LoggerMessage(EventId = 5004, Level = LogLevel.Critical, Message = "Unhandled exception on the UI thread")]
    private static partial void LogDispatcherUnhandledException(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 5005, Level = LogLevel.Critical, Message = "Unhandled exception in the application domain (terminating: {IsTerminating})")]
    private static partial void LogAppDomainUnhandledException(ILogger logger, bool isTerminating, Exception? exception);

    [LoggerMessage(EventId = 5006, Level = LogLevel.Error, Message = "Unobserved task exception")]
    private static partial void LogUnobservedTaskException(ILogger logger, Exception exception);
}
