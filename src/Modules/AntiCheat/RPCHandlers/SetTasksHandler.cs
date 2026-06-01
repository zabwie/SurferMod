using Surfer.Attributes;

namespace Surfer.Modules.AntiCheat;

[RegisterRPCHandler]
internal sealed class SetTasksHandler : RPCHandler
{
    internal override byte CallId => (byte)RpcCalls.SetTasks;
}
