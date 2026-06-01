using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetInfectedHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetInfected;
}
