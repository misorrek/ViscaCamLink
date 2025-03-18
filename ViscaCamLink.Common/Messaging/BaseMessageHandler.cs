namespace ViscaCamLink.Common.Messaging;

using System;
using System.Windows;
using System.Windows.Threading;

public class BaseMessageHandler : IMessageHandler
{
    public BaseMessageHandler(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    private readonly Dispatcher _dispatcher;

    public bool ShowInfo(string title, string text)
    {
        var result = ShowMessage(
            text,
            title,
            MessageBoxImage.Information,
            MessageBoxButton.OK);

        return result is MessageBoxResult.OK;
    }

    public bool ShowQuestion(string title, string text)
    {
        var result = ShowMessage(
            text,
            title,
            MessageBoxImage.Question,
            MessageBoxButton.YesNo,
            "Ja",
            "Nein");

        return result is MessageBoxResult.OK;
    }

    public bool ShowWarning(string title, string text)
    {
        var result = ShowMessage(
            text,
            title,
            MessageBoxImage.Warning,
            MessageBoxButton.OK);

        return result is MessageBoxResult.OK;
    }

    public bool ShowError(string title, string text)
    {
        var result = ShowMessage(
            text,
            title,
            MessageBoxImage.Error,
            MessageBoxButton.OK);

        return result is MessageBoxResult.OK;
    }

    public bool ShowRetry(string title, string text)
    {
        var result = ShowMessage(
            text,
            title,
            MessageBoxImage.Warning,
            MessageBoxButton.OKCancel,
            "Wiederholen");

        return result is MessageBoxResult.OK;
    }

    protected MessageBoxResult ShowMessage(
        string text,
        string title,
        MessageBoxImage image,
        MessageBoxButton button,
        string? okButtonText = null,
        string? cancelButtonText = null)
    {
        return _dispatcher.Invoke(new Func<MessageBoxResult>(() =>
        {
            var viewModel = new MessageViewModel()
            {
                Text = text,
                Title = title,
                Image = image,
                Button = button,
                OkButtonText = okButtonText ?? "Ok",
                CancelButtonText = cancelButtonText ?? "Abbrechen",
            };
            var view = new MessageView();

            viewModel.RequestCloseDialog = view.Close;
            view.DataContext = viewModel;

            view.Topmost = true;
            view.ShowDialog();

            return viewModel.Result;
        }));
    }
}
