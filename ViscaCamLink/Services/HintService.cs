namespace ViscaCamLink.Services;

using System;
using System.Linq;

public class HintService(ISettingsService settingsService) : IHintService
{
    public event Action? HintsChanged;

    public bool IsDismissed(HintId hintId) => settingsService.DismissedHints.Contains(hintId.ToString());

    public void Dismiss(HintId hintId)
    {
        if (IsDismissed(hintId))
        {
            return;
        }

        settingsService.DismissHint(hintId.ToString());

        HintsChanged?.Invoke();
    }

    public void ResetAll()
    {
        settingsService.ResetDismissedHints();

        HintsChanged?.Invoke();
    }
}
