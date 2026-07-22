using System;
using System.Collections.Generic;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using Mirror;
using Mirror.LiteNetLib4Mirror;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class SpawnMovingTargetCommand : ICommand
{
    private static readonly Dictionary<int, GameObject> ActiveTargets = new();

    public string Command => "targetmove";
    public string[] Aliases => new[] { "movingtarget", "tm" };
    public string Description => "Spawns or removes a moving target.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.targetmove"))
        {
            response = "Missing permission: at.targetmove";
            return false;
        }

        Player? player = Player.Get(sender);

        if (player is null)
        {
            response = "This command can only be used by an in-game player.";
            return false;
        }

        if (ActiveTargets.TryGetValue(
                player.PlayerId,
                out GameObject existingTarget))
        {
            RemoveTarget(player.PlayerId, existingTarget);
            response = "Moving target removed.";
            return true;
        }

        LiteNetLib4MirrorNetworkManager? networkManager =
            LiteNetLib4MirrorNetworkManager.singleton;

        if (networkManager is null)
        {
            response = "The network manager is unavailable.";
            return false;
        }

        GameObject? prefab = networkManager.spawnPrefabs
            .FirstOrDefault(candidate =>
                candidate is not null &&
                string.Equals(
                    candidate.name,
                    "sportTargetPrefab",
                    StringComparison.OrdinalIgnoreCase));

        if (prefab is null)
        {
            response = "Could not find sportTargetPrefab.";
            return false;
        }

        GameObject target = UnityEngine.Object.Instantiate(prefab);

        target.transform.SetPositionAndRotation(
            player.Camera.position + player.Camera.forward * 3f,
            Quaternion.Euler(
                0f,
                player.Camera.rotation.eulerAngles.y,
                0f));

        MovingTargetFollower follower =
            target.AddComponent<MovingTargetFollower>();

        follower.Initialize(
            player.PlayerId,
            OnTargetDestroyed);

        NetworkServer.Spawn(target);
        ActiveTargets[player.PlayerId] = target;

        response = "Moving target spawned. Run targetmove again to remove it.";
        return true;
    }

    private static void OnTargetDestroyed(int playerId)
    {
        ActiveTargets.Remove(playerId);
    }

    private static void RemoveTarget(int playerId, GameObject target)
    {
        ActiveTargets.Remove(playerId);

        if (target is null)
            return;

        if (NetworkServer.active &&
            target.TryGetComponent(out NetworkIdentity identity) &&
            identity.isServer)
        {
            NetworkServer.Destroy(target);
            return;
        }

        UnityEngine.Object.Destroy(target);
    }
}

public sealed class MovingTargetFollower : MonoBehaviour
{
    private int playerId;
    private Action<int>? onDestroyed;

    public void Initialize(
        int ownerPlayerId,
        Action<int> destroyedCallback)
    {
        playerId = ownerPlayerId;
        onDestroyed = destroyedCallback;
    }

    private void Update()
    {
        Player? currentPlayer = Player.List.FirstOrDefault(
            candidate => candidate.PlayerId == playerId);

        if (currentPlayer is null)
        {
            DestroyTarget();
            return;
        }

        Transform camera = currentPlayer.Camera;

        transform.SetPositionAndRotation(
            camera.position + camera.forward * 3f,
            Quaternion.Euler(
                0f,
                camera.rotation.eulerAngles.y,
                0f));
    }

    private void DestroyTarget()
    {
        if (NetworkServer.active &&
            TryGetComponent(out NetworkIdentity identity) &&
            identity.isServer)
        {
            NetworkServer.Destroy(gameObject);
            return;
        }

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        onDestroyed?.Invoke(playerId);
    }
}
