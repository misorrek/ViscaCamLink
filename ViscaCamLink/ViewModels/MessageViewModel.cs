namespace ViscaCamLink.ViewModels;

using System;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Services;

public class MessageViewModel(
    string title,
    string text,
    MessageKind kind,
    string primaryButtonText,
    string? secondaryButtonText,
    bool defaultToSecondary,
    Action closeAction) : ViewModelBase
{
    public string Title { get; } = title;

    public string Text { get; } = text;

    public MessageKind Kind { get; } = kind;

    public string PrimaryButtonText { get; } = primaryButtonText;

    public string? SecondaryButtonText { get; } = secondaryButtonText;

    public bool HasSecondaryButton => !string.IsNullOrEmpty(SecondaryButtonText);

    public bool IsPrimaryDefault => !defaultToSecondary || !HasSecondaryButton;

    public bool IsSecondaryDefault => !IsPrimaryDefault;

    // True only when the user explicitly confirmed; closing the window by any other means counts as declined.
    public bool Result { get; private set; }

    public ICommand PrimaryCommand => new Command(() => Close(true));

    public ICommand SecondaryCommand => new Command(() => Close(false));

    private void Close(bool result)
    {
        Result = result;
        closeAction();
    }
}
