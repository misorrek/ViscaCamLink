namespace ViscaCamLink
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using AutoUpdaterDotNET;

    using ViscaCamLink.Properties;
    using ViscaCamLink.Util;
    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(Object sender, StartupEventArgs startupEventArgs)
        {
            LocalizationHelper.ApplyLocalization();
            CheckSettingsUpgradeRequired();

            var viscaCamLinkView = new ViscaCamLinkView();
            var viscaCamLinkViewModel = new ViscaCamLinkViewModel();

            viscaCamLinkViewModel.UpdateAvailable += viscaCamLinkView.ShowUpdateButton;
            viscaCamLinkView.DataContext = viscaCamLinkViewModel;
            viscaCamLinkView.Closed += OnClosed;

            AutoUpdater.CheckForUpdateEvent += viscaCamLinkViewModel.AutoUpdaterOnCheckForUpdateEvent;
#if RELEASE            
            var updateXmlUrl = "https://github.com/FreakyTorial/ViscaCamLink/releases/latest/download/update-installer.xml";
#elif RELEASE_PORTABLE || DEBUG
            AutoUpdater.CheckForUpdateEvent += viscaCamLinkViewModel.AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.InstallationPath = Environment.CurrentDirectory;

            var updateXmlUrl = "https://github.com/FreakyTorial/ViscaCamLink/releases/latest/download/update-portable.xml";
#endif
            var thread = new Thread(async () =>
            {
                await Task.Delay(5000);

                viscaCamLinkView.Dispatcher.Invoke(() => AutoUpdater.Start(updateXmlUrl));
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();

            viscaCamLinkView.Show();            
        }

        private void OnClosed(Object? sender, EventArgs eventArgs)
        {
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
