using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class UsePlatformHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.UsePlatform;
}
