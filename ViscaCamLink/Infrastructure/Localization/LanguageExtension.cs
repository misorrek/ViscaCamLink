namespace ViscaCamLink.Infrastructure.Localization;

using System;
using System.ComponentModel;

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

    public static string GetLanguageCode(this Language language)
    {
        var field = typeof(Language).GetField(language.ToString());

        if (field is not null &&
            Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        throw new ArgumentException("Has no description attribute", nameof(language));
    }
}
