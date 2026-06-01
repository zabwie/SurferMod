using HarmonyLib;
using UnityEngine;

namespace Surfer.Modules;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnGameJoined))]
internal static class CopyLobbyCodePatch
{
    public static string LastGameId = "";

    public static void Postfix(string gameIdString)
    {
        LastGameId = gameIdString;

        if (SurferPlugin.CopyLobbyCode?.Value == true && !string.IsNullOrEmpty(gameIdString))
        {
            GUIUtility.systemCopyBuffer = gameIdString;
        }
    }
}
