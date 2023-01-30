namespace ViscaCamLink.Views
{
    using System;
    using System.Windows;

    using WpfAnimatedGif;

    /// <summary>
    /// Interaction logic for UpdateView.xaml
    /// </summary>
    public partial class UpdateView : Window
    {
        public UpdateView()
        {
            InitializeComponent();
        }

        private void Window_Loaded(Object sender, RoutedEventArgs e)
        {
            //StartUpdateAnimation();
        }

        private void StartUpdateAnimation()
        {
            var animationController = ImageBehavior.GetAnimationController(UpdateAnimationImage);

            animationController.GotoFrame(0);
            animationController.Play();
        }

        private void Window_ContentRendered(Object sender, EventArgs e)
        {
            StartUpdateAnimation();
        }
    }    
}
