namespace ViscaCamLink.ViewModels;

using System;
using System.Windows.Input;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Services;

public class HintViewModel : ViewModelBase
{
    private readonly IHintService _hintService;
    private readonly HintId _hintId;
    private readonly string _messageKey;

    public HintViewModel(IHintService hintService, HintId hintId, string messageKey)
    {
        _hintService = hintService;
        _hintId = hintId;
        _messageKey = messageKey;

        _hintService.HintsChanged += OnHintsChanged;
        TranslationSource.Instance.LanguageChanged += OnLanguageChanged;

        DismissCommand = new Command(() => _hintService.Dismiss(_hintId));
    }

    public ICommand DismissCommand { get; }

    public string Message => TranslationSource.Instance[_messageKey];

    public bool IsVisible => !_hintService.IsDismissed(_hintId);

    private void OnHintsChanged() => NotifyPropertyChanged(nameof(IsVisible));

    private void OnLanguageChanged(object? sender, EventArgs eventArgs) => NotifyPropertyChanged(nameof(Message));
}
