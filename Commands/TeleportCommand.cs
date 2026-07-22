using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class TeleportCommand : ICommand
{
    public string Command => "teleportx";

    public string[] Aliases => new[]
    {
        "tpx"
    };

    public string Description =>
        "Teleports a player, or all players, to another player.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.tp"))
        {
            response = "Missing permission: at.tp";
            return false;
        }

        if (arguments.Count != 2)
        {
            response =
                "Usage: teleportx <player ID/name|all|*> <destination ID/name>";
            return false;
        }

        string targetArgument = arguments.At(0);
        string destinationArgument = arguments.At(1);

        Player? destination = FindPlayer(destinationArgument);

        if (destination is null)
        {
            response = $"Destination player not found: {destinationArgument}";
            return false;
        }

        if (targetArgument.Equals("all", StringComparison.OrdinalIgnoreCase) ||
            targetArgument == "*")
        {
            int teleported = 0;

            foreach (Player player in Player.ReadyList.ToArray())
            {
                if (player == destination || !player.IsAlive)
                    continue;

                player.Position = destination.Position;
                teleported++;
            }

            response =
                $"Teleported {teleported} player(s) to " +
                $"{destination.Nickname} ({destination.PlayerId}).";

            return true;
        }

        Player? target = FindPlayer(targetArgument);

        if (target is null)
        {
            response = $"Player not found: {targetArgument}";
            return false;
        }

        if (!target.IsAlive)
        {
            response =
                $"{target.Nickname} ({target.PlayerId}) is not currently alive.";
            return false;
        }

        if (!destination.IsAlive)
        {
            response =
                $"{destination.Nickname} ({destination.PlayerId}) is not currently alive.";
            return false;
        }

        target.Position = destination.Position;

        response =
            $"Teleported {target.Nickname} ({target.PlayerId}) to " +
            $"{destination.Nickname} ({destination.PlayerId}).";

        return true;
    }

    private static Player? FindPlayer(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Remote Admin player ID, such as 2, 4, or 5.
        if (int.TryParse(input, out int playerId))
        {
            Player? idMatch = Player.ReadyList.FirstOrDefault(
                player => player.PlayerId == playerId);

            if (idMatch is not null)
                return idMatch;
        }

        // Exact Steam/Discord user ID.
        Player? userIdMatch = Player.ReadyList.FirstOrDefault(
            player => string.Equals(
                player.UserId,
                input,
                StringComparison.OrdinalIgnoreCase));

        if (userIdMatch is not null)
            return userIdMatch;

        // Exact nickname.
        Player? exactNameMatch = Player.ReadyList.FirstOrDefault(
            player => string.Equals(
                player.Nickname,
                input,
                StringComparison.OrdinalIgnoreCase));

        if (exactNameMatch is not null)
            return exactNameMatch;

        // Partial nickname, only when there is exactly one match.
        Player[] partialNameMatches = Player.ReadyList
        .Where(player =>
            !string.IsNullOrEmpty(player.Nickname) &&
            player.Nickname.IndexOf(
                input,
                StringComparison.OrdinalIgnoreCase) >= 0)
        .ToArray();
    
    return partialNameMatches.Length == 1
        ? partialNameMatches[0]
        : null;
        }
}