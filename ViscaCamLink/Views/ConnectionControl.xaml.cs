namespace ViscaCamLink.Views;

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class ConnectionControl : UserControl
{
    public ConnectionControl()
    {
        InitializeComponent();
    }

    private readonly Regex IpRegex = new(@"^(?:\d{1,3}\.){0,3}\d{0,3}$");
    private readonly Regex PortRegex = new(@"\b[1-9]\d{0,4}\b");

    private void TextBox_PreviewTextInput_Ip(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var fullText = GetFullText(textBox, e.Text);
            e.Handled = !IsTextIp(fullText);
        }
    }

    private void TextBox_PreviewTextInput_Port(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            var fullText = GetFullText(textBox, e.Text);
            e.Handled = !IsTextPort(fullText);
        }
    }

    private void TextBox_PastingHandler_Ip(object sender, DataObjectPastingEventArgs e)
    {
        PastingHandler(sender, e, IsTextIp);
    }

    private void TextBox_PastingHandler_Port(object sender, DataObjectPastingEventArgs e)
    {
        PastingHandler(sender, e, IsTextPort);
    }

    private static void PastingHandler(object sender, DataObjectPastingEventArgs e, Func<string, bool> pastedTextTester)
    {
        if (sender is TextBox textBox && e.DataObject.GetDataPresent(typeof(string)))
        {
            var text = (string)e.DataObject.GetData(typeof(string));
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

    private static string GetFullText(TextBox textBox, string inputText)
    {
        var selectionStart = textBox.Text.IndexOf(textBox.SelectedText);
        var selectionClearedText = textBox.Text.Remove(selectionStart, textBox.SelectedText.Length);
        return selectionClearedText.Insert(textBox.CaretIndex, inputText);
    }

    private bool IsTextIp(string text) => IpRegex.IsMatch(text);

    private bool IsTextPort(string text) => PortRegex.IsMatch(text);
}
