using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class CommandsHelpCommand : ICommand
{
    public string Command => "commands";
    public string[] Aliases => new[] { "playhousecommands" };
    public string Description => "Lists PlayhousePlugin commands.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response =
            "Playhouse commands:\n" +
            ".commands - Shows this list\n" +
            ".kill / .suicide - Kills yourself\n" +
            ".scplist - Lists living SCP teammates\n" +
            ".pets - Opens the donator pets menu\n" +
            ".deletedata - Deletes your stored statistics\n" +
            ".discord - Shows the Discord invite\n" +
            ".clearbroadcast - Clears your broadcasts";
        return true;
    }
}
