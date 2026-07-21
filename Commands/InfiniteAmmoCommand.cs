using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class InfiniteAmmoCommand : ICommand
{
    public string Command => "infammo";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Toggles, lists, or clears infinite ammo.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (!sender.HasPermission("ct.infammo"))
        {
            response = "Missing permission: ct.infammo";
            return false;
        }

        var state = PlayhousePlugin.Instance?.State;
        if (state is null)
        {
            response = "PlayhousePlugin is unavailable.";
            return false;
        }

        if (arguments.Count == 1 && arguments.At(0).ToLowerInvariant() is "all" or "*")
        {
            foreach (Player player in Player.ReadyList) state.InfiniteAmmoPlayerIds.Add(player.PlayerId);
            Server.SendBroadcast("Everyone has been given infinite ammo!", 5);
            response = "Infinite ammo enabled for everyone.";
            return true;
        }

        if (arguments.Count == 1 && arguments.At(0).Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            state.InfiniteAmmoPlayerIds.Clear();
            Server.SendBroadcast("Infinite ammo has been taken away from everyone!", 5);
            response = "Infinite ammo cleared.";
            return true;
        }

        if (arguments.Count == 1 && arguments.At(0).Equals("list", StringComparison.OrdinalIgnoreCase))
        {
            string[] names = Player.ReadyList.Where(p => state.InfiniteAmmoPlayerIds.Contains(p.PlayerId)).Select(p => p.Nickname).ToArray();
            response = names.Length == 0 ? "There are no players with infinite ammo." : "Players with infinite ammo: " + string.Join(", ", names);
            return true;
        }

        if (arguments.Count == 2 && arguments.At(0).Equals("give", StringComparison.OrdinalIgnoreCase))
        {
            Player? target = Player.Get(arguments.At(1));
            if (target is null)
            {
                response = $"Player not found: {arguments.At(1)}";
                return false;
            }

            bool enabled = state.InfiniteAmmoPlayerIds.Add(target.PlayerId);
            if (!enabled) state.InfiniteAmmoPlayerIds.Remove(target.PlayerId);
            target.SendBroadcast($"Infinite ammo is {(enabled ? "enabled" : "disabled")} for you!", 5);
            response = $"Infinite ammo {(enabled ? "enabled" : "disabled")} for {target.Nickname}.";
            return true;
        }

        response = "Usage: infammo <all|clear|list|give player>";
        return false;
    }
}
