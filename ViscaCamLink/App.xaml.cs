namespace VirtualCamControl
{
    using System;
    using System.Windows;

    using ViscaCamLink.Configuration;
    using ViscaCamLink.ViewModels;
    using ViscaCamLink.Views;

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(Object sender, StartupEventArgs e)
        {
            var appConfiguration = new AppConfiguration();
            var viscaCamLinkView = new ViscaCamLinkView();
            var viscaCamLinkViewModel = new ViscaCamLinkViewModel(appConfiguration);

            viscaCamLinkView.DataContext = viscaCamLinkViewModel;
            viscaCamLinkView.Closed += OnClosed;
            viscaCamLinkView.Show();
        }

        private void OnClosed(Object? sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
