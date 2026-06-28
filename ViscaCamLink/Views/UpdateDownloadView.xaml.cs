namespace ViscaCamLink.Views;

using System;
using System.Windows;

using ViscaCamLink.ViewModels;

/// <summary>
/// Interaction logic for UpdateDownloadView.xaml
/// </summary>
public partial class UpdateDownloadView : Window
{
    public UpdateDownloadView()
    {
        InitializeComponent();
    }

    private void Window_ContentRendered(object sender, EventArgs e)
    {
        if (DataContext is UpdateDownloadViewModel vm)
            _ = vm.StartDownloadAsync();
    }
}
