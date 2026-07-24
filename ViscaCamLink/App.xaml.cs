namespace ViscaCamLink;

using System;
using System.Threading;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
    private IServiceProvider? _serviceProvider;
    private CancellationTokenSource? _updateCheckCancellation;

    private void Application_Startup(object sender, StartupEventArgs startupEventArgs)
    {
        var settingsRepository = new AppSettingsRepository(AppPaths.SettingsFile, AppPaths.LegacyUserDataRoot);
        var appSettings = settingsRepository.Load();

        EnsureCameraProfileExists(appSettings, settingsRepository);
        LocalizationHelper.ApplyLocalization(appSettings.Language);
        ThemeHelper.ApplyTheme(appSettings.Theme);

        _serviceProvider = ConfigureServices(appSettings, settingsRepository);

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

    private static ServiceProvider ConfigureServices(AppSettings appSettings, AppSettingsRepository settingsRepository)
    {
        var services = new ServiceCollection();

        services.AddLogging(builder => builder
            .SetMinimumLevel(appSettings.LogLevel)
            .AddProvider(new FileLoggerProvider(AppPaths.LogsDirectory, TimeProvider.System)));

        services.AddSingleton(settingsRepository);
        services.AddSingleton(appSettings);

        services.AddSingleton<ISettingsService>(provider => new SettingsService(
            provider.GetRequiredService<AppSettings>(),
            provider.GetRequiredService<AppSettingsRepository>(),
            LocalizationHelper.ApplyLocalization,
            ThemeHelper.ApplyTheme));

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
        services.AddSingleton<IUpdateService, VelopackUpdateService>();
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

    private void OnClosed(object? sender, EventArgs eventArgs)
    {
        _updateCheckCancellation?.Cancel();
        _updateCheckCancellation?.Dispose();

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }

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
}
