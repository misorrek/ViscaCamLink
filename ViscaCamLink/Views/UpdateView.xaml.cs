namespace ViscaCamLink.Views;

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

using ViscaCamLink.Infrastructure;
using ViscaCamLink.ViewModels;

using WpfAnimatedGif;

public partial class UpdateView : Window
{
    private ulong? _changelogNavigationId;

    public UpdateView()
    {
        InitializeComponent();
        ChangelogWebView.CreationProperties = new CoreWebView2CreationProperties
        {
            UserDataFolder = AppPaths.WebView2DataDirectory,
        };
    }

    private void Window_ContentRendered(object sender, EventArgs eventArgs)
    {
        var animationController = ImageBehavior.GetAnimationController(UpdateAnimationImage);

        animationController.GotoFrame(0);
        animationController.Play();
    }

    private void ChangelogWebView_CoreWebView2InitializationCompleted(object? sender, CoreWebView2InitializationCompletedEventArgs eventArgs)
    {
        if (!eventArgs.IsSuccess)
        {
            ChangelogWebView.Visibility = Visibility.Collapsed;
            ChangelogFallback.Visibility = Visibility.Visible;
            return;
        }

        ChangelogWebView.CoreWebView2.NewWindowRequested += ChangelogWebView_NewWindowRequested;
        var settings = ChangelogWebView.CoreWebView2.Settings;
        settings.AreDevToolsEnabled = false;
        settings.IsPasswordAutosaveEnabled = false;
        settings.IsGeneralAutofillEnabled = false;
        settings.AreDefaultContextMenusEnabled = false;
    }

    private void ChangelogWebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs eventArgs)
    {
        if (DataContext is UpdateViewModel viewModel && viewModel.IsChangelogUrl(eventArgs.Uri))
        {
            _changelogNavigationId = eventArgs.NavigationId;
            return;
        }

        if (IsReleasePage(eventArgs.Uri))
        {
            return;
        }

        eventArgs.Cancel = true;
        OpenExternalUrl(eventArgs.Uri);
    }

    private void ChangelogWebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs eventArgs)
    {
        if (_changelogNavigationId == eventArgs.NavigationId && eventArgs.HttpStatusCode == 404
            && DataContext is UpdateViewModel viewModel)
        {
            _changelogNavigationId = null;
            var previousUrl = viewModel.ChangelogUrl;
            viewModel.ShowReleasePage();
            if (previousUrl == viewModel.ChangelogUrl)
            {
                ChangelogWebView.CoreWebView2.Navigate(viewModel.ChangelogUrl);
            }
        }
    }

    private void ChangelogWebView_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs eventArgs)
    {
        eventArgs.Handled = true;

        if (IsReleasePage(eventArgs.Uri)
            || (DataContext is UpdateViewModel viewModel && viewModel.IsChangelogUrl(eventArgs.Uri)))
        {
            ChangelogWebView.CoreWebView2.Navigate(eventArgs.Uri);
            return;
        }

        OpenExternalUrl(eventArgs.Uri);
    }

    private void ChangelogFallback_RequestNavigate(object sender, RequestNavigateEventArgs eventArgs)
    {
        OpenExternalUrl(eventArgs.Uri.AbsoluteUri);
        eventArgs.Handled = true;
    }

    private static bool IsReleasePage(string value)
    {
        const string releasePath = "/misorrek/ViscaCamLink/releases";

        return Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase)
            && uri.Port == 443
            && uri.AbsolutePath.StartsWith(releasePath, StringComparison.OrdinalIgnoreCase)
            && (uri.AbsolutePath.Length == releasePath.Length
                || uri.AbsolutePath[releasePath.Length] == '/');
    }

    private static void OpenExternalUrl(string value)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri)
            && uri.Scheme is "http" or "https")
        {
            Process.Start(new ProcessStartInfo(uri.AbsoluteUri) { UseShellExecute = true });
        }
    }
}
