using AmongUs.GameOptions;
using BepInEx.Unity.IL2CPP.Utils;
using Surfer.Helpers;
using Surfer.Managers;
using Surfer.Modules;
using Il2CppInterop.Runtime.Attributes;
using InnerNet;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Surfer.Mono;

/// <summary>
/// Extended player information with additional data and anti-cheat features.
/// Plain C# class — NOT a MonoBehaviour, NOT registered with ClassInjector.
/// No finalizer = no IL2CPP crash on scene transitions or play-again.
/// </summary>
internal sealed class ExtendedPlayerInfo
{
    /// <summary>
    /// Dictionary mapping NetworkedPlayerInfo to their ExtendedPlayerInfo.
    /// Used for O(1) lookup in BetterData() and iteration in UpdateAll().
    /// </summary>
    internal static readonly Dictionary<NetworkedPlayerInfo, ExtendedPlayerInfo> _dataMap = [];

    internal ExtendedPlayerInfo()
    {
        HandshakeHandler = new(this);
    }

    /// <summary>
    /// Gets or sets the base NetworkedPlayerInfo instance.
    /// </summary>
    internal NetworkedPlayerInfo? BaseMono { get; set; }

    /// <summary>
    /// Gets the NetworkedPlayerInfo instance.
    /// </summary>
    internal NetworkedPlayerInfo? _Data => BaseMono;

    private bool hasSet = false;

    /// <summary>
    /// Initializes the extended player info.
    /// </summary>
    /// <param name="data">The NetworkedPlayerInfo to extend.</param>
    [HideFromIl2Cpp]
    internal void SetInfo(NetworkedPlayerInfo data)
    {
        if (hasSet) return;
        BaseMono = data;
        _PlayerId = data.PlayerId;
        hasSet = true;
        _dataMap[data] = this;

        // Start handshake coroutine — data is a MonoBehaviour (InnerNetObject)
        HandshakeHandler.WaitSendSecretToPlayer(data);
    }

    /// <summary>
    /// Removes an ExtendedPlayerInfo from tracking.
    /// </summary>
    /// <param name="data">The NetworkedPlayerInfo to remove.</param>
    internal static void Remove(NetworkedPlayerInfo data)
    {
        _dataMap.Remove(data);
    }

    private float timeAccumulator = 0f;

    /// <summary>
    /// Updates anti-cheat monitoring and state tracking for all active instances.
    /// Called from ModManagerPatch.LateUpdate each frame.
    /// </summary>
    internal static void UpdateAll()
    {
        List<NetworkedPlayerInfo>? toRemove = null;

        foreach (var kvp in _dataMap)
        {
            var epi = kvp.Value;
            if (epi._Data == null || epi._Data.Object == null)
            {
                (toRemove ??= []).Add(kvp.Key);
                continue;
            }

            var time = Time.deltaTime;
            epi.AntiCheatInfo!.TimeSinceLastTask += time;

            if (epi._Data.Object.IsLocalPlayer()) continue;

            if (epi.AntiCheatInfo.RPCSentPS > 0)
            {
                bool flag = epi._Data.IsCheater();

                if (epi.AntiCheatInfo.RPCSentPS >= ExtendedAntiCheatInfo.MAX_RPC_SENT && !flag)
                {
                    BetterNotificationManager.NotifyCheat(epi._Data.Object,
                        Translator.GetString("AntiCheat.Reason.RPCSentPS"),
                        Translator.GetString("AntiCheat.UnauthorizedAction"));
                    Logger_.LogCheat($"{epi._Data.Object.BetterData()!.RealName} {epi.AntiCheatInfo.RPCSentPS} Sent.");
                }

                epi.timeAccumulator += time;
                if (epi.timeAccumulator >= 0.25f - 0.005 * epi.AntiCheatInfo.RPCSentPS)
                {
                    epi.AntiCheatInfo.RPCSentPS -= 1;
                    epi.timeAccumulator = 0f;
                }
            }
        }

        if (toRemove != null)
        {
            foreach (var key in toRemove)
            {
                _dataMap.Remove(key);
            }
        }
    }

    /// <summary>
    /// Gets the handshake handler for this player.
    /// </summary>
    [HideFromIl2Cpp]
    internal HandshakeHandler HandshakeHandler { get; }

    /// <summary>
    /// Gets the player ID.
    /// </summary>
    [HideFromIl2Cpp]
    internal byte _PlayerId { get; private set; }

    /// <summary>
    /// Gets the player's real name.
    /// </summary>
    internal string RealName => _Data?.PlayerName ?? "???";

