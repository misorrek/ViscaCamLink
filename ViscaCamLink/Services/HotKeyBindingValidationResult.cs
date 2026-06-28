namespace ViscaCamLink.Services;

public sealed class HotKeyBindingValidationResult
{
    private HotKeyBindingValidationResult(bool isValid, string? message)
    {
        IsValid = isValid;
        Message = message;
    }

    public bool IsValid { get; }

    public string? Message { get; }

    public static HotKeyBindingValidationResult Valid() => new(true, null);

    public static HotKeyBindingValidationResult Invalid(string message) => new(false, message);
}