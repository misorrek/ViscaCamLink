namespace ViscaCamLink.Common.Messaging;

using System;

public interface IMessageHandler
{
    bool ShowInfo(string title, string text);

    bool ShowQuestion(string title, string text);

    bool ShowWarning(string title, string text);

    bool ShowError(string title, string text);

    bool ShowRetry(string title, string text);
}
