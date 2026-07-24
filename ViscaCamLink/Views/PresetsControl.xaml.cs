namespace ViscaCamLink.Views;

using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

using ViscaCamLink.ViewModels;

public partial class PresetsControl : UserControl
{
    public PresetsControl()
    {
        InitializeComponent();
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
