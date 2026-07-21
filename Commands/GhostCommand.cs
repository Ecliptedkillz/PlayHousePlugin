using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class GhostCommand : ICommand
{
    public string Command => "ghost";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Toggles player invisibility.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermission("at.ghost")) { response = "Missing permission: at.ghost"; return false; }
        var invisible = PlayhousePlugin.Instance?.State.InvisiblePlayerIds;
        if (invisible is null) { response = "PlayhousePlugin is unavailable."; return false; }
        if (arguments.Count != 1) { response = "Usage: ghost <player|all|*|clear>"; return false; }
        string targetName = arguments.At(0);
        if (targetName.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            invisible.Clear();
            response = "Everyone is no longer invisible.";
            return true;
        }
        if (targetName is "all" or "*")
        {
            foreach (Player player in Player.ReadyList) invisible.Add(player.PlayerId);
            response = "Everyone is now invisible.";
            return true;
        }
        Player? target = Player.Get(targetName);
        if (target is null) { response = $"Player not found: {targetName}"; return false; }
        bool enabled = invisible.Add(target.PlayerId);
        if (!enabled) invisible.Remove(target.PlayerId);
        if (enabled) target.SendHint("You are invisible!", 3f);
        response = $"{target.Nickname} is {(enabled ? "now" : "no longer")} invisible.";
        return true;
    }
}
