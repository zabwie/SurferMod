using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class ExiledHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.Exiled;
}
