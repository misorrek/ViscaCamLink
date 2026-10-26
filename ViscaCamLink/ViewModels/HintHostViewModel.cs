namespace ViscaCamLink.ViewModels;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using ViscaCamLink.Infrastructure.Interface;
using ViscaCamLink.Resources;
using ViscaCamLink.Services;

// Owns all hints and exposes the one to show. To add a hint: add a HintId member and an entry to Catalog (order = priority).
public class HintHostViewModel : ViewModelBase
{
    private static readonly TimeSpan DefaultStartupDelay = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan DefaultBetweenDelay = TimeSpan.FromSeconds(5);

    private static readonly (HintId Id, string MessageKey)[] Catalog =
    [
        (HintId.PresetRename, nameof(Strings.Hint_PresetRename)),
    ];

    private readonly IUiDispatcher _uiDispatcher;
    private readonly IReadOnlyList<HintViewModel> _hints;
    private readonly TimeSpan _betweenDelay;

    private HintViewModel? _currentHint;
    private bool _isShowPending;

    public HintHostViewModel(
        IHintService hintService,
        IUiDispatcher uiDispatcher,
        TimeSpan? startupDelay = null,
        TimeSpan? betweenDelay = null)
    {
        _uiDispatcher = uiDispatcher;
        _betweenDelay = betweenDelay ?? DefaultBetweenDelay;
        _hints = Catalog.Select(entry => new HintViewModel(hintService, entry.Id, entry.MessageKey)).ToList();

        hintService.HintsChanged += OnHintsChanged;

        ScheduleShow(startupDelay ?? DefaultStartupDelay);
    }

    public HintViewModel? CurrentHint => _currentHint;

    public bool HasHint => _currentHint is not null;

    private void OnHintsChanged()
    {
        if (_isShowPending)
        {
            return;
        }

        if (_currentHint is null)
        {
            ShowNext();
        }
        else if (!_currentHint.IsVisible)
        {
            SetCurrent(null);

            if (_hints.Any(hint => hint.IsVisible))
            {
                ScheduleShow(_betweenDelay);
            }
        }
    }

    private void ScheduleShow(TimeSpan delay)
    {
        if (delay <= TimeSpan.Zero)
        {
            ShowNext();
            return;
        }

        _isShowPending = true;

        _ = Task.Delay(delay).ContinueWith(
            _ => _uiDispatcher.Post(() =>
            {
                _isShowPending = false;
                ShowNext();
            }),
            TaskScheduler.Default);
    }

    private void ShowNext() => SetCurrent(_hints.FirstOrDefault(hint => hint.IsVisible));

    private void SetCurrent(HintViewModel? hint)
    {
        _currentHint = hint;

        NotifyPropertyChanged(nameof(CurrentHint));
        NotifyPropertyChanged(nameof(HasHint));
    }
}
