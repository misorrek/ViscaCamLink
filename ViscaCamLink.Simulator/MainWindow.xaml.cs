namespace ViscaCamLink.Simulator;

using System;
using System.Windows;
using System.Windows.Controls;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closed += (_, _) =>
        {
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        };
    }

    private void OnTrimmedTextToolTipOpening(object sender, ToolTipEventArgs e)
    {
        var textBlock = (TextBlock)sender;
        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        if (textBlock.DesiredSize.Width <= textBlock.ActualWidth)
        {
            e.Handled = true;
        }
    }
}
