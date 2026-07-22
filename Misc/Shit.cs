using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MapGeneration;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using RemoteAdmin;
using UnityEngine;
using LabLogger = LabApi.Features.Console.Logger;

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

        Room? room = player.Room;

        if (room is null || room.Name != RoomName.LczToilets)
        {
            response = room is null
                ? "Your current room could not be detected."
                : $"You're not in the LCZ bathroom. Current room: {room.Name}";

            return false;
        }

        LiteNetLib4MirrorNetworkManager? networkManager =
            LiteNetLib4MirrorNetworkManager.singleton;

        if (networkManager is null)
        {
            response = "The SCP:SL network manager is unavailable.";
            return false;
        }

        GameObject? tantrumPrefab = networkManager.spawnPrefabs
            .FirstOrDefault(prefab =>
                prefab is not null &&
                prefab.name.Equals(
                    "TantrumObj",
                    StringComparison.OrdinalIgnoreCase));

        if (tantrumPrefab is null)
           {
        string prefabs = string.Join(
            ", ",
            networkManager.spawnPrefabs
                .Where(prefab => prefab is not null)
                .Select(prefab => prefab.name)
                .Where(name =>
                    !string.IsNullOrWhiteSpace(name) &&
                    name.IndexOf(
                        "Tantrum",
                        StringComparison.OrdinalIgnoreCase) >= 0));
    
        response = string.IsNullOrWhiteSpace(prefabs)
            ? "Could not find the TantrumObj prefab."
            : $"TantrumObj was not found. Similar prefabs: {prefabs}";
    
        return false;
    }

        try
        {
            GameObject tantrumObject =
                UnityEngine.Object.Instantiate(tantrumPrefab);

            Vector3 spawnPosition =
                player.Position + Vector3.up * 0.15f;

            tantrumObject.transform.SetPositionAndRotation(
                spawnPosition,
                Quaternion.Euler(0f, player.Rotation.y, 0f));

            NetworkServer.Spawn(tantrumObject);

            LabLogger.Info(
                $"Spawned TantrumObj for {player.Nickname} " +
                $"in {room.Name} at {spawnPosition}.");

            response = "Shitting time.";
            return true;
        }
        catch (Exception exception)
        {
            LabLogger.Error($"Failed to spawn TantrumObj:\n{exception}");

            response = $"Failed to spawn it: {exception.Message}";
            return false;
        }
    }
}