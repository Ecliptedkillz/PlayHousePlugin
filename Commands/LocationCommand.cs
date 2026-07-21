using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class LocationCommand : ICommand
{
    public string Command => "location";
    public string[] Aliases => new[] { "loc" };
    public string Description => "Returns your exact position and rotation.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null)
        {
            response = "This command can only be run by a player.";
            return false;
        }

        var position = player.Position;
        var rotation = player.Rotation;
        response = $"Position: {position.x}, {position.y}, {position.z}\nRotation: {rotation.x}, {rotation.y}, {rotation.z}";
        return true;
    }
}
