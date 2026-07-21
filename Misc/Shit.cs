using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using MapGeneration;
using Mirror;
using RemoteAdmin;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ShitCommand : ICommand
{
    public string Command => "shit";

    public string[] Aliases => Array.Empty<string>();

    public string Description =>
        "Makes you shit yourself if you're in the LCZ bathroom.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (sender is not PlayerCommandSender playerSender)
        {
            response = "This command can only be run by a player.";
            return false;
        }

        Player? player = Player.Get(playerSender.ReferenceHub);

        if (player is null)
        {
            response = "Could not find your player.";
            return false;
        }

        if (player.Room is null ||
            player.Room.Name != RoomName.LczToilets)
        {
            response = "You're not in the right place to shit yourself.";
            return false;
        }

        NetworkManager? networkManager = NetworkManager.singleton;

        if (networkManager is null)
        {
            response = "The network manager is unavailable.";
            return false;
        }

        GameObject? tantrumPrefab = networkManager.spawnPrefabs
            .FirstOrDefault(prefab =>
                prefab != null &&
                prefab.name.Equals(
                    "TantrumObj",
                    StringComparison.OrdinalIgnoreCase));

        if (tantrumPrefab is null)
        {
            response = "Could not find the TantrumObj prefab.";
            return false;
        }

        GameObject tantrumObject =
            UnityEngine.Object.Instantiate(tantrumPrefab);

        tantrumObject.transform.SetPositionAndRotation(
            player.Position,
            Quaternion.identity);

        NetworkServer.Spawn(tantrumObject);

        response = "Shitting time.";
        return true;
    }
}