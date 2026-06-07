using Surfer.Data;
using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Modules;
using Surfer.Patches.Gameplay.UI.Settings;
using HarmonyLib;
using Hazel;
using InnerNet;

namespace Surfer.Patches.Client;

[HarmonyPatch]
internal static class InnerNetClientPatch
{
    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendOrDisconnect))]
    [HarmonyPrefix]
    private static bool InnerNetClient_SendOrDisconnect_Prefix(InnerNetClient __instance, MessageWriter msg)
    {
        try
        {
            // Route all outgoing messages through custom NetworkManager
            // This allows Surfer to intercept/modify network traffic
            NetworkManager.SendToServer(msg);
        }
        catch (Exception ex)
        {
            Logger_.Error(ex, "InnerNetClient.SendOrDisconnect");
        }
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.HandleGameData))]
    [HarmonyPrefix]
    private static bool InnerNetClient_HandleGameDataInner_Prefix([HarmonyArgument(0)] MessageReader oldReader)
    {
        try
        {
            // Route all incoming game data through custom NetworkManager
            // This allows Surfer to process/modify incoming network messages
            NetworkManager.HandleGameData(oldReader);
        }
        catch (Exception ex)
        {
            Logger_.Error(ex, "InnerNetClient.HandleGameData");
        }
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanBan))]
    [HarmonyPrefix]
    private static bool InnerNetClient_CanBan_Prefix(ref bool __result)
    {
        // Only allow hosts to ban players in Surfer
        __result = GameState.IsHost;
        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.CanKick))]
    [HarmonyPrefix]
    private static bool InnerNetClient_CanKick_Prefix(ref bool __result)
    {
        // Allow kicking under specific conditions:
        // 1. Host can always kick
        // 2. Non-hosts can only kick during meetings or when player is being voted out
        __result = GameState.IsHost || (GameState.IsInGamePlay && (GameState.IsMeeting || GameState.IsExilling));

        return false;
    }

    [HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.KickPlayer))]
    [HarmonyPrefix]
    private static void InnerNetClient_KickPlayer_Prefix(ref int clientId, ref bool ban)
    {
        // When banning a player, add them to Surfer's custom ban list if enabled
        if (ban && BetterGameSettings.UseBanPlayerList.GetBool())
        {
            // Get player info from client ID
            NetworkedPlayerInfo info = Utils.PlayerFromClientId(clientId).Data;

            // Add player to ban list using both friend code and PUID
            BetterDataManager.AddToBanList(info.FriendCode, info.Puid);
        }
    }
}