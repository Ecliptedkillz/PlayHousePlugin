using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class ContentGunClearCommand : ICommand
{
    public string Command => "contentclear";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Clears all active content gun usages.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        PlayhousePlugin? plugin = PlayhousePlugin.Instance;

        if (player is null || plugin is null)
        {
            response = "Player only.";
            return false;
        }

        // Restrict to your account (same as the old command)
        if (!player.UserId.Equals("Eclipted", StringComparison.OrdinalIgnoreCase))
        {
            response = "You do not have permission to use this command.";
            return false;
        }

        plugin.State.ContentGunRounds.Clear();

        response = "Cleared all active content gun usages.";
        return true;
    }
}