using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class LobbyTimeExpiringHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.LobbyTimeExpiring;
}
