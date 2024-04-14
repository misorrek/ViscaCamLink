namespace ViscaCamLink.Views;

using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

using WpfAnimatedGif;

/// <summary>
/// Interaction logic for ViscaCamLinkView.xaml
/// </summary>
public partial class ViscaCamLinkView : Window
{
    public ViscaCamLinkView()
    {            
        InitializeComponent();
    }

    private readonly Regex IpRegex = new (@"^(?:\d{1,3}\.){0,3}\d{0,3}$");
    private readonly Regex PortRegex = new(@"\b[1-9]\d{0,4}\b");

    private void Window_LayoutUpdated(Object sender, EventArgs e)
    {
        SizeToContent = SizeToContent.Height; // To only allow horizontal resize
    }

    private void TextBox_PreviewTextInput_Ip(Object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var fullText = GetFullText(textBox, e.Text);

            e.Handled = !IsTextIp(fullText);
        }
    }

    private void TextBox_PreviewTextInput_Port(Object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var fullText = GetFullText(textBox, e.Text);

            e.Handled = !IsTextPort(fullText);
        }
    }

    private void TextBox_PastingHandler_Ip(Object sender, DataObjectPastingEventArgs e)
    {
        PastingHandler(sender, e, IsTextIp);
    }

    private void TextBox_PastingHandler_Port(Object sender, DataObjectPastingEventArgs e)
    {
        PastingHandler(sender, e, IsTextPort);
    }

    private static void PastingHandler(Object sender, DataObjectPastingEventArgs e, Func<String, Boolean> pastedTextTester)
    {
        if (sender is TextBox textBox && e.DataObject.GetDataPresent(typeof(String)))
        {
            String text = (String)e.DataObject.GetData(typeof(String));
            var fullText = GetFullText(textBox, text);

            if (!pastedTextTester(fullText))
            {
                e.CancelCommand();
            }
        }
        else
        {
            e.CancelCommand();
        }
    }

    private static String GetFullText(TextBox textBox, String inputText)
    {
        var selectionStart = textBox.Text.IndexOf(textBox.SelectedText);
        var selectionClearedText = textBox.Text.Remove(selectionStart, textBox.SelectedText.Length);

        return selectionClearedText.Insert(textBox.CaretIndex, inputText);
    }

    private Boolean IsTextIp(String text)
    {
        return IpRegex.IsMatch(text);
    }

    private Boolean IsTextPort(String text)
    {
        return PortRegex.IsMatch(text);
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
}
