using AmongUs.GameOptions;
using Surfer.Attributes;
using Surfer.Helpers;
using Surfer.Managers;
using Hazel;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class StartVanishHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.StartVanish;

    internal override bool HandleAntiCheatCancel(PlayerControl? sender, MessageReader reader)
    {
        if (RoleCheck(sender) == false)
        {
            return false;
        }

        if (sender.IsInVent())
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                LogRpcInfo($"Phantom attempted to vanish while in vent");
            }
            return false;
        }

        return true;
    }

    internal bool RoleCheck(PlayerControl? sender)
    {
        if (!sender.Is(RoleTypes.Phantom) || !sender.IsAlive())
        {
            if (BetterNotificationManager.NotifyCheat(sender, GetFormatActionText()))
            {
                string issue = GetRoleCheckIssue(sender);
                LogRpcInfo($"Invalid vanish role: {issue}");
            }
            return false;
        }

        return true;
    }

    private static string GetRoleCheckIssue(PlayerControl sender)
    {
        if (!sender.Is(RoleTypes.Phantom)) return "Sender not Phantom role";
        if (!sender.IsAlive()) return "Sender not alive";

        return "Unknown role check issue";
    }
}
