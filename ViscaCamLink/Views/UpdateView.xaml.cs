namespace ViscaCamLink.Views;

using System;
using System.Windows;

using WpfAnimatedGif;

public partial class UpdateView : Window
{
    public UpdateView()
    {
        InitializeComponent();
    }

    private void Window_ContentRendered(object sender, EventArgs eventArgs)
    {
        var animationController = ImageBehavior.GetAnimationController(UpdateAnimationImage);

        animationController.GotoFrame(0);
        animationController.Play();
    }
}
