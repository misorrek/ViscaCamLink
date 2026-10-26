namespace ViscaCamLink.Views;

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using ViscaCamLink.ViewModels;

public partial class PresetsControl : UserControl
{
    private Window? _window;

    public PresetsControl()
    {
        InitializeComponent();

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        _window = Window.GetWindow(this);

        if (_window is not null)
        {
            _window.PreviewMouseDown += OnWindowPreviewMouseDown;
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        if (_window is not null)
        {
            _window.PreviewMouseDown -= OnWindowPreviewMouseDown;
            _window = null;
        }
    }

    // A click outside the rename textbox confirms the rename, like Enter.
    private void OnWindowPreviewMouseDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (DataContext is not PresetsViewModel viewModel ||
            eventArgs.OriginalSource is not DependencyObject source ||
            IsInsideTextBox(source))
        {
            return;
        }

        if (viewModel.RenamingSlotIndex >= 0)
        {
            viewModel.MemoryRenameConfirmCommand.Execute(null);
        }

        if (viewModel.PresetGroups.Any(group => group.IsRenaming))
        {
            viewModel.GroupRenameConfirmCommand.Execute(null);
        }
    }

    private static bool IsInsideTextBox(DependencyObject element)
    {
        while (element is not null)
        {
            if (element is TextBox)
            {
                return true;
            }

            element = element is System.Windows.Media.Visual or System.Windows.Media.Media3D.Visual3D
                ? System.Windows.Media.VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return false;
    }

    private void PresetButton_Click(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is Button button && DataContext is PresetsViewModel viewModel)
        {
            viewModel.MemoryCommand.Execute(button.CommandParameter);
        }
    }

    private void PresetButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs eventArgs)
    {
        if (eventArgs.ClickCount != 2 ||
            sender is not Button button ||
            DataContext is not PresetsViewModel viewModel)
        {
            return;
        }

        viewModel.MemoryRenameCommand.Execute(button.CommandParameter);
        eventArgs.Handled = true;
    }

    protected override void OnPreviewKeyDown(KeyEventArgs eventArgs)
    {
        base.OnPreviewKeyDown(eventArgs);

        if (DataContext is not PresetsViewModel viewModel)
        {
            return;
        }

        if (eventArgs.Key == Key.Return)
        {
            if (viewModel.RenamingSlotIndex >= 0)
            {
                viewModel.MemoryRenameConfirmCommand.Execute(null);
                eventArgs.Handled = true;
            }
            else if (viewModel.PresetGroups.Any(group => group.IsRenaming))
            {
                viewModel.GroupRenameConfirmCommand.Execute(null);
                eventArgs.Handled = true;
            }
        }
        else if (eventArgs.Key == Key.Escape)
        {
            if (viewModel.RenamingSlotIndex >= 0)
            {
                viewModel.MemoryRenameCancelCommand.Execute(null);
                eventArgs.Handled = true;
            }
            else if (viewModel.PresetGroups.Any(group => group.IsRenaming))
            {
                viewModel.GroupRenameCancelCommand.Execute(null);
                eventArgs.Handled = true;
            }
        }
    }

    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (sender is TextBox textBox && eventArgs.NewValue is true)
        {
            Dispatcher.InvokeAsync(() => textBox.Focus(), DispatcherPriority.Input);
        }
    }
}
