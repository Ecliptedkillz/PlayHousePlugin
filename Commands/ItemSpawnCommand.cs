using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ItemSpawnCommand : ICommand
{
    public string Command => "item";
    public string[] Aliases => new[] { "i" };
    public string Description => "Spawns scaled item pickups at your position.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? caller = Player.Get(sender);
        if (caller is null)
        {
            response = "This command can only be run by a player.";
            return false;
        }
        if (arguments.Count < 1 || !Enum.TryParse(arguments.At(0), true, out ItemType itemType))
        {
            response = "Usage: item <itemType> [count] [x,y,z | x y z]";
            return false;
        }

        int count = 1;
        if (arguments.Count >= 2 && (!int.TryParse(arguments.At(1), out count) || count < 1 || count > 100))
        {
            response = "Count must be between 1 and 100.";
            return false;
        }

        Vector3 scale = Vector3.one;
        if (arguments.Count >= 3)
        {
            string[] parts = arguments.At(2).Split(',');
            if (parts.Length == 3)
            {
                if (!TryVector(parts[0], parts[1], parts[2], out scale))
                { response = "Invalid scale."; return false; }
            }
            else if (arguments.Count == 5)
            {
                if (!TryVector(arguments.At(2), arguments.At(3), arguments.At(4), out scale))
                { response = "Invalid scale."; return false; }
            }
            else
            {
                response = "Usage: item <itemType> [count] [x,y,z | x y z]";
                return false;
            }
        }

        for (int i = 0; i < count; i++)
        {
            Pickup? pickup = Pickup.Create(itemType, caller.Position, Quaternion.identity, scale);
            pickup?.Spawn();
        }
        response = $"Spawned {count} {itemType} pickup(s).";
        return true;
    }

    private static bool TryVector(string sx, string sy, string sz, out Vector3 value)
    {
        value = Vector3.one;
        if (!float.TryParse(sx, out float x) || !float.TryParse(sy, out float y) || !float.TryParse(sz, out float z)) return false;
        value = new Vector3(x, y, z);
        return true;
    }
}
