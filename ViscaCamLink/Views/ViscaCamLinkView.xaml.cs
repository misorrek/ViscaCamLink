namespace ViscaCamLink.Views;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using ViscaCamLink.ViewModels;
using WpfAnimatedGif;

public partial class ViscaCamLinkView : Window
{
    public ViscaCamLinkView()
    {
        InitializeComponent();
    }

    private void Window_LayoutUpdated(object sender, EventArgs e)
    {
        SizeToContent = SizeToContent.Height;
    }

    public void ShowUpdateButton()
    {
        var updateButtonTemplate = UpdateButton.Template;
        var updateImageControl = (Image)updateButtonTemplate.FindName("UpdateImage", UpdateButton);
        var animationController = ImageBehavior.GetAnimationController(updateImageControl);

        UpdateButton.Visibility = Visibility.Visible;

        animationController.GotoFrame(0);
        animationController.Play();
    }

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
