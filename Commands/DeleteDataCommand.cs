using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class DeleteDataCommand : ICommand
{
    public string Command => "deletedata";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Deletes your stored Playhouse statistics after this round.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (player is null || state is null) { response = "This command can only be run by a player."; return false; }
        bool added = state.PendingStatisticsDeletion.Add(player.UserId);
        response = added
            ? "Your data is marked for deletion and will be removed after this round."
            : "Your data is already marked for deletion.";
        return true;
    }
}
