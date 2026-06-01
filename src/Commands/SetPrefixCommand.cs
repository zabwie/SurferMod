using Surfer.Attributes;
using Surfer.Commands.Arguments;
using Surfer.Modules.Support;

namespace Surfer.Commands;

[RegisterCommand]
internal sealed class SetPrefixCommand : BaseCommand
{
    internal override string Name => "setprefix";
    internal override string Description => "Set command prefix";

    internal SetPrefixCommand()
    {
        prefixArgument = new StringArgument(this, "{prefix}");
        Arguments = [prefixArgument];
    }
    private StringArgument? prefixArgument { get; }

    internal override bool ShowCommand() => !SurferModdedSupportFlags.HasFlag(SurferModdedSupportFlags.Force_Surfer_Command_Prefix);

    internal override void Run()
    {
        var oldPrefix = SurferPlugin.CommandPrefix.Value;
        var prefix = prefixArgument.Arg.ToCharArray()?.First().ToString();
        if (!string.IsNullOrEmpty(prefix))
        {
            SurferPlugin.CommandPrefix.Value = prefix;
            CommandResultText($"Command prefix set from <#c1c100>{oldPrefix}</color> to <#c1c100>{prefix}</color>");
        }
        else
        {
            CommandErrorText("Invalid Syntax!");
        }
    }
}
