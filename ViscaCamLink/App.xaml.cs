namespace ViscaCamLink
{
    using System.Net.Http;
    using System.IO;
    using System.Reflection;
    using System.Windows;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ViscaCamLink.Properties;
    using ViscaCamLink.Services;
    using ViscaCamLink.Updater;
    using ViscaCamLink.Util;
    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;
    using ViscaCamLink.Visca;
    using ViscaCamLink.Repositories;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private IServiceProvider? _serviceProvider;
        private CancellationTokenSource? _updateCheckCts;

        private void Application_Startup(object sender, StartupEventArgs startupEventArgs)
        {
            LocalizationHelper.ApplyLocalization();
            CheckSettingsUpgradeRequired();

            _serviceProvider = ConfigureServices();

            var viscaCamLinkViewModel = _serviceProvider.GetRequiredService<ViscaCamLinkViewModel>();
            var viscaCamLinkView = _serviceProvider.GetRequiredService<ViscaCamLinkView>();
            var startupUpdateCheckService = _serviceProvider.GetRequiredService<IStartupUpdateCheckService>();

            viscaCamLinkViewModel.UpdateAvailable += viscaCamLinkView.ShowUpdateButton;
            viscaCamLinkView.DataContext = viscaCamLinkViewModel;
            viscaCamLinkView.Closed += OnClosed;

            _updateCheckCts = new CancellationTokenSource();
            _ = startupUpdateCheckService.RunAsync(_updateCheckCts.Token);

            viscaCamLinkView.Show();
        }

        private static IServiceProvider ConfigureServices()
        {
            var services = new ServiceCollection();

            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ViscaCamLink", "logs");
            services.AddLogging(b => b
                .SetMinimumLevel(LogLevel.Debug)
                .AddProvider(new FileLoggerProvider(logDir)));

            services.AddSingleton<ISettingsService>(_ => new SettingsService(LocalizationHelper.ApplyLocalization));
            services.AddSingleton<IPresetRepository, PresetRepository>();
            services.AddSingleton<IViscaClient>(sp => new TcpViscaClient(
                Settings.Default.Ip, Settings.Default.Port,
                sp.GetRequiredService<ILogger<TcpViscaClient>>(), sendLock: null));
            services.AddSingleton<IViscaController, ViscaController>();
            services.AddSingleton<ICameraConnectionService, CameraConnectionService>();
            services.AddSingleton<IPowerService, PowerService>();
            services.AddSingleton<IPresetService, PresetService>();
            services.AddSingleton<ICameraMovementService, CameraMovementService>();
            services.AddSingleton<IGlobalHotKeyManager>(sp => new GlobalHotKeyManager(sp.GetRequiredService<ViscaCamLinkView>()));
            services.AddSingleton<IHotKeyRepository, HotKeyRepository>();
            services.AddSingleton<IHotKeyService, HotKeyService>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<HttpClient>();
            services.AddSingleton<IGitHubUpdateChecker, GitHubUpdateChecker>();
            services.AddSingleton<IUpdateDownloader, UpdateDownloader>();
            services.AddSingleton<IInstallerLauncher, InstallerLauncher>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IOptionsViewModelFactory, OptionsViewModelFactory>();
            services.AddSingleton<IUpdateViewModelFactory, UpdateViewModelFactory>();
            services.AddSingleton<IUpdateDownloadViewModelFactory, UpdateDownloadViewModelFactory>();
            services.AddSingleton<IStartupUpdateCheckService>(sp =>
                new StartupUpdateCheckService(sp.GetRequiredService<IUpdateService>()));

#if USE_VELOPACK
            // Velopack: delta updates, in-process apply, silent restart.
            // Requires the app to have been packaged with vpk (see ViscaCamLink.Installer.Velopack/).
            services.AddSingleton<VelopackUpdateService>();
            services.AddSingleton<IUpdateService>(sp => sp.GetRequiredService<VelopackUpdateService>());
#else
            // Default: GitHub Releases API check + separate installer download/launch.
            services.AddSingleton<IUpdateService>(sp => new UpdateService(
                sp.GetRequiredService<IGitHubUpdateChecker>(),
                Assembly.GetEntryAssembly()!.GetName().Version!));
#endif
            services.AddSingleton<ViscaCamLinkViewModel>();
            services.AddSingleton<ConnectionViewModel>();
            services.AddSingleton<PresetsViewModel>();
            services.AddSingleton<MovementViewModel>();
            services.AddSingleton<ZoomViewModel>();
            services.AddSingleton<ViscaCamLinkView>();

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

            Settings.Default.Save();
            Application.Current.Shutdown();
        }

        private static void CheckSettingsUpgradeRequired()
        {
            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.LastUpgrade = DateTime.Now;
                Settings.Default.Save();
            }
        }
    }
}
