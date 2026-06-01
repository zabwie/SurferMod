using BepInEx.Unity.IL2CPP.Utils;
using HarmonyLib;
using Il2CppInterop.Runtime.Attributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Surfer.Mono;

/// <summary>
/// Extends PlayerControl with additional functionality.
/// Plain C# class — NOT a MonoBehaviour, NOT registered with ClassInjector.
/// Instances stored in static dictionary to avoid IL2CPP finalizer crashes.
/// </summary>
internal sealed class ExtendedPlayerControl
{
    internal static readonly Dictionary<PlayerControl, ExtendedPlayerControl> _instances = [];

    /// <summary>
    /// Gets or sets the base PlayerControl instance.
    /// </summary>
    public PlayerControl? BaseMono { get; set; }

    /// <summary>
    /// Gets the PlayerControl instance.
    /// </summary>
    internal PlayerControl? _Player => BaseMono;

    /// <summary>
    /// Dictionary storing last name set for each player.
    /// </summary>
    internal readonly Dictionary<NetworkedPlayerInfo, string> NameSetLastFor = [];

    /// <summary>
    /// Attempts to create extended data for a player.
    /// Creates a plain C# ExtendedPlayerInfo — no MonoBehaviour, no AddComponent.
    /// </summary>
    /// <param name="data">The player data to extend.</param>
    internal static void TryCreateExtendedData(NetworkedPlayerInfo data)
    {
        if (data.BetterData() != null) return;
        var epi = new ExtendedPlayerInfo();
        epi.SetInfo(data);
    }
}

/// <summary>
/// Extension methods for PlayerControl.
/// </summary>
internal static class PlayerControlExtension
{
    [HarmonyPatch(typeof(PlayerControl))]
    private class PlayerControlPatch
    {
        [HarmonyPatch(nameof(PlayerControl.Awake))]
        [HarmonyPrefix]
        internal static void Awake_Prefix(PlayerControl __instance)
        {
            TryCreateExtendedPlayerControl(__instance);
        }

        [HarmonyPatch(nameof(PlayerControl.OnDestroy))]
        [HarmonyPrefix]
        internal static void OnDestroy_Prefix(PlayerControl __instance)
        {
            ExtendedPlayerControl._instances.Remove(__instance);
        }

        internal static void TryCreateExtendedPlayerControl(PlayerControl pc)
        {
            if (pc.BetterPlayerControl() != null) return;

            var epc = new ExtendedPlayerControl { BaseMono = pc };
            ExtendedPlayerControl._instances[pc] = epc;

            // Create PlayerInfoDisplay (was in old Awake)
            pc.gameObject.AddComponent<PlayerInfoDisplay>().Init(pc);

            // Schedule BetterData creation after player data loads
            pc.StartCoroutine(CoAddBetterData(pc));
        }

        [HideFromIl2Cpp]
        private static IEnumerator CoAddBetterData(PlayerControl pc)
        {
            while (pc?.Data == null)
            {
                yield return null;
            }
            ExtendedPlayerControl.TryCreateExtendedData(pc.Data);
        }
    }

    /// <summary>
    /// Gets the extended player control for a PlayerControl.
    /// </summary>
    /// <param name="player">The PlayerControl instance.</param>
    /// <returns>The ExtendedPlayerControl, or null if not found.</returns>
    internal static ExtendedPlayerControl? BetterPlayerControl(this PlayerControl player)
    {
        if (player == null) return null;
        ExtendedPlayerControl._instances.TryGetValue(player, out var epc);
        return epc;
    }

    /// <summary>
    /// Gets the extended player control for a PlayerPhysics.
    /// </summary>
    /// <param name="playerPhysics">The PlayerPhysics instance.</param>
    /// <returns>The ExtendedPlayerControl, or null if not found.</returns>
    internal static ExtendedPlayerControl? BetterPlayerControl(this PlayerPhysics playerPhysics)
    {
        return playerPhysics?.myPlayer?.BetterPlayerControl();
    }
}
