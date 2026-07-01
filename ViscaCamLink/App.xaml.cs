namespace ViscaCamLink
{
    using System.Net.Http.Headers;
    using System.IO;
    using System.Windows;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using ViscaCamLink.Services;
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
            var settingsRepository = new AppSettingsRepository(AppPaths.Settings, AppPaths.LegacyUserDataRoot);
            var appSettings = settingsRepository.Load();
            LocalizationHelper.ApplyLocalization(appSettings.Language);

            _serviceProvider = ConfigureServices(appSettings, settingsRepository);

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
            services.AddSingleton<IViscaClient>(sp => new TcpViscaClient(
                appSettings.Ip, appSettings.Port,
                sp.GetRequiredService<ILogger<TcpViscaClient>>()));
            services.AddSingleton<IViscaController>(sp => new ViscaController(
                sp.GetRequiredService<IViscaClient>(),
                sp.GetRequiredService<ILogger<ViscaController>>()));
            services.AddSingleton<ICameraConnectionService, CameraConnectionService>();
            services.AddSingleton<IPowerService, PowerService>();
            services.AddSingleton<IPresetService, PresetService>();
            services.AddSingleton<ICameraMovementService, CameraMovementService>();
            services.AddSingleton<IGlobalHotKeyManager>(sp => new GlobalHotKeyManager(sp.GetRequiredService<ViscaCamLinkView>()));
            services.AddSingleton<IHotKeyRepository, HotKeyRepository>();
            services.AddSingleton<IHotKeyService, HotKeyService>();
            services.AddSingleton<IUiDispatcher, WpfUiDispatcher>();
            services.AddSingleton<IDialogService, DialogService>();
            services.AddSingleton<IOptionsViewModelFactory, OptionsViewModelFactory>();
            services.AddSingleton<IStartupUpdateCheckService>(sp =>
                new StartupUpdateCheckService(sp.GetRequiredService<IUpdateService>()));
            services.AddSingleton<VelopackUpdateService>();
            services.AddSingleton<IUpdateService>(sp => sp.GetRequiredService<VelopackUpdateService>());
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

            Current.Shutdown();
        }
    }
}
