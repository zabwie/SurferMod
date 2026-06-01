using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetRoleHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetRole;
}
