namespace ViscaCamLink.Util;

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
    public static String ToLocalizedString(this Language language)
    {
        switch (language) 
        {
            case Language.System:
                return Strings.Language_System;
            case Language.English:
                return Strings.Language_English;
            case Language.German:
                return Strings.Language_German;
            default:
                return language.ToString();
        }
    }
}
