using Surfer.Commands.Arguments;
using Surfer.Data;
using Surfer.Helpers;
using Surfer.Attributes;

namespace Surfer.Commands;

[RegisterCommand]
internal sealed class RemovePlayerCommand : BaseCommand
{
    internal override string Name => "removeplayer";
    internal override string Description => "Remove player from local <color=#4f92ff>Anti-Cheat</color> data";

    public RemovePlayerCommand()
    {
        identifierArgument = new StringArgument(this, "{identifier}")
        {
            GetArgSuggestions = () =>
                BetterDataManager.BetterDataFile.AllCheatData
                    .SelectMany(info => new[] { info.HashPuid.Replace(' ', '_'), info.FriendCode.Replace(' ', '_'), info.PlayerName.Replace(' ', '_') })
                    .ToArray()
        };
        Arguments = [identifierArgument];
    }
    private StringArgument identifierArgument { get; }

    internal override void Run()
    {
        if (BetterDataManager.RemovePlayer(identifierArgument.Arg) == true)
        {
            Utils.AddChatPrivate($"{identifierArgument.Arg} successfully removed from local <color=#4f92ff>Anti-Cheat</color> data!");
        }
        else
        {
            Utils.AddChatPrivate($"{identifierArgument.Arg} Could not find player data from identifier");
        }
    }
}
