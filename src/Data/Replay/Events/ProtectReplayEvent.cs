using AmongUs.GameOptions;
using Surfer.Helpers;
using Surfer.Interfaces;
using System.Text.Json.Serialization;

namespace Surfer.Data.Replay.Events;

[Serializable]
internal sealed class ProtectReplayEvent : IReplayEvent<(int playerId, int targetId)>
{
    public string Id => "protect_player";

    [JsonInclude]
    public (int playerId, int targetId) EventData { get; set; }

    public void Play()
    {
        var player = Utils.PlayerFromPlayerId(EventData.playerId);
        var target = Utils.PlayerFromPlayerId(EventData.targetId);
        if (player?.Data.RoleType is RoleTypes.GuardianAngel)
        {
            player?.ProtectPlayer(target, player.Data.DefaultOutfit.ColorId);
        }
    }

    public void Record(PlayerControl player, PlayerControl target)
    {
        EventData = (player.PlayerId, target.PlayerId);
    }
}
