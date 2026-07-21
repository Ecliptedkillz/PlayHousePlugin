using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class ForceReloadCommand : ICommand
{
    public string Command => "forcereload";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Forces all currently held firearms to reload.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        int count = 0;
        foreach (Player player in Player.ReadyList)
        {
            if (player.CurrentItem is not FirearmItem firearm || !firearm.CanReload) continue;
            firearm.Reload();
            count++;
        }
        response = $"Forced {count} held firearm(s) to reload.";
        return true;
    }
}
