using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class PositionCommand : ICommand
{
    public string Command => "position";
    public string[] Aliases => new[] { "pos" };
    public string Description => "Gets or changes a player's position.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count is not (1 or 4))
        {
            response = "Usage: position <player> [x y z]";
            return false;
        }
        Player? player = Player.Get(arguments.At(0));
        if (player is null)
        {
            response = $"Player not found: {arguments.At(0)}";
            return false;
        }
        if (arguments.Count == 1)
        {
            response = $"{player.Nickname}: {player.Position}";
            return true;
        }
        if (!float.TryParse(arguments.At(1), out float x) || !float.TryParse(arguments.At(2), out float y) ||
            !float.TryParse(arguments.At(3), out float z))
        {
            response = "Invalid position.";
            return false;
        }
        player.Position = new Vector3(x, y, z);
        response = $"Moved {player.Nickname} to {player.Position}.";
        return true;
    }
}
