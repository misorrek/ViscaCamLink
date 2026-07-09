namespace ViscaCamLink.Extensions;

using System;
using System.ComponentModel;

public static class EnumExtension
{
    public static string GetDescription<T>(this T enumerationValue)
    {
        if (enumerationValue == null)
        {
            throw new ArgumentNullException(nameof(enumerationValue));
        }

        var type = enumerationValue.GetType();

        if (!type.IsEnum)
        {
            throw new ArgumentException("Must be an enum type", nameof(enumerationValue));
        }

        var name = Enum.GetName(type, enumerationValue);

        if (name != null)
        {
            var field = type.GetField(name);

            if (field != null)
            {
                if (Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) is DescriptionAttribute attribute)
                {
                    return attribute.Description;
                }
            }
        }

        throw new ArgumentException("Has no description attribute", nameof(enumerationValue));
    }
}