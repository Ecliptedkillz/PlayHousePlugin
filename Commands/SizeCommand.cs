using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SizeCommand : ICommand
{
    public string Command => "size";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Changes player scale.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermission("at.size"))
        {
            response = "Missing permission: at.size";
            return false;
        }
        if (arguments.Count == 1 && arguments.At(0) == "reset")
        {
            foreach (Player player in Player.ReadyList)
                player.Scale = Vector3.one;
            response = "Everyone's size has been reset.";
            return true;
        }
        if (arguments.Count != 4 ||
            !float.TryParse(arguments.At(1), out float x) ||
            !float.TryParse(arguments.At(2), out float y) ||
            !float.TryParse(arguments.At(3), out float z))
        {
            response = "Usage: size <player|all|*> <x> <y> <z>, or size reset";
            return false;
        }

        Vector3 scale = new(x, y, z);
        if (arguments.At(0) is "all" or "*")
        {
            foreach (Player player in Player.ReadyList)
                player.Scale = scale;
            response = $"Everyone's scale is now {x} {y} {z}.";
            return true;
        }

        Player? target = Player.Get(arguments.At(0));
        if (target is null)
        {
            response = $"Player not found: {arguments.At(0)}";
            return false;
        }
        target.Scale = scale;
        response = $"{target.Nickname}'s scale is now {x} {y} {z}.";
        return true;
    }
}
