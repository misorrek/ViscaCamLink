namespace ViscaCamLink.Views;

using System.Windows;
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

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Skip DragMove if the click originated on a Button (or a child of a Button).
        DependencyObject? source = e.OriginalSource as DependencyObject;
        while (source is not null)
        {
            if (source is System.Windows.Controls.Button) return;
            source = VisualTreeHelper.GetParent(source);
        }

        DragMove();
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => _coordinator.MinimizeCompact();

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => _coordinator.ExitCompactMode();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => _coordinator.CloseApplication();

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (DataContext is ViscaCamLinkViewModel vm && vm.Movement.IsMousePanning)
            vm.Movement.MousePanEndCommand.Execute(null);
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        if (DataContext is ViscaCamLinkViewModel vm &&
            vm.Movement.IsMousePanning &&
            Mouse.LeftButton == MouseButtonState.Released)
        {
            vm.Movement.MousePanEndCommand.Execute(null);
        }
    }
}
