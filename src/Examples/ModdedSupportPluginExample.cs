using BepInEx.Unity.IL2CPP;

namespace Surfer.Examples;

/// <summary>
/// Example plugin demonstrating how to integrate with Surfer modded support system.
/// Shows the structure required for a plugin to declare Surfer events and flags.
/// </summary>
/// <remarks>
/// This example shows a minimal plugin that implements both event handlers and flags.
/// In a real implementation, you would add your plugin's actual logic in the Load() method.
/// </remarks>
internal class ModdedSupportPluginExample : BasePlugin
{
    /// <summary>
    /// Event handler instance for Surfer events.
    /// This field must be named exactly "SurferEvents" to be detected by the reflection system.
    /// </summary>
    public static ModdedSupportSurferEventExample SurferEvents = new();

    /// <summary>
    /// Array of flags to control Surfer behavior.
    /// This field must be named exactly "SurferFlags" to be detected by the reflection system.
    /// </summary>
    /// <remarks>
    /// Flags can disable specific Surfer features or modify its behavior.
    /// See <see cref="Modules.Support.SurferModdedSupportFlags"/> for available flag constants.
    /// </remarks>
    public static string[] SurferFlags = [];

    /// <summary>
    /// Main plugin load method.
    /// In a real implementation, this is where your plugin initialization would occur.
    /// </summary>
    public override void Load()
    {
        // Plugin initialization logic would go here
    }
}