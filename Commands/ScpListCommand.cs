using System;
using System.Text;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ScpListCommand : ICommand
{
    public string Command => "scplist";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Lists living SCP teammates.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? caller = Player.Get(sender);
        if (caller is null || caller.Team != Team.SCPs)
        {
            response = "Only SCP players can use this command.";
            return false;
        }
        var text = new StringBuilder("----------");
        foreach (Player player in Player.ReadyList)
        {
            if (player.Team != Team.SCPs || !player.IsAlive) continue;
            text.Append("\n").Append(player.Nickname).Append(" - ").Append(player.Role)
                .Append(" - ").Append(player.Health).Append(" HP\nCurrent Zone: ")
                .Append(player.Zone).Append("\n----------");
        }
        response = text.ToString();
        return true;
    }
}
