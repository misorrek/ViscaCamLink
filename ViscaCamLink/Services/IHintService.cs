namespace ViscaCamLink.Services;

using System;

public interface IHintService
{
    event Action? HintsChanged;

    bool IsDismissed(HintId hintId);

    void Dismiss(HintId hintId);

    void ResetAll();
}
