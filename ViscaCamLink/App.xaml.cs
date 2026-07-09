namespace ViscaCamLink
{
    using System.Net.Http.Headers;
    using System.IO;
    using System.Windows;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.Win32;

    using ViscaCamLink.Services;
    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;
    using ViscaCamLink.Visca;
    using ViscaCamLink.Repositories.AppSettings;
    using ViscaCamLink.Repositories.HotKeys;
    using ViscaCamLink.Repositories.Presets;
    using ViscaCamLink.Infrastructure;
    using ViscaCamLink.Infrastructure.Localization;
    using ViscaCamLink.Infrastructure.Interface;
    using ViscaCamLink.Infrastructure.Logging;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private CancellationTokenSource? _updateCheckCts;

        private void Application_Startup(object sender, StartupEventArgs startupEventArgs)
        {
            var settingsRepository = new AppSettingsRepository(AppPaths.Settings, AppPaths.LegacyUserDataRoot);
            var appSettings = settingsRepository.Load();

            // Ensure there is always at least one camera profile.
            if (appSettings.CameraProfiles.Count == 0)
            {
                var defaultCamera = new CameraProfile();

                appSettings.CameraProfiles.Add(defaultCamera);
                appSettings.ActiveCameraProfileId = defaultCamera.Id;
                settingsRepository.Save(appSettings);
            }

            LocalizationHelper.ApplyLocalization(appSettings.Language);

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

            _updateCheckCts = new CancellationTokenSource();
            _ = startupUpdateCheckService.RunAsync(_updateCheckCts.Token);

            viscaCamLinkView.Show();

            ShowMigrationDialogIfNeeded(appSettings, settingsRepository,
                _serviceProvider.GetRequiredService<IDialogService>());
        }

        // TODO Maybe use IInjectable?
        private static ServiceProvider ConfigureServices(AppSettings appSettings, AppSettingsRepository settingsRepository)
        {
            var services = new ServiceCollection();

            services.AddLogging(b => b
                .SetMinimumLevel(appSettings.LogLevel)
                .AddProvider(new FileLoggerProvider(AppPaths.Logs, TimeProvider.System)));

            services.AddSingleton(settingsRepository);
            services.AddSingleton(appSettings);
            services.AddSingleton<ISettingsService>(sp => new SettingsService(
                sp.GetRequiredService<AppSettings>(),
                sp.GetRequiredService<AppSettingsRepository>(),
                LocalizationHelper.ApplyLocalization));
            services.AddSingleton<IPresetRepository, PresetRepository>();
            services.AddSingleton<IViscaClient>(sp =>
            {
                var settings = sp.GetRequiredService<ISettingsService>();
                var camera = settings.ActiveCameraProfile;

                return new TcpViscaClient(
                    camera?.Ip ?? CameraProfile.DefaultIp,
                    camera?.Port ?? CameraProfile.DefaultPort,
                    sp.GetRequiredService<ILogger<TcpViscaClient>>());
            });
            services.AddSingleton<IViscaController>(sp => new ViscaController(
                sp.GetRequiredService<IViscaClient>(),
                sp.GetRequiredService<ILogger<ViscaController>>()));
            services.AddSingleton<ICameraConnectionService>(sp => new CameraConnectionService(
                sp.GetRequiredService<IViscaController>(),
                sp.GetRequiredService<ISettingsService>(),
                sp.GetRequiredService<IPresetService>()));
            services.AddSingleton<IPowerService, PowerService>();
            services.AddSingleton<IPresetService>(sp => new PresetService(
                sp.GetRequiredService<IViscaController>(),
                sp.GetRequiredService<IPresetRepository>(),
                sp.GetRequiredService<ISettingsService>()));
            services.AddSingleton<ICameraMovementService, CameraMovementService>();
            services.AddSingleton<IHotKeyManager>(sp => new HotKeyManager(sp.GetRequiredService<ViscaCamLinkView>()));
            services.AddSingleton<IHotKeyRepository, HotKeyRepository>();
            services.AddSingleton<IHotKeyService, HotKeyService>();
            services.AddSingleton<IWindowModeCoordinator, WindowModeCoordinator>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IOptionsViewModelFactory, OptionsViewModelFactory>();
            services.AddSingleton<IStartupUpdateCheckService>(sp =>
                new StartupUpdateCheckService(sp.GetRequiredService<IUpdateService>()));
            services.AddSingleton<VelopackUpdateService>();
            services.AddSingleton<IUpdateService>(sp => sp.GetRequiredService<VelopackUpdateService>());
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<ViscaCamLinkViewModel>();
            services.AddSingleton<ConnectionViewModel>();
            services.AddSingleton<PresetsViewModel>();
            services.AddSingleton<MovementViewModel>();
            services.AddSingleton<ZoomViewModel>();
            services.AddSingleton<ViscaCamLinkView>();
            services.AddSingleton<CompactWindow>();

            return services.BuildServiceProvider();
        }

        private void OnClosed(object? sender, EventArgs eventArgs)
        {
            _updateCheckCts?.Cancel();
            _updateCheckCts?.Dispose();

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
                using var parent = Registry.LocalMachine.OpenSubKey(searchPath);
                if (parent is null)
                {
                    continue;
                }

                foreach (var name in parent.GetSubKeyNames())
                {
                    using var key = parent.OpenSubKey(name);
                    if (key?.GetValue("DisplayName") is string displayName
                        && displayName.Equals("ViscaCamLink", StringComparison.OrdinalIgnoreCase)
                        && key.GetValue("UninstallString") is string uninstallString)
                    {
                        return uninstallString;
                    }
                }
            }

            return null;
        }
    }
}
