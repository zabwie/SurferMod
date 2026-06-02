using HarmonyLib;
using InnerNet;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch]
internal static class CopyLobbyCodePatch
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnPlayerLeft))]
    [HarmonyPostfix]
    private static void AmongUsClient_OnPlayerLeft_Postfix()
    {
        if (SurferPlugin.CopyLobbyCode?.Value == true && AmongUsClient.Instance != null)
        {
            string code = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
            if (!string.IsNullOrEmpty(code))
                GUIUtility.systemCopyBuffer = code;
        }
    }
}