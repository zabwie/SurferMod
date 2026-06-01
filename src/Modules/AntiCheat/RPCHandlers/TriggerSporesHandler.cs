using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class TriggerSporesHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.TriggerSpores;
}
