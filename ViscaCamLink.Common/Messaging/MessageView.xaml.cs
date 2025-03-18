namespace ViscaCamLink.Common;

using System.Windows;

/// <summary>
/// Interaction logic for MessageView.xaml
/// </summary>
public partial class MessageView : Window
{
    public MessageView()
    {
        InitializeComponent();

        Icon = Application.Current.MainWindow.Icon;
    }
}
