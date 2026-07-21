using System;
using System.Collections;
using LabLogger = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin;

/// <summary>
/// Makes three pickup objects follow a player.
/// Ported from the old EXILED implementation to Northwood LabAPI.
/// </summary>
public static class Cock
{
    private const float UpdateInterval = 0.05f;
    private const int AfkTicksRequired = 250; // About 12.5 seconds at 0.05 seconds per update.
    private const int MaximumErrors = 5;

    private static readonly System.Random Random = new();

    /// <summary>
    /// Starts the follower coroutine and returns its Unity coroutine handle.
    /// </summary>
    public static Coroutine StartFollowing(
        Player owner,
        Pickup ball1,
        Pickup ball2,
        Pickup body)
    {
        if (owner is null) throw new ArgumentNullException(nameof(owner));
        if (ball1 is null) throw new ArgumentNullException(nameof(ball1));
        if (ball2 is null) throw new ArgumentNullException(nameof(ball2));
        if (body is null) throw new ArgumentNullException(nameof(body));

        return CoroutineRunner.Instance.StartCoroutine(FollowPlayer(owner, ball1, ball2, body));
    }

    /// <summary>
    /// Continuously moves the supplied pickups with the owner.
    /// </summary>
    public static IEnumerator FollowPlayer(
        Player owner,
        Pickup ball1,
        Pickup ball2,
        Pickup body)
    {
        DisablePhysics(ball1);
        DisablePhysics(ball2);
        DisablePhysics(body);

        Vector3 previousPosition = owner.Position;
        float orbitAngle = 0f;
        int afkTicks = 0;
        int errorCount = 0;

        float roomOffsetX = Random.Next(-150, 151) / 100f;
        float roomOffsetZ = Random.Next(-150, 151) / 100f;

        try
        {
            while (IsValid(owner, ball1, ball2, body) && owner.IsAlive)
            {
                yield return new WaitForSeconds(UpdateInterval);

                bool delayAfterError = false;

                try
                {
                    bool isAfk = afkTicks >= AfkTicksRequired;
                    bool isIn914 = owner.Room?.Name == RoomName.Lcz914;

                    CalculatePositions(
                        owner,
                        isAfk,
                        isIn914,
                        roomOffsetX,
                        roomOffsetZ,
                        orbitAngle,
                        out Vector3 ball1Position,
                        out Vector3 ball2Position,
                        out Vector3 bodyPosition,
                        out Quaternion bodyRotation);

                    SetTransform(ball1, ball1Position, Quaternion.identity);
                    SetTransform(ball2, ball2Position, Quaternion.identity);
                    SetTransform(body, bodyPosition, bodyRotation);

                    if (isAfk)
                        orbitAngle += 0.08f;

                    if (Vector3.Distance(previousPosition, owner.Position) > 0.1f)
                    {
                        previousPosition = owner.Position;
                        afkTicks = 0;
                        orbitAngle = 0f;
                    }
                    else
                    {
                        afkTicks++;
                    }

                    errorCount = 0;
                }
                catch (Exception exception)
                {
                    errorCount++;
                    LabLogger.Warn($"Follower update failed for {owner.Nickname}: {exception.Message}");

                    if (errorCount > MaximumErrors)
                    {
                        owner.SendBroadcast("<i>Psst, your pet lost you! Equip it again.</i>", 6);
                        break;
                    }

                    delayAfterError = true;
                }

                if (delayAfterError)
                    yield return new WaitForSeconds(1f);
            }
        }
        finally
        {
            DestroySafely(ball1);
            DestroySafely(ball2);
            DestroySafely(body);
        }
    }