    /// <summary>
    /// Gets or sets the last name set for this player.
    /// </summary>
    internal string NameSetAsLast { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this player is a Surfer user.
    /// </summary>
    internal bool IsBetterUser { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this player is a verified Surfer user.
    /// </summary>
    internal bool IsVerifiedBetterUser { get; set; } = false;

    /// <summary>
    /// Gets or sets whether disconnect message has been shown.
    /// </summary>
    internal bool HasShowDcMsg { get; set; } = false;

    /// <summary>
    /// Gets or sets the disconnect reason.
    /// </summary>
    internal DisconnectReasons DisconnectReason { get; set; } = DisconnectReasons.Unknown;

    /// <summary>
    /// Gets the extended role information.
    /// </summary>
    [HideFromIl2Cpp]
    internal ExtendedRoleInfo? RoleInfo { get; } = new();

    /// <summary>
    /// Gets the extended anti-cheat information.
    /// </summary>
    [HideFromIl2Cpp]
    internal ExtendedAntiCheatInfo? AntiCheatInfo { get; } = new();
}

/// <summary>
/// Contains anti-cheat monitoring information for a player.
/// </summary>
internal class ExtendedAntiCheatInfo
{
    /// <summary>
    /// Maximum allowed RPCs per second.
    /// </summary>
    internal const int MAX_RPC_SENT = 50;

    /// <summary>
    /// Gets or sets whether the player is banned by anti-cheat.
    /// </summary>
    internal bool BannedByAntiCheat { get; set; } = false;

    /// <summary>
    /// Gets or sets the list of AUM chat messages.
    /// </summary>
    internal List<string> AUMChats { get; set; } = [];

    /// <summary>
    /// Gets or sets the RPCs sent per second.
    /// </summary>
    internal int RPCSentPS { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of times attempted to kill.
    /// </summary>
    internal int TimesAttemptedKilled { get; set; } = 0;

    /// <summary>
    /// Gets or sets the number of open sabotages.
    /// </summary>
    internal int OpenSabotageNum { get; set; } = 0;

    /// <summary>
    /// Gets whether the player is fixing panel sabotage.
    /// </summary>
    internal bool IsFixingPanelSabotage => OpenSabotageNum != 0;

    /// <summary>
    /// Gets or sets the time since last task.
    /// </summary>
    internal float TimeSinceLastTask { get; set; } = 5f;

    /// <summary>
    /// Gets or sets the last task ID.
    /// </summary>
    internal uint LastTaskId { get; set; } = 999;

    /// <summary>
    /// Gets or sets whether the player has set their name.
    /// </summary>
    internal bool HasSetName { get; set; }

    /// <summary>
    /// Gets or sets whether the player has set their level.
    /// </summary>
    internal bool HasSetLevel { get; set; }
}

/// <summary>
/// Contains extended role information for a player.
/// </summary>
internal class ExtendedRoleInfo
{
    /// <summary>
    /// Gets or sets the number of kills.
    /// </summary>
    internal int Kills { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether noisemaker notification is enabled.
    /// </summary>
    internal bool HasNoisemakerNotify { get; set; } = false;

    /// <summary>
    /// Gets or sets the role to display when dead.
    /// </summary>
    internal RoleTypes DeadDisplayRole { get; set; }
}

/// <summary>
/// Extension methods for accessing extended player data.
/// </summary>
internal static class PlayerControlDataExtension
{
    /// <summary>
    /// Gets extended player data from a PlayerControl.
    /// </summary>
    /// <param name="player">The PlayerControl instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this PlayerControl player)
    {
        if (player?.Data == null) return null;
        return player.Data.BetterData();
    }

    /// <summary>
    /// Waits for extended player data to be available, then calls a callback.
    /// </summary>
    /// <param name="player">The PlayerControl instance.</param>
    /// <param name="callback">The callback to execute with the extended data.</param>
    internal static void BetterDataWait(this PlayerControl player, Action<ExtendedPlayerInfo> callback)
    {
        if (player == null) return;
        player.StartCoroutine(CoBetterDataWait(player, callback));
    }

    [HideFromIl2Cpp]
    private static IEnumerator CoBetterDataWait(PlayerControl player, Action<ExtendedPlayerInfo> callback)
    {
        ExtendedPlayerInfo? epi;
        while ((epi = player.BetterData()) == null)
            yield return null;
        callback(epi);
    }

    /// <summary>
    /// Gets extended player data from a NetworkedPlayerInfo.
    /// </summary>
    /// <param name="data">The NetworkedPlayerInfo instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this NetworkedPlayerInfo data)
    {
        if (data == null) return null;
        ExtendedPlayerInfo._dataMap.TryGetValue(data, out var epi);
        return epi;
    }

    /// <summary>
    /// Gets extended player data from a ClientData.
    /// </summary>
    /// <param name="data">The ClientData instance.</param>
    /// <returns>The ExtendedPlayerInfo, or null if not found.</returns>
    internal static ExtendedPlayerInfo? BetterData(this ClientData data)
    {
        var player = Utils.PlayerFromClientId(data.Id);
        return player?.Data?.BetterData();
    }
}
