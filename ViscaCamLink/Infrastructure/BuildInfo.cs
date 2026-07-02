namespace ViscaCamLink.Infrastructure;

/// <summary>
/// Exposes compile-time build configuration flags baked in via pre-processor symbols.
/// </summary>
internal static class BuildInfo
{
    /// <summary>
    /// <see langword="true"/> when built under the <c>Release-Portable</c> configuration
    /// (i.e. the <c>RELEASE_PORTABLE</c> symbol is defined).
    /// </summary>
    public static bool IsPortable =>
#if RELEASE_PORTABLE
        true;
#else
        false;
#endif
}
