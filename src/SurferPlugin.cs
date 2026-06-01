using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using BepInEx.Unity.IL2CPP.Utils;
using Surfer.Attributes;
using Surfer.Data;
using Surfer.Data.Json;
using Surfer.Enums;
using Surfer.Helpers;
using Surfer.Modules;
using Surfer.Modules.OptionItems;
using Surfer.Modules.Support;
using Surfer.Network;
using Surfer.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace Surfer;

[BepInPlugin(ModInfo.PLUGIN_GUID, ModInfo.PLUGIN_NAME, ModInfo.PLUGIN_VERSION)]
[BepInProcess(ModInfo.AmongUs.PROCESS_NAME)]
internal class SurferPlugin : BasePlugin
{
    /// <summary>
    /// Gets the formatted version text for display.
    /// </summary>
    /// <param name="newLine">Whether to use newline separation for additional info.</param>
    /// <returns>Formatted version string.</returns>
    internal static string GetVersionText(bool newLine = false)
    {
        string text = string.Empty;

        string newLineText = newLine ? "\n" : " ";

        switch (ModInfo.ReleaseBuildType)
        {
            case ReleaseTypes.Release:
                text = $"v{SurferVersion}";
                break;
            case ReleaseTypes.Beta:
                text = $"v{SurferVersion}{newLineText}Beta {ModInfo.BETA_NUM}";
                break;
            case ReleaseTypes.Dev:
                text = $"v{SurferVersion}{newLineText}Dev {ModInfo.CommitHash}-{ModInfo.BuildDate}";
                break;
            default:
                break;
        }

        if (ModInfo.IS_HOTFIX)
            text += $"{newLineText}Hotfix {ModInfo.HOTFIX_NUM}";

        return text;
    }

    /// <summary>
    /// Gets the Harmony instance used for patching.
    /// </summary>
    internal static Harmony Harmony { get; } = new Harmony(ModInfo.PLUGIN_GUID);

    /// <summary>
    /// Gets the Surfer version string.
    /// </summary>
    internal static string SurferVersion => ModInfo.PLUGIN_VERSION;

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    internal static string AppVersion => Application.version;

    /// <summary>
    /// Gets the Among Us version string from reference data.
    /// </summary>
    internal static string AmongUsVersion => ReferenceDataManager.Instance.Refdata.userFacingVersion;

    /// <summary>
    /// Gets platform-specific data.
    /// </summary>
    internal static PlatformSpecificData PlatformData => Constants.GetPlatformData();

    /// <summary>
    /// Gets the list of supported Among Us versions.
    /// </summary>
    internal static string[] SupportedAmongUsVersions =
    [
        "2025.11.18",
    ];

    /// <summary>
    /// Gets the list of all PlayerControl instances.
    /// </summary>
    internal static List<PlayerControl> AllPlayerControls = [];

    /// <summary>
    /// Gets the list of all alive PlayerControl instances.
    /// </summary>
    internal static List<PlayerControl> AllAlivePlayerControls => AllPlayerControls.Where(pc => pc.IsAlive()).ToList();

    /// <summary>
    /// Gets all DeadBody objects in the scene.
    /// </summary>
    internal static DeadBody[] AllDeadBodys => UnityEngine.Object.FindObjectsOfType<DeadBody>().ToArray();

    /// <summary>
    /// Gets all Vent objects in the scene.
    /// </summary>
    internal static Vent[] AllVents => UnityEngine.Object.FindObjectsOfType<Vent>();

    /// <summary>
    /// Gets the BepInEx logger instance.
    /// </summary>
    internal static ManualLogSource? Logger;

    public override void Load()
    {
        try
        {
            foreach (var listener in BepInEx.Logging.Logger.Listeners)
            {
                if (listener.GetType().Name.ToLower().Contains("Unity"))
                {
                    BepInEx.Logging.Logger.Listeners.Remove(listener);
                    break;
                }
            }

            Logger = BepInEx.Logging.Logger.CreateLogSource(ModInfo.PLUGIN_GUID);
            RegisterAllMonoBehavioursInAssembly();
            IL2CPPChainloader.Instance.Finished += OnChainloaderFinished;
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
        }
    }

