using HarmonyLib;
using InnerNet;
using Surfer.Helpers;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch]
internal static class CopyLobbyCodePatch
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerLeft_Postfix(ClientData data)
    {
        if (SurferPlugin.CopyLobbyCode?.Value == true && data?.Character != null && data.Character.IsLocalPlayer() && AmongUsClient.Instance != null)
        {
            string code = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            if (!string.IsNullOrEmpty(code))
                GUIUtility.systemCopyBuffer = code;
        }
    }
}