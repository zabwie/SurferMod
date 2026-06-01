using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetStartCounterHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetStartCounter;
}
