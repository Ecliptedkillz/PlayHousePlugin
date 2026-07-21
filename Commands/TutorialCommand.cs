using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class TutorialCommand : ICommand
{
    public string Command => "tutorial";
    public string[] Aliases => new[] { "tut" };
    public string Description => "Toggles a player between Tutorial and Spectator.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermission("at.tut")) { response = "Missing permission: at.tut"; return false; }
        if (arguments.Count > 1) { response = "Usage: tutorial [player]"; return false; }

        Player? player = arguments.Count == 0 ? Player.Get(sender) : Player.Get(arguments.At(0));
        if (player is null) { response = "Player not found."; return false; }

        var oldPosition = player.Position;
        bool wasTutorial = player.Role == RoleTypeId.Tutorial;
        player.SetRole(wasTutorial ? RoleTypeId.Spectator : RoleTypeId.Tutorial,
            RoleChangeReason.RemoteAdmin, RoleSpawnFlags.All);
        if (!wasTutorial)
            player.Position = oldPosition;
        response = $"{player.Nickname} is now {(wasTutorial ? "Spectator" : "Tutorial")}.";
        return true;
    }
}
