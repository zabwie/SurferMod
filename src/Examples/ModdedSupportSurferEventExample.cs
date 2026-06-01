using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;

namespace Surfer.Examples;

/// <summary>
/// Example event handler class demonstrating how to respond to Surfer events.
/// This class must be instantiated and assigned to the SurferEvents field in the plugin.
/// </summary>
/// <remarks>
/// Each method in this class corresponds to a different lifecycle event in Surfer.
/// Methods are optional - only implement the ones your plugin needs.
/// </remarks>
internal class ModdedSupportSurferEventExample
{
    /// <summary>
    /// Called when Surfer is loading. Can be used to prevent Surfer from loading.
    /// </summary>
    /// <param name="bauPlugin">The Surfer plugin instance.</param>
    /// <returns>
    /// Return true to allow Surfer to continue loading, or false to prevent Surfer from loading.
    /// This can be useful if your plugin has compatibility issues with specific Surfer versions.
    /// </returns>
    public bool OnSurferLoad(BasePlugin bauPlugin)
    {
        return true;
    }

    /// <summary>
    /// Called when Surfer game options have been loaded.
    /// Allows plugins to read or modify Surfer game options.
    /// </summary>
    /// <param name="options">
    /// Array of game options objects from Surfer.
    /// The exact type of these objects depends on Surfer's internal implementation.
    /// Common option types to check for:
    /// <see cref="Modules.OptionItems.OptionItem"/>,
    /// <see cref="Modules.OptionItems.OptionItem{T}"/>, 
    /// <see cref="Modules.OptionItems.OptionCheckboxItem"/>,
    /// <see cref="Modules.OptionItems.OptionFloatItem"/>,
    /// <see cref="Modules.OptionItems.OptionIntItem"/>,
    /// <see cref="Modules.OptionItems.OptionPercentItem"/>,
    /// <see cref="Modules.OptionItems.OptionPlayerItem"/>,
    /// <see cref="Modules.OptionItems.OptionStringItem"/>,
    /// </param>
    public void OnSurferOptionsLoaded(object[] options)
    {
    }

    /// <summary>
    /// Called when Surfer configuration entries have been loaded.
    /// Allows plugins to read or modify Surfer's BepInEx configuration entries.
    /// </summary>
    /// <param name="configs">
    /// Array of BepInEx configuration entries from Surfer.
    /// These can be cast to ConfigEntry&lt;T&gt; to access their values.
    /// </param>
    /// <remarks>
    /// This is useful for plugins that need to interact with Surfer's configuration,
    /// such as reading default values or adding validation to certain settings.
    /// </remarks>
    public void OnSurferConfigEntriesLoaded(ConfigEntryBase[] configs)
    {
    }
}