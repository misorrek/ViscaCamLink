namespace ViscaCamLink.Extensions;

using System;
using System.ComponentModel;

public static class EnumExtension
{
    public static string GetDescription<TEnum>(this TEnum enumerationValue) where TEnum : struct, Enum
    {
        var name = enumerationValue.ToString();
        var field = typeof(TEnum).GetField(name);

        if (field is not null &&
            Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
        {
            return attribute.Description;
        }

        throw new ArgumentException("Has no description attribute", nameof(enumerationValue));
    }
}
