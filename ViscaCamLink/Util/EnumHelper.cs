namespace ViscaCamLink.Util;

using System;
using System.ComponentModel;

public static class EnumHelper
{
    public static String GetDescription<T>(this T enumerationValue)
    {
        if (enumerationValue == null)
        {
            throw new ArgumentNullException(nameof(enumerationValue));
        }

        Type type = enumerationValue.GetType();

        if (!type.IsEnum)
        {
            throw new ArgumentException("EnumerationValue must be of Enum type", nameof(enumerationValue));
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

        throw new ArgumentException("EnumerationValue has no description", nameof(enumerationValue));
    }
}