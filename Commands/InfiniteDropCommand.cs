using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class InfiniteDropCommand : ICommand
{
    public string Command => "infinitedrop";
    public string[] Aliases => new[] { "idrop" };
    public string Description => "Returns each item you drop or throw.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (player is null || state is null)
        {
            response = "This command can only be run by a player.";
            return false;
        }

        bool enabled = state.InfiniteDropPlayerIds.Add(player.PlayerId);
        if (!enabled)
            state.InfiniteDropPlayerIds.Remove(player.PlayerId);
        response = enabled ? "Infinite drop enabled." : "Infinite drop disabled.";
        return true;
    }
}
