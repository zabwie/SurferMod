using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetColorHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetColor;
}
