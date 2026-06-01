using Surfer.Data;
using Surfer.Helpers;
using Surfer.Attributes;

namespace Surfer.Commands;

[RegisterCommand]
internal sealed class RemoveAllCommand : BaseCommand
{
    internal override string Name => "removeall";
    internal override string Description => "Remove all players from local <color=#4f92ff>Anti-Cheat</color> data";
    internal override void Run()
    {
        BetterDataManager.ClearCheatData();
        Utils.AddChatPrivate($"All data successfully removed from local <color=#4f92ff>Anti-Cheat</color>!");
    }
}
