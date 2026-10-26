namespace ViscaCamLink.Services;

using Microsoft.Extensions.Logging;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Infrastructure.Theming;

public record OptionsSelection(
    Language Language,
    Theme Theme,
    bool UseCompactView,
    bool UsePresetGroups,
    bool UseNumpadLayout,
    bool UseGlobalHotKeys,
    LogLevel LogLevel);
