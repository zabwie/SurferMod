using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class UseZiplineHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.UseZipline;
}
