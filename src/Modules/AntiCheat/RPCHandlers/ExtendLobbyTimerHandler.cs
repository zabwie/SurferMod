using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ExtendLobbyTimerHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.ExtendLobbyTimer;
}
