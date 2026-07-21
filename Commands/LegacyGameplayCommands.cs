using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using CustomPlayerEffects;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayhousePlugin.Runtime;
using UnityEngine;

namespace PlayhousePlugin.Commands;

internal static class PlayerFinder
{
    public static Player? Find(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        if (int.TryParse(input, out int playerId))
        {
            Player? playerById = Player.List.FirstOrDefault(
                player => player.PlayerId == playerId
            );

            if (playerById is not null)
                return playerById;
        }

        Player? playerByUserId = Player.List.FirstOrDefault(
            player => string.Equals(
                player.UserId,
                input,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (playerByUserId is not null)
            return playerByUserId;

        Player? playerByExactName = Player.List.FirstOrDefault(
            player => string.Equals(
                player.Nickname,
                input,
                StringComparison.OrdinalIgnoreCase
            )
        );

        if (playerByExactName is not null)
            return playerByExactName;

        return Player.List.FirstOrDefault(
            player =>
                !string.IsNullOrWhiteSpace(player.Nickname) &&
                player.Nickname.IndexOf(
                    input,
                    StringComparison.OrdinalIgnoreCase
                ) >= 0
        );
    }

    public static bool IsConnected(Player player)
    {
        return Player.List.Any(current =>
            current.PlayerId == player.PlayerId &&
            current.UserId == player.UserId);
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class JailCommand : ICommand
{
    private static readonly Dictionary<string, JailState> Jailed = new();

    public string Command => "jail";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Jails or releases a player.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.jail"))
        {
            response = "Missing permission: at.jail";
            return false;
        }

        if (arguments.Count != 1)
        {
            response = "Usage: jail <player id|name|user id>";
            return false;
        }

        string input = arguments.At(0);

        Player? player = PlayerFinder.Find(input);

        if (player is null)
        {
            response = $"Player '{input}' not found.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(player.UserId))
        {
            response = "That player does not have a valid User ID.";
            return false;
        }

        // Running the command on an already-jailed player releases them.
        if (Jailed.TryGetValue(player.UserId, out JailState state))
        {
            Jailed.Remove(player.UserId);

            player.SetRole(
                state.Role,
                RoleChangeReason.RemoteAdmin,
                RoleSpawnFlags.All
            );

            PlayhousePlugin.Instance?.Runtime?.Schedule(0.5f, () =>
            {
                if (!PlayerFinder.IsConnected(player))
                    return;

                state.Restore(player);

                player.Position = state.Position;
                player.Health = Math.Min(player.MaxHealth, state.Health);
                player.CustomInfo = string.Empty;
            });

            response = $"{player.Nickname} has been released.";
            return true;
        }

        ItemType[] itemTypes = player.Items
            .Select(item => item.Type)
            .ToArray();

        Dictionary<ItemType, ushort> ammo = new();

        ItemType[] ammoTypes =
        {
            ItemType.Ammo9x19,
            ItemType.Ammo556x45,
            ItemType.Ammo762x39,
            ItemType.Ammo12gauge,
            ItemType.Ammo44cal,
        };

        foreach (ItemType ammoType in ammoTypes)
            ammo[ammoType] = player.GetAmmo(ammoType);

        Jailed[player.UserId] = new JailState(
            player.Role,
            player.Position,
            player.Health,
            itemTypes,
            ammo
        );

        player.SetRole(
            RoleTypeId.Tutorial,
            RoleChangeReason.RemoteAdmin,
            RoleSpawnFlags.All
        );

        PlayhousePlugin.Instance?.Runtime?.Schedule(0.5f, () =>
        {
            if (!PlayerFinder.IsConnected(player))
                return;

            player.ClearInventory();
            player.Position = new Vector3(38.845f, 314.112f, -31.992f);
            player.CustomInfo = "JAILED";
        });

        response = $"{player.Nickname} has been jailed.";
        return true;
    }

    private readonly struct JailState
    {
        public JailState(
            RoleTypeId role,
            Vector3 position,
            float health,
            ItemType[] items,
            Dictionary<ItemType, ushort> ammo)
        {
            Role = role;
            Position = position;
            Health = health;
            Items = items;
            Ammo = ammo;
        }

        public RoleTypeId Role { get; }

        public Vector3 Position { get; }

        public float Health { get; }

        public ItemType[] Items { get; }

        public Dictionary<ItemType, ushort> Ammo { get; }

        public void Restore(Player player)
        {
            player.ClearInventory();

            foreach (ItemType item in Items)
                player.AddItem(item);

            foreach (KeyValuePair<ItemType, ushort> pair in Ammo)
                player.SetAmmo(pair.Key, pair.Value);
        }
    }
}

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class MiniBossCommand : ICommand
{
    public string Command => "miniboss";

    public string[] Aliases => new[]
    {
        "mb",
        "mini",
        "boss",
    };

    public string Description => "Turns selected players into legacy mini-bosses.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.miniboss"))
        {
            response = "Missing permission: at.miniboss";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = "Usage: miniboss <player id|name...>";
            return false;
        }

        int changed = 0;
        List<string> notFound = new();

        foreach (string argument in arguments)
        {
            Player? player = PlayerFinder.Find(argument);

            if (player is null)
            {
                notFound.Add(argument);
                continue;
            }

            player.SetRole(
                RoleTypeId.ChaosRepressor,
                RoleChangeReason.RemoteAdmin,
                RoleSpawnFlags.All
            );

            PlayhousePlugin.Instance?.Runtime?.Schedule(0.5f, () =>
            {
                if (!PlayerFinder.IsConnected(player))
                    return;

                player.ClearInventory();
                player.AddItem(ItemType.GunLogicer);

                player.MaxHealth = 15000f;
                player.Health = 15000f;

                player.EnableEffect<Scp207>(4, 0);
                player.Position = new Vector3(-17f, 994f, -58f);
                player.Scale = Vector3.one * 2f;
            });

            changed++;
        }

        if (changed == 0)
        {
            response = notFound.Count > 0
                ? $"No players found: {string.Join(", ", notFound)}"
                : "No players were changed.";

            return false;
        }

        response = $"Created {changed} mini-boss(es).";

        if (notFound.Count > 0)
            response += $" Not found: {string.Join(", ", notFound)}.";

        return true;
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ScpSwapCommand : ICommand
{
    private static readonly Dictionary<int, SwapRequest> RequestsBySource = new();
    private static readonly Dictionary<int, SwapRequest> RequestsByTarget = new();

    private static readonly Dictionary<string, RoleTypeId> Roles =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["173"] = RoleTypeId.Scp173,
            ["peanut"] = RoleTypeId.Scp173,

            ["939"] = RoleTypeId.Scp939,
            ["dog"] = RoleTypeId.Scp939,

            ["079"] = RoleTypeId.Scp079,
            ["computer"] = RoleTypeId.Scp079,

            ["106"] = RoleTypeId.Scp106,
            ["larry"] = RoleTypeId.Scp106,

            ["096"] = RoleTypeId.Scp096,
            ["shyguy"] = RoleTypeId.Scp096,

            ["049"] = RoleTypeId.Scp049,
            ["doctor"] = RoleTypeId.Scp049,
        };

    public string Command => "scpswap";

    public string[] Aliases => Array.Empty<string>();

    public string Description => "Requests a role swap with another SCP.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        Player? source = Player.Get(sender);

        if (source is null ||
            source.Team != Team.SCPs ||
            source.Role == RoleTypeId.Scp0492)
        {
            response = "Only non-zombie SCPs can use this command.";
            return false;
        }

        if (PlayhousePlugin.Instance?.State.ScpSwapAllowed != true)
        {
            response =
                "SCP swaps are only available during the first 120 seconds of the round.";

            return false;
        }

        if (arguments.Count != 1)
        {
            response = "Usage: scpswap <scp number|yes|no|cancel>";
            return false;
        }

        string action = arguments.At(0);

        if (action.Equals("yes", StringComparison.OrdinalIgnoreCase))
            return Respond(source, true, out response);

        if (action.Equals("no", StringComparison.OrdinalIgnoreCase))
            return Respond(source, false, out response);

        if (action.Equals("cancel", StringComparison.OrdinalIgnoreCase))
        {
            if (!RequestsBySource.TryGetValue(
                    source.PlayerId,
                    out SwapRequest request))
            {
                response = "No outgoing request.";
                return false;
            }

            Remove(request);
            response = "Swap request cancelled.";
            return true;
        }

        if (!Roles.TryGetValue(action, out RoleTypeId role))
        {
            response = "Invalid SCP.";
            return false;
        }

        if (RequestsBySource.ContainsKey(source.PlayerId))
        {
            response = "You already have a pending request.";
            return false;
        }

        if (RequestsByTarget.ContainsKey(source.PlayerId))
        {
            response = "You already have an incoming request.";
            return false;
        }

        Player? target = Player.ReadyList.FirstOrDefault(
            player =>
                player.Role == role &&
                player != source
        );

        if (target is null)
        {
            response = "No player currently has that SCP role.";
            return false;
        }

        if (RequestsBySource.ContainsKey(target.PlayerId) ||
            RequestsByTarget.ContainsKey(target.PlayerId))
        {
            response = "That player already has a pending swap request.";
            return false;
        }

        SwapRequest created = new(source, target);

        RequestsBySource[source.PlayerId] = created;
        RequestsByTarget[target.PlayerId] = created;

        created.Timeout =
            PlayhousePlugin.Instance?.Runtime?.Schedule(
                120f,
                () => Remove(created)
            );

        target.SendBroadcast(
            $"<i>{source.Nickname} requested an SCP swap. " +
            "Use .scpswap yes or .scpswap no.</i>",
            8
        );

        response = "Swap request sent.";
        return true;
    }

    private static bool Respond(
        Player target,
        bool accept,
        out string response)
    {
        if (!RequestsByTarget.TryGetValue(
                target.PlayerId,
                out SwapRequest request))
        {
            response = "No incoming request.";
            return false;
        }

        Remove(request);

        if (!accept)
        {
            response = "Swap request denied.";

            if (PlayerFinder.IsConnected(request.Source))
            {
                request.Source.SendConsoleMessage(
                    "Your SCP swap was denied.",
                    "red"
                );
            }

            return true;
        }

        if (!PlayerFinder.IsConnected(request.Source) ||
            !PlayerFinder.IsConnected(target) ||
            !request.Source.IsAlive ||
            !target.IsAlive)
        {
            response =
                "Swap cancelled because a player disconnected or is no longer alive.";

            return false;
        }

        RoleTypeId sourceRole = request.Source.Role;
        RoleTypeId targetRole = target.Role;

        float sourceHealth = request.Source.Health;
        float targetHealth = target.Health;

        request.Source.SetRole(
            targetRole,
            RoleChangeReason.RemoteAdmin,
            RoleSpawnFlags.All
        );

        target.SetRole(
            sourceRole,
            RoleChangeReason.RemoteAdmin,
            RoleSpawnFlags.All
        );

        PlayhousePlugin.Instance?.Runtime?.Schedule(0.5f, () =>
        {
            if (!PlayerFinder.IsConnected(request.Source) || !PlayerFinder.IsConnected(target))
                return;

            request.Source.Health = Math.Min(
                request.Source.MaxHealth,
                targetHealth
            );

            target.Health = Math.Min(
                target.MaxHealth,
                sourceHealth
            );
        });

        response = "Swap successful.";
        return true;
    }

    public static void ClearAll()
    {
        foreach (SwapRequest request in RequestsBySource.Values.ToArray())
            Remove(request);
    }

    public static void ClearFor(Player player)
    {
        if (RequestsBySource.TryGetValue(
                player.PlayerId,
                out SwapRequest outgoing))
        {
            Remove(outgoing);
        }

        if (RequestsByTarget.TryGetValue(
                player.PlayerId,
                out SwapRequest incoming))
        {
            Remove(incoming);
        }
    }

    private static void Remove(SwapRequest request)
    {
        RequestsBySource.Remove(request.Source.PlayerId);
        RequestsByTarget.Remove(request.Target.PlayerId);
        request.Timeout?.Dispose();
    }

    private sealed class SwapRequest
    {
        public SwapRequest(Player source, Player target)
        {
            Source = source;
            Target = target;
        }

        public Player Source { get; }

        public Player Target { get; }

        public ScheduledHandle? Timeout { get; set; }
    }
}