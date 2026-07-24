namespace ViscaCamLink.Infrastructure.Localization;

using System.ComponentModel;

public enum Language
{
    System = 0,

    [Description("en")]
    English = 1,

    [Description("de")]
    German = 2,
}
