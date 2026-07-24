namespace ViscaCamLink.Services;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;

public record OptionsSelection(
    Language Language,
    Theme Theme,
    bool UseCompactView,
    bool UsePresetGroups,
    bool UseNumpadLayout,
    bool UseGlobalHotKeys);
