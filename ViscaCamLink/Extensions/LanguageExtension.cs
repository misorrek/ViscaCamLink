namespace ViscaCamLink.Extensions;

using ViscaCamLink.Infrastructure.Localization;
using ViscaCamLink.Resources;

public static class LanguageExtension
{
    public static string ToLocalizedString(this Language language)
    {
        return language switch
        {
            Language.System => Strings.Language_System,
            Language.English => Strings.Language_English,
            Language.German => Strings.Language_German,
            _ => language.ToString(),
        };
    }
}
