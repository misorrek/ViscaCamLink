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

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (DataContext is not PresetsViewModel vm) return;

        if (e.Key == Key.Return)
        {
            if (vm.RenamingSlotIndex >= 0)
            {
                vm.MemoryRenameConfirmCommand.Execute(null);
                e.Handled = true;
            }
            else if (vm.PresetGroups.Any(g => g.IsRenaming))
            {
                vm.GroupRenameConfirmCommand.Execute(null);
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Escape)
        {
            if (vm.RenamingSlotIndex >= 0)
            {
                vm.MemoryRenameCancelCommand.Execute(null);
                e.Handled = true;
            }
            else if (vm.PresetGroups.Any(g => g.IsRenaming))
            {
                vm.GroupRenameCancelCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    private void RenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is TextBox tb && (bool)e.NewValue)
        {
            Dispatcher.InvokeAsync(() => tb.Focus(), DispatcherPriority.Input);
        }
    }
}