    private static void CalculatePositions(
        Player owner,
        bool isAfk,
        bool isIn914,
        float roomOffsetX,
        float roomOffsetZ,
        float orbitAngle,
        out Vector3 ball1Position,
        out Vector3 ball2Position,
        out Vector3 bodyPosition,
        out Quaternion bodyRotation)
    {
        Transform playerTransform = owner.GameObject.transform;
        Vector3 forward = playerTransform.forward;
        Vector3 right = playerTransform.right;
        Vector3 ownerPosition = owner.Position;

        forward.y = 0f;
        right.y = 0f;

        if (forward.sqrMagnitude > 0.001f)
            forward.Normalize();

        if (right.sqrMagnitude > 0.001f)
            right.Normalize();

        if (isAfk)
        {
            Vector3 orbitCenter = isIn914 && owner.Room is not null
                ? owner.Room.Position + new Vector3(roomOffsetX, 1.3f, roomOffsetZ)
                : ownerPosition + Vector3.up * 0.8f;

            Vector3 orbitOffset = new(
                Mathf.Cos(orbitAngle) * 0.55f,
                Mathf.Sin(orbitAngle * 2f) * 0.15f,
                Mathf.Sin(orbitAngle) * 0.55f);

            bodyPosition = orbitCenter + orbitOffset;
            ball1Position = bodyPosition - right * 0.08f - Vector3.down * 0.08f;
            ball2Position = bodyPosition + right * 0.08f - Vector3.down * 0.08f;
            bodyRotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            return;
        }

        if (isIn914 && owner.Room is not null)
        {
            bodyPosition = owner.Room.Position + new Vector3(roomOffsetX, 0.5f, roomOffsetZ);
            ball1Position = bodyPosition - right * 0.08f;
            ball2Position = bodyPosition + right * 0.08f;
            bodyRotation = Quaternion.Euler(90f, ownerTransformYaw(owner), 0f);
            return;
        }

        bool isScp939 = owner.Role == RoleTypeId.Scp939;

        if (isScp939)
        {
            bodyPosition = ownerPosition - forward * 0.70f + Vector3.down * 0.60f;
            ball1Position = ownerPosition - forward * 0.90f - right * 0.07f + Vector3.down * 0.70f;
            ball2Position = ownerPosition - forward * 0.90f + right * 0.07f + Vector3.down * 0.70f;
        }
        else
        {
            bodyPosition = ownerPosition + forward * 0.40f + Vector3.down * 0.10f;
            ball1Position = ownerPosition + forward * 0.17f - right * 0.07f + Vector3.down * 0.25f;
            ball2Position = ownerPosition + forward * 0.17f + right * 0.07f + Vector3.down * 0.25f;
        }

        bodyRotation = Quaternion.LookRotation(forward, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
    }

    private static float ownerTransformYaw(Player owner) => owner.GameObject.transform.eulerAngles.y;

    private static void SetTransform(Pickup pickup, Vector3 position, Quaternion rotation)
    {
        pickup.Position = position;
        pickup.Rotation = rotation;

        Rigidbody? rigidbody = pickup.GameObject.GetComponent<Rigidbody>();
        if (rigidbody is not null)
        {
            rigidbody.position = position;
            rigidbody.rotation = rotation;
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private static void DisablePhysics(Pickup pickup)
    {
        foreach (Collider collider in pickup.GameObject.GetComponentsInChildren<Collider>(true))
            collider.enabled = false;

        Rigidbody? rigidbody = pickup.GameObject.GetComponent<Rigidbody>();
        if (rigidbody is null)
            return;

        rigidbody.useGravity = false;
        rigidbody.detectCollisions = false;
        rigidbody.isKinematic = true;
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
    }

    private static bool IsValid(Player owner, params Pickup[] pickups)
    {
        if (owner is null || owner.GameObject is null)
            return false;

        foreach (Pickup pickup in pickups)
        {
            if (pickup is null || pickup.GameObject is null)
                return false;
        }

        return true;
    }

    private sealed class CoroutineRunner : MonoBehaviour
    {
        private static CoroutineRunner? instance;

        public static CoroutineRunner Instance
        {
            get
            {
                if (instance is not null)
                    return instance;

                GameObject runnerObject = new("PlayhousePlugin.CockCoroutineRunner");
                UnityEngine.Object.DontDestroyOnLoad(runnerObject);
                instance = runnerObject.AddComponent<CoroutineRunner>();
                return instance;
            }
        }
    }

    private static void DestroySafely(Pickup pickup)
    {
        try
        {
            if (pickup?.GameObject is not null)
                pickup.Destroy();
        }
        catch (Exception exception)
        {
            LabLogger.Debug($"Could not destroy follower pickup: {exception.Message}");
        }
    }
}