    private void OnChainloaderFinished()
    {
        try
        {
            SurferModdedSupportFlags.Initialize();
            LoadOptions();
            BetterDataManager.Initialize();
            Translator.Initialize();
            Harmony.PatchAll();
            GameSettingsPatch.SetupSettings(true);
            InstanceAttribute.RegisterAll();
            OutfitData.Initialize();
            Logger_.Log("Surfer loaded successfully!");

            try
            {
                var menuObj = new GameObject("SurferMenu");
                UnityEngine.Object.DontDestroyOnLoad(menuObj);
                menuObj.hideFlags = HideFlags.HideAndDontSave;
                var menu = menuObj.AddComponent<SurferMenu>();
                menu.enabled = true;
                Logger_.Log("SurferMenu GUI created, enabled=" + menu.enabled);
            }
            catch (Exception guiEx)
            {
                Logger_.Error(guiEx, "SurferMenu");
            }
        }
        catch (Exception ex)
        {
            Logger_.Error(ex);
        }
    }

    /// <summary>
    /// Sets up the console window for logging.
    /// </summary>
    private static void SetupConsole()
    {
        ConsoleManager.CreateConsole();
        ConsoleManager.ConfigPreventClose.Value = true;
        if (ConsoleManager.ConfigConsoleEnabled.Value) ConsoleManager.DetachConsole();
        ConsoleManager.ConfigConsoleEnabled.Value = false;
        ConsoleManager.SetConsoleTitle("Among Us - Surfer Console");
        Logger = BepInEx.Logging.Logger.CreateLogSource(ModInfo.PLUGIN_GUID);
        var customLogListener = new CustomLogListener();
        BepInEx.Logging.Logger.Listeners.Add(customLogListener);
        ConsoleManager.SetConsoleColor(ConsoleColor.Green);
        ConsoleManager.ConsoleStream.WriteLine($".--------------------------------------------------------------------------------.\r\n|  ____       _   _                 _                                  _   _     |\r\n| | __ )  ___| |_| |_ ___ _ __     / \\   _ __ ___   ___  _ __   __ _  | | | |___ |\r\n| |  _ \\ / _ \\ __| __/ _ \\ '__|   / _ \\ | '_ ` _ \\ / _ \\| '_ \\ / _` | | | | / __||\r\n| | |_) |  __/ |_| ||  __/ |     / ___ \\| | | | | | (_) | | | | (_| | | |_| \\__ \\|\r\n| |____/ \\___|\\__|\\__\\___|_|    /_/   \\_\\_| |_| |_|\\___/|_| |_|\\__, |  \\___/|___/|\r\n|                                                              |___/             |\r\n'--------------------------------------------------------------------------------'");
    }

    /// <summary>
    /// Registers all MonoBehaviours for IL2CPP injection.
    /// SurferBehaviour base class suppresses GC finalization in OnDestroy
    /// to prevent ClassInjector.Finalize crashes on destroyed handles.
    /// </summary>
    private static void RegisterAllMonoBehavioursInAssembly()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var monoBehaviourTypes = assembly.GetTypes()
            .Where(type => type.IsSubclassOf(typeof(MonoBehaviour)) && !type.IsAbstract)
            .OrderBy(type => type.Name);

