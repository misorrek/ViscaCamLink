namespace ViscaCamLink.Views;

using System;
using System.Windows;

using ViscaCamLink.ViewModels;

public partial class UpdateDownloadView : Window
{
    public UpdateDownloadView()
    {
        InitializeComponent();
    }

    private void Window_ContentRendered(object sender, EventArgs eventArgs)
    {
        if (DataContext is UpdateDownloadViewModel viewModel)
        {
            _ = viewModel.StartDownloadAsync();
        }
    }
}
