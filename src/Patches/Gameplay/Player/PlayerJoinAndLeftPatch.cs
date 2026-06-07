using Surfer.Data;
using Surfer.Helpers;
using Surfer.Modules;
using Surfer.Mono;
using Surfer.Patches.Gameplay.UI;
using Surfer.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using InnerNet;

namespace Surfer.Patches.Gameplay.Player;

[HarmonyPatch]
internal static class PlayerJoinAndLeftPatch
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnGameJoined_Postfix()
    {
        // Fix host icon color display on modded servers
        if (!GameState.IsVanillaServer)
        {
            var host = AmongUsClient.Instance.GetHost().Character;
            host?.SetColor(-2);
            host?.SetColor(host.CurrentOutfit.ColorId);
        }

        Logger_.Log($"Successfully joined {GameCode.IntToGameName(AmongUsClient.Instance.GameId)}", "OnGameJoinedPatch");
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerJoined))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerJoined_Postfix(ClientData data)
    {
        // Schedule ban list checks 2.5 seconds after player joins
        LateTask.Schedule(() =>
        {
            if (GameState.IsHost)
            {
                if (GameState.IsInGame)
                {
                    var player = Utils.PlayerFromClientId(data.Id);

                    if (player.IsLocalPlayer()) return;

                    // Skip all ban/anti-cheat checks for whitelisted players
                    if (BetterDataManager.IsWhitelisted(player?.Data?.FriendCode)) return;

                    // Check if player is in ban list by friend code or PUID
                    if (BetterGameSettings.UseBanPlayerList.GetBool())
                    {
                        if (player != null)
                        {
                            if (TextFileHandler.CompareStringMatch(BetterDataManager.banPlayerListFile,
                                SurferPlugin.AllPlayerControls.Select(player => player.Data.FriendCode)
                                .Concat(SurferPlugin.AllPlayerControls.Select(player => player.GetHashPuid())).ToArray()))
                            {
                                player.Kick(true, Translator.GetString("AntiCheat.BanPlayerListMessage"), bypassDataCheck: true);
                            }
                        }
                    }

                    // Check if player name matches banned name patterns
                    if (BetterGameSettings.UseBanNameList.GetBool())
                    {
                        if (player != null)
                        {
                            if (TextFileHandler.CompareStringFilters(BetterDataManager.banNameListFile, [player.Data.PlayerName]))
                            {
                                player?.Kick(true, Translator.GetString("AntiCheat.BanPlayerListMessage"), bypassDataCheck: true);
                            }
                        }
                    }
                }
            }
        }, 2.5f, "OnPlayerJoinedPatch", false);
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerLeft_Postfix(ClientData data, DisconnectReasons reason)
    {
        Logger_.Log($"[CRASH-TRACE] OnPlayerLeft START — clientId={data?.Character?.PlayerId}");
        // Reclaim favorite color when player leaves in lobby
        if (GameState.IsLobby && PlayerControl.LocalPlayer != null)
        {
            var favColorId = (byte)SurferPlugin.FavoriteColor.Value;
            if (SurferPlugin.FavoriteColor.Value >= 0)
            {
                if (PlayerControl.LocalPlayer.cosmetics.ColorId != favColorId && data.ColorId == favColorId)
                {
                    PlayerControl.LocalPlayer.CmdCheckColor(favColorId);
                }
            }
        }

        // Update host icon in meeting
        MeetingHudPatch.UpdateHostIcon();
        Logger_.Log($"[CRASH-TRACE] OnPlayerLeft END");
    }

    [HarmonyPatch(typeof(GameData))]
    [HarmonyPatch(nameof(GameData.HandleDisconnect))]
    [HarmonyPatch(MethodType.Normal)]
    [HarmonyPatch([typeof(PlayerControl), typeof(DisconnectReasons)])]
    [HarmonyPrefix]
    private static void GameData_HandleDisconnect_Prefix(PlayerControl player, DisconnectReasons reason)
    {
        Logger_.Log($"[CRASH-TRACE] HandleDisconnect START — player={player?.Data?.PlayerName}");
        // Store disconnect reason in player's BetterData
        if (player.BetterData() != null)
        {
            player.BetterData().DisconnectReason = reason;
        }

        // Show custom disconnect notification
        BetterShowNotification(player.Data, reason);
        Logger_.Log($"[CRASH-TRACE] HandleDisconnect END");
    }

    [HarmonyPatch(typeof(GameData), nameof(GameData.ShowNotification))]
    [HarmonyPrefix]
    internal static bool GameData_ShowNotification_Prefix()
    {
        // Disable vanilla disconnect notifications (use Surfer's instead)
        return false;
    }

    internal static void BetterShowNotification(NetworkedPlayerInfo playerData, DisconnectReasons reason = DisconnectReasons.Unknown, string forceReasonText = "")
    {
        if (HudManager.Instance == null || HudManager.Instance.Notifier == null) return;
        Logger_.Log($"[CRASH-TRACE] BetterShowNotification START — reason={reason}");
        // Prevent showing duplicate notifications
        var bd = playerData?.BetterData();
        if (bd == null) return;
        if (bd.AntiCheatInfo.BannedByAntiCheat || bd.HasShowDcMsg) return;
        bd.HasShowDcMsg = true;

        string? playerName = bd.RealName;

        // Use custom reason text if provided
        if (forceReasonText != "")
        {
            var ReasonText = $"<color=#ff0>{bd.RealName}</color> {forceReasonText}";

            Logger_.Log(ReasonText);

            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
        else
        {
            string ReasonText;

            // Format disconnect message based on reason type
            switch (reason)
            {
                case DisconnectReasons.ExitGame:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
                case DisconnectReasons.ClientTimeout:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Disconnect"), playerName);
                    break;
                case DisconnectReasons.Kicked:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Kicked"), playerName, AmongUsClient.Instance?.GetHost()?.Character?.Data?.PlayerName ?? "???");
                    break;
                case DisconnectReasons.Banned:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Banned"), playerName, AmongUsClient.Instance?.GetHost()?.Character?.Data?.PlayerName ?? "???");
                    break;
                case DisconnectReasons.Hacking:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Cheater"), playerName);
                    break;
                case DisconnectReasons.Error:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Error"), playerName);
                    break;
                case DisconnectReasons.Unknown:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Unknown"), playerName);
                    break;
                default:
                    ReasonText = string.Format(Translator.GetString("DisconnectReason.Left"), playerName);
                    break;
            }

            Logger_.Log(ReasonText);

            // Add formatted disconnect message to game UI
            HudManager.Instance.Notifier.AddDisconnectMessage(ReasonText);
        }
        Logger_.Log($"[CRASH-TRACE] BetterShowNotification END");
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.DisconnectInternal))]
    [HarmonyPrefix]
    private static void InnerNetClient_DisconnectInternal_Prefix(DisconnectReasons reason, string stringReason)
    {
        Logger_.Error($"[DISCONNECT] Local player disconnected — reason={reason} ({Enum.GetName(reason)}), detail=\"{stringReason}\", IsHost={GameState.IsHost}, IsGameStarting={GameState.IsGameStarting}, IsInGamePlay={GameState.IsInGamePlay}, IsLobby={GameState.IsLobby}, IsInGame={GameState.IsInGame}");
    }
}