        foreach (var type in monoBehaviourTypes)
        {
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp(type);
            }
            catch (Exception ex)
            {
                Logger_.Error($"Failed to register MonoBehaviour: {type.FullName}\n{ex}");
            }
        }
    }

    /// <summary>
    /// Gets or sets the configuration entry for private only lobby setting.
    /// </summary>
    internal static ConfigEntry<bool>? PrivateOnlyLobby { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for anti-cheat setting.
    /// </summary>
    internal static ConfigEntry<bool>? AntiCheat { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for sending Better RPC setting.
    /// </summary>
    internal static ConfigEntry<bool>? SendBetterRpc { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for better notifications setting.
    /// </summary>
    internal static ConfigEntry<bool>? BetterNotifications { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for force own language setting.
    /// </summary>
    internal static ConfigEntry<bool>? ForceOwnLanguage { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for chat dark mode setting.
    /// </summary>
    internal static ConfigEntry<bool>? ChatDarkMode { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for chat in gameplay setting.
    /// </summary>
    internal static ConfigEntry<bool>? ChatInGameplay { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for lobby player info setting.
    /// </summary>
    internal static ConfigEntry<bool>? LobbyPlayerInfo { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for disable lobby theme setting.
    /// </summary>
    internal static ConfigEntry<bool>? DisableLobbyTheme { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for unlock FPS setting.
    /// </summary>
    internal static ConfigEntry<bool>? UnlockFPS { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for show FPS setting.
    /// </summary>
    internal static ConfigEntry<bool>? ShowFPS { get; private set; }

    /// <summary>
    /// Gets or sets the configuration entry for command prefix setting.
    /// </summary>
    internal static ConfigEntry<string>? CommandPrefix { get; set; }

    /// <summary>
    /// Gets or sets the configuration entry for favorite color setting.
    /// </summary>
    internal static ConfigEntry<int>? FavoriteColor { get; set; }

    /// <summary>
    /// Gets or sets the configuration entry for the settings preset.
    /// </summary>
    internal static ConfigEntry<int>? SettingsPreset { get; private set; }

    internal static ConfigEntry<bool>? AutoKick { get; private set; }
    internal static ConfigEntry<int>? AutoKickThreshold { get; private set; }
    internal static ConfigEntry<bool>? AntiBot { get; private set; }
    internal static ConfigEntry<bool>? LongerMessages { get; private set; }
    internal static ConfigEntry<bool>? UnlockClipboard { get; private set; }
    internal static ConfigEntry<bool>? BypassUrlBlock { get; private set; }
    internal static ConfigEntry<bool>? CopyLobbyCode { get; private set; }
    internal static ConfigEntry<bool>? LowerRateLimits { get; private set; }

    /// <summary>
    /// Loads configuration options from BepInEx config file.
    /// </summary>
    private void LoadOptions()
    {
        PrivateOnlyLobby = Config.Bind("Mod", "PrivateOnlyLobby", false);
        AntiCheat = Config.Bind("Better Options", "AntiCheat", true);
        SendBetterRpc = Config.Bind("Better Options", "SendBetterRpc", true);
        BetterNotifications = Config.Bind("Better Options", "BetterNotifications", true);
        ForceOwnLanguage = Config.Bind("Better Options", "ForceOwnLanguage", false);
        ChatDarkMode = Config.Bind("Better Options", "ChatDarkMode", true);
        ChatInGameplay = Config.Bind("Better Options", "ChatInGameplay", true);
        LobbyPlayerInfo = Config.Bind("Better Options", "LobbyPlayerInfo", true);
        DisableLobbyTheme = Config.Bind("Better Options", "DisableLobbyTheme", true);
        UnlockFPS = Config.Bind("Better Options", "UnlockFPS", false);
        ShowFPS = Config.Bind("Better Options", "ShowFPS", false);
        CommandPrefix = Config.Bind("Client Options", "CommandPrefix", "/");
        FavoriteColor = Config.Bind("Mod", "FavoriteColor", -1);
        SettingsPreset = Config.Bind("Mod", "SettingsPreset", 0);
        AutoKick = Config.Bind("Surfer Options", "AutoKick", false);
        AutoKickThreshold = Config.Bind("Surfer Options", "AutoKickThreshold", 0);
        AntiBot = Config.Bind("Surfer Options", "AntiBot", false);
        LongerMessages = Config.Bind("Surfer Options", "LongerMessages", false);
        UnlockClipboard = Config.Bind("Surfer Options", "UnlockClipboard", false);
        BypassUrlBlock = Config.Bind("Surfer Options", "BypassUrlBlock", false);
        CopyLobbyCode = Config.Bind("Surfer Options", "CopyLobbyCode", false);
        LowerRateLimits = Config.Bind("Surfer Options", "LowerRateLimits", false);

        SurferModdedSupportEvents.InvokeAll_OnSurferConfigEntriesLoaded([
            PrivateOnlyLobby, AntiCheat, SendBetterRpc,
            BetterNotifications, ForceOwnLanguage, ChatDarkMode,
            ChatInGameplay, LobbyPlayerInfo, DisableLobbyTheme,
            UnlockFPS, ShowFPS, CommandPrefix,
            FavoriteColor, SettingsPreset,
            AutoKick, AutoKickThreshold, AntiBot, LongerMessages,
            UnlockClipboard, BypassUrlBlock, CopyLobbyCode, LowerRateLimits
        ]);

        QualitySettings.vSyncCount = UnlockFPS?.Value == true ? 0 : 1;
        Application.targetFrameRate = UnlockFPS?.Value == true ? 999 : 60;
    }

    /// <summary>
    /// Gets the persistent data path for Among Us.
    /// </summary>
    /// <returns>The persistent data path string.</returns>
    internal static string GetDataPathToAmongUs() => Application.persistentDataPath;

    /// <summary>
    /// Gets the game installation path for Among Us.
    /// </summary>
    /// <returns>The game installation path string.</returns>
    internal static string GetGamePathToAmongUs() => Path.GetDirectoryName(Application.dataPath) ?? Application.dataPath;
}