namespace ViscaCamLink.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using ViscaCamLink.Services;
using ViscaCamLink.ViewModels;

public partial class CompactWindow : Window
{
    private readonly IWindowModeCoordinator _coordinator;

    public CompactWindow(IWindowModeCoordinator coordinator)
    {
        _coordinator = coordinator;

        InitializeComponent();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        // Skip DragMove if the click originated on a Button (or a child of a Button).
        var source = eventArgs.OriginalSource as DependencyObject;

        while (source is not null)
        {
            if (source is Button)
            {
                return;
            }

            source = VisualTreeHelper.GetParent(source);
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs eventArgs)
        => _coordinator.MinimizeCompact();

    private void MaximizeButton_Click(object sender, RoutedEventArgs eventArgs)
        => _coordinator.ExitCompactMode();

    private void CloseButton_Click(object sender, RoutedEventArgs eventArgs)
        => _coordinator.CloseApplication();

    private void Window_Deactivated(object sender, EventArgs eventArgs)
    {
        if (DataContext is ViscaCamLinkViewModel viewModel && viewModel.Movement.IsMousePanning)
        {
            viewModel.Movement.MousePanEndCommand.Execute(null);
        }
    }

    private void Window_Activated(object sender, EventArgs eventArgs)
    {
        if (DataContext is ViscaCamLinkViewModel viewModel &&
            viewModel.Movement.IsMousePanning &&
            Mouse.LeftButton == MouseButtonState.Released)
        {
            viewModel.Movement.MousePanEndCommand.Execute(null);
        }
    }
}
