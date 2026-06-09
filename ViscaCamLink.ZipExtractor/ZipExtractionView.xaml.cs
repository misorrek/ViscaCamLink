namespace ViscaCamLink.ZipExtractor;

using System;
using System.ComponentModel;
using System.Windows;

/// <summary>
/// Interaction logic for ZipExtractionView.xaml
/// </summary>
public partial class ZipExtractionView : Window
{
    public ZipExtractionView()
    {
        InitializeComponent();
    }

    private void Window_Loaded(Object sender, RoutedEventArgs e)
    {
        ((ZipExtractionViewModel)this.DataContext).ExtractionCommand.Execute(null);
    }

    private void Window_Closing(Object sender, CancelEventArgs e)
    {
        e.Cancel = ((ZipExtractionViewModel)this.DataContext).ExtractionRunning;
    }
}
