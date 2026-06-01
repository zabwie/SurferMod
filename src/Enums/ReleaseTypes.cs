namespace Surfer.Enums;

/// <summary>
/// Defines the release types or build channels for Surfer.
/// </summary>
internal enum ReleaseTypes : int
{
    /// <summary>
    /// Stable release version.
    /// </summary>
    Release,

    /// <summary>
    /// Beta testing version.
    /// </summary>
    Beta,

    /// <summary>
    /// Development/experimental version.
    /// </summary>
    Dev,
}