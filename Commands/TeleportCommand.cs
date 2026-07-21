using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class TeleportCommand : ICommand
{
    public string Command => "teleportx";
    public string[] Aliases => new[] { "tpx" };
    public string Description => "Teleports a player, or all players, to another player.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermission("at.tp"))
        {
            response = "Missing permission: at.tp";
            return false;
        }
        if (arguments.Count != 2)
        {
            response = "Usage: teleportx <player|all|*> <destination player>";
            return false;
        }

        Player? destination = Player.Get(arguments.At(1));
        if (destination is null)
        {
            response = $"Player not found: {arguments.At(1)}";
            return false;
        }

        if (arguments.At(0) is "all" or "*")
        {
            foreach (Player player in Player.ReadyList)
                if (player.IsAlive && player != destination)
                    player.Position = destination.Position;
            response = $"Everyone has been teleported to {destination.Nickname}.";
            return true;
        }

        Player? target = Player.Get(arguments.At(0));
        if (target is null)
        {
            response = $"Player not found: {arguments.At(0)}";
            return false;
        }
        target.Position = destination.Position;
        response = $"{target.Nickname} has been teleported to {destination.Nickname}.";
        return true;
    }
}
