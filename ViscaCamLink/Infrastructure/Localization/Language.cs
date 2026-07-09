namespace ViscaCamLink.Infrastructure.Localization;

using System;
using System.ComponentModel;

using ViscaCamLink.Resources;

[Serializable]
public enum Language
{
    System = 0,
   [Description("en-US")]
    English = 1,
   [Description("de-DE")]
    German = 2,
}

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
