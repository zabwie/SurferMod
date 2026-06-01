using AmongUs.Data;
using Surfer.Modules;
using Surfer.Modules.Support;
using Discord;
using HarmonyLib;

namespace Surfer.Patches.Client.Discord;

[HarmonyPatch]
internal static class DiscordPatch
{
    private static string lobbycode = "";
    private static string region = "";

    [HarmonyPatch(typeof(ActivityManager), nameof(ActivityManager.UpdateActivity))]
    [HarmonyPrefix]
    private static void ActivityManager_UpdateActivity_Prefix(Activity activity)
    {
        // Skip Discord Rich Presence if other mods have disabled it via SurferModdedSupportFlags
        if (SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Disable_DiscordRP)) return;
        if (activity == null) return;

        string details = $"Surfer {SurferPlugin.GetVersionText()}";
        activity.Details = details;

        // Skip lobby info processing if Discord already shows "In Menus"
        if (activity.State == "In Menus") return;

        try
        {
            // Only show lobby code/region if streamer mode is off
            if (!DataManager.Settings.Gameplay.StreamerMode)
            {
                UpdateRegionAndLobbyCode();
                // Build lobby info string only when both code and region exist
                if (!string.IsNullOrEmpty(lobbycode) && !string.IsNullOrEmpty(region))
                {
                    // Show lobby code with region in parentheses
                    if (GameState.IsNormalGame)
                        details = $"Surfer - {lobbycode} ({region})";
                    else if (GameState.IsHideNSeek)
                        details = $"Surfer Hide & Seek - {lobbycode} ({region})";
                }
            }
            else
            {
                // Streamer mode hides lobby code, only show mode info
                if (GameState.IsHideNSeek)
                    details = $"Surfer v{ModInfo.PLUGIN_VERSION} - Hide & Seek";
            }
        }
        catch
        {
            // Silent fail to prevent Discord crashes from affecting gameplay
        }

        activity.Details = details;
    }

    private static void UpdateRegionAndLobbyCode()
    {
        // Only fetch lobby data when player is in a lobby
        if (GameState.IsLobby)
        {
            if (GameStartManager.Instance?.GameRoomNameCode != null)
            {
                lobbycode = GameStartManager.Instance.GameRoomNameCode.text;
                region = ServerManager.Instance.CurrentRegion.Name;
                // Convert full region names to short codes
                region = region switch
                {
                    "North America" => "NA",
                    "Europe" => "EU",
                    "Asia" => "AS",
                    _ when region.Contains("MNA") => "MNA",
                    _ when region.Contains("MEU") => "MEU",
                    _ when region.Contains("MAS") => "MAS",
                    _ => region
                };
            }
        }
    }
}