namespace ViscaCamLink.Views
{
    using System.Windows;

    /// <summary>
    /// Interaction logic for ViscaCamLinkView.xaml
    /// </summary>
    public partial class ViscaCamLinkView : Window
    {
        public ViscaCamLinkView()
        {            
            InitializeComponent();
        }

        private void Window_LayoutUpdated(System.Object sender, System.EventArgs e)
        {
            SizeToContent = SizeToContent.Height; // To only allow horizontal resize
        }
    }
}
