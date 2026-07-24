namespace ViscaCamLink.ViewModels;

using ViscaCamLink.Extensions;
using ViscaCamLink.Infrastructure.Theming;

public class ThemeItem(Theme theme)
{
    public Theme ThemeValue { get; } = theme;

    public string ThemeDisplay => ThemeValue.ToLocalizedString();
}
