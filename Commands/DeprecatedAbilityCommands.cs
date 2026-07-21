using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

public abstract class DeprecatedAbilityCommand : ICommand
{
    public abstract string Command { get; }
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Legacy custom-class ability binding.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "This command is no longer supported; bind .activateability and .changeability instead.";
        return false;
    }
}
[CommandHandler(typeof(ClientCommandHandler))] public sealed class Ability1Command : DeprecatedAbilityCommand { public override string Command => "ability1"; }
[CommandHandler(typeof(ClientCommandHandler))] public sealed class Ability2Command : DeprecatedAbilityCommand { public override string Command => "ability2"; }
[CommandHandler(typeof(ClientCommandHandler))] public sealed class Ability3Command : DeprecatedAbilityCommand { public override string Command => "ability3"; }
[CommandHandler(typeof(ClientCommandHandler))] public sealed class Ability4Command : DeprecatedAbilityCommand { public override string Command => "ability4"; }
