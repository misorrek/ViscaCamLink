namespace ViscaCamLink
{
    using System;
    using System.Windows;

    using ViscaCamLink.Properties;
    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(Object sender, StartupEventArgs e)
        {
            CheckSettingsUpgradeRequired();

            var viscaCamLinkView = new ViscaCamLinkView();
            var viscaCamLinkViewModel = new ViscaCamLinkViewModel();

            viscaCamLinkView.DataContext = viscaCamLinkViewModel;
            viscaCamLinkView.Closed += OnClosed;
            viscaCamLinkView.Show();
        }

        private void OnClosed(Object? sender, EventArgs e)
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
