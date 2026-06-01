using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using System.Reflection;

namespace Surfer.Modules.Support;

/// <summary>
/// Provides a system for modded plugins to interact with Surfer through reflection-based events.
/// Allows other plugins to define event handlers without direct assembly references.
/// </summary>
/// <remarks>
/// Plugins can implement event handlers by creating a class with specific method signatures.
/// The system automatically discovers and invokes these handlers at runtime.
/// </remarks>
public class SurferModdedSupportEvents
{
    // ============================================
    // Method Structures
    // ============================================

    /// <summary>
    /// Default implementation of the OnSurferLoad event handler.
    /// </summary>
    /// <param name="bauPlugin">The Surfer plugin instance.</param>
    /// <returns>Always returns true to indicate successful loading.</returns>
    public bool OnSurferLoad(BasePlugin bauPlugin) => true;

    /// <summary>
    /// Default implementation of the OnSurferOptionsLoaded event handler.
    /// </summary>
    /// <param name="options">The loaded game options from Surfer.</param>
    public void OnSurferOptionsLoaded(object[] options) { }

    /// <summary>
    /// Default implementation of the OnSurferConfigEntriesLoaded event handler.
    /// </summary>
    /// <param name="configs">An array of BepInEx configuration entries from Surfer.</param>
    public void OnSurferConfigEntriesLoaded(ConfigEntryBase[] configs) { }

    // ============================================
    // Method Structures
    // ============================================

    /// <summary>
    /// Invokes the OnSurferLoad event handler for all loaded plugins.
    /// </summary>
    /// <param name="bauPlugin">The Surfer plugin instance.</param>
    /// <returns>
    /// Returns false if any plugin's OnSurferLoad handler returns false, otherwise returns true.
    /// Returns true if no plugins implement the handler.
    /// </returns>
    internal static bool InvokeAll_OnSurferLoad(BasePlugin bauPlugin)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            if (InvokePluginMethod<bool?>(plugin, nameof(OnSurferLoad), defaultValue: true, bauPlugin) == false)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Invokes the OnSurferOptionsLoaded event handler for all loaded plugins.
    /// </summary>
    /// <param name="options">The loaded game options from Surfer.</param>
    internal static void InvokeAll_OnSurferOptionsLoaded(object[] options)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            InvokePluginMethod<object>(plugin, nameof(OnSurferOptionsLoaded), parameters: options);
        }
    }

    /// <summary>
    /// Invokes the OnSurferConfigEntriesLoaded event handler for all loaded plugins.
    /// </summary>
    /// <param name="configs">An array of BepInEx configuration entries from Surfer.</param>
    internal static void InvokeAll_OnSurferConfigEntriesLoaded(ConfigEntryBase[] configs)
    {
        foreach (var pluginInfo in IL2CPPChainloader.Instance.Plugins.Values)
        {
            var plugin = (BasePlugin)pluginInfo.Instance;
            InvokePluginMethod<object>(plugin, nameof(OnSurferConfigEntriesLoaded), parameters: configs);
        }
    }

    /// <summary>
    /// Invokes a specific method on a plugin's event class using reflection.
    /// </summary>
    /// <typeparam name="T">The expected return type of the method.</typeparam>
    /// <param name="plugin">The plugin instance to invoke the method on.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="defaultValue">The value to return if the method cannot be invoked.</param>
    /// <param name="parameters">The parameters to pass to the method.</param>
    /// <returns>
    /// The result of the method invocation, or <paramref name="defaultValue"/> if the method
    /// cannot be found, invocation fails, or the return type doesn't match <typeparamref name="T"/>.
    /// </returns>
    private static T? InvokePluginMethod<T>(BasePlugin plugin, string methodName, T? defaultValue = default, params object[] parameters)
    {
        var eventClass = GetEventClass(plugin);
        if (eventClass == null) return defaultValue;

        var method = eventClass.GetType().GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        if (method == null) return defaultValue;

        try
        {
            var result = method.Invoke(eventClass, parameters);

            return result is T typedResult ? typedResult : defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    /// <summary>
    /// Retrieves the event class instance from a plugin using reflection.
    /// </summary>
    /// <param name="plugin">The plugin instance to search for the event class.</param>
    /// <returns>
    /// The event class instance if found, otherwise null.
    /// </returns>
    private static object? GetEventClass(BasePlugin plugin)
    {
        const string EventFieldName = "SurferEvents";

        var field = plugin.GetType().GetField(EventFieldName,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

        if (field == null) return null;

        var value = field.IsStatic
            ? field.GetValue(null)
            : field.GetValue(plugin);

        return value?.GetType().IsClass == true ? value : null;
    }
}