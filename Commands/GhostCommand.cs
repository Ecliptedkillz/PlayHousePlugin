using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class GhostCommand : ICommand
{
    private static readonly HashSet<int> GhostPlayers = new();

    public string Command => "ghost";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Toggles the plugin ghost state for players.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.ghost"))
        {
            response = "Missing permission: at.ghost";
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "Usage: ghost <me|all|clear|player id|name>";
            return false;
        }

        string target = arguments.At(0);

        if (target.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            Player[] players = Player.List.ToArray();

            foreach (Player player in players)
                GhostPlayers.Add(player.PlayerId);

            response = $"Enabled ghost state for {players.Length} player(s).";
            return true;
        }

        if (target.Equals("clear", StringComparison.OrdinalIgnoreCase))
        {
            int count = GhostPlayers.Count;
            GhostPlayers.Clear();
            response = $"Cleared ghost state for {count} player(s).";
            return true;
        }

        Player? selected;

        if (target.Equals("me", StringComparison.OrdinalIgnoreCase))
        {
            selected = Player.Get(sender);
        }
        else
        {
            selected = FindPlayer(target);
        }

        if (selected is null)
        {
            response = $"Player '{target}' was not found.";
            return false;
        }

        bool enabled;

        if (GhostPlayers.Add(selected.PlayerId))
        {
            enabled = true;
        }
        else
        {
            GhostPlayers.Remove(selected.PlayerId);
            enabled = false;
        }

        response = enabled
            ? $"Enabled ghost state for {selected.Nickname}."
            : $"Disabled ghost state for {selected.Nickname}.";

        return true;
    }

    public static bool IsGhost(Player player)
    {
        return GhostPlayers.Contains(player.PlayerId);
    }

    private static Player? FindPlayer(string input)
    {
        if (int.TryParse(input, out int playerId))
        {
            Player? byId = Player.List.FirstOrDefault(
                player => player.PlayerId == playerId);

            if (byId is not null)
                return byId;
        }

        return Player.List.FirstOrDefault(
            player =>
                !string.IsNullOrWhiteSpace(player.Nickname) &&
                player.Nickname.IndexOf(
                    input,
                    StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
