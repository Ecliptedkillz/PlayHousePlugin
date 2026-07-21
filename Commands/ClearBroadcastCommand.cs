using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ClearBroadcastCommand : ICommand
{
    public string Command => "clearbroadcast";
    public string[] Aliases => new[] { "clearbc", "clearbroadcasts", "clearbcs" };
    public string Description => "Clears your broadcasts.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = "This command can only be run by a player.";
            return false;
        }

        player.ClearBroadcasts();
        response = "Cleared broadcasts!";
        return true;
    }
}
