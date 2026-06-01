using Surfer.Helpers;
using Surfer.Attributes;
using System.Text;

namespace Surfer.Commands;

[RegisterCommand]
internal sealed class PlayersInfoCommand : BaseCommand
{
    internal override string Name => "players";
    internal override string Description => "Get all Player information";

    internal override void Run()
    {
        StringBuilder sb = new();
        foreach (PlayerControl player in SurferPlugin.AllPlayerControls.Where(player => !player.isDummy))
        {
            var hexColor = Utils.Color32ToHex(Palette.PlayerColors[player.CurrentOutfit.ColorId]);
            sb.Append($"<color={hexColor}><b>{player?.Data?.PlayerName}</color> Info:</b>\n");
            sb.Append($"<color=#c1c1c1>{player?.Data?.PlayerId}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetHashStr($"{player?.Data?.Puid}")}</color> - ");
            sb.Append($"<color=#c1c1c1>{Utils.GetPlatformName(player)}</color> - ");
            sb.Append($"<color=#c1c1c1>{player?.Data?.FriendCode}</color>");
            sb.Append("\n\n");
        }
        CommandResultText(sb.ToString());
    }
}
