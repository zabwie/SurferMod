using Surfer.Modules;
using Surfer.Patches.Gameplay.UI.Settings;
using HarmonyLib;

namespace Surfer.Managers;

/// <summary>
/// Manages lobby-specific behaviors for private lobbies
/// </summary>
[HarmonyPatch]
internal static class PrivateOnlyLobbyManager
{
    [HarmonyPatch(typeof(PlayerControl))]
    [HarmonyPatch(nameof(PlayerControl.Die))]
    [HarmonyPostfix]
    internal static void PlayerControlDie_Postfix(PlayerControl __instance)
    {
        if (!GameState.IsHost) return;

        if (GameState.IsPrivateOnlyLobby && BetterGameSettings.RemovePetOnDeath.GetBool())
        {
            __instance.RpcSetPet(PetData.EmptyId);
        }
    }
}