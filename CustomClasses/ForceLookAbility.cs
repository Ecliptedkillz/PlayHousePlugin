using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayhousePlugin.Runtime;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ForceLookAbility : AbilityBase, IDisposable
{
    // Ability settings
    private const float Range = 8f;
    private const float Duration = 2.5f;
    private const float Cooldown = 45f;
    private const float UpdateInterval = 0.15f;

    // Controls how strongly the player's view is pulled toward SCP-096.
    //
    // 0.05 = very weak
    // 0.10 = easy to resist
    // 0.20 = medium
    // 0.30 = strong
    // 0.50 = very strong
    // 1.00 = instant lock
    private const float ForceStrength = 0.10f;

    private readonly PluginRuntime runtime;

    private ScheduledHandle? activeForceLook;
    private List<Player> targets = new();

    private float nextUseTime;
    private float forceLookEndTime;
    private bool disposed;

    public ForceLookAbility(
        Player player,
        PluginRuntime runtime)
        : base(player)
    {
        this.runtime = runtime;
    }

    public override string Name => "Force Look";

    public override bool Use(out string response)
    {
        if (disposed)
        {
            response = "Force Look is unavailable.";
            return false;
        }

        if (!Player.ReadyList.Contains(Player) ||
            !Player.IsAlive ||
            Player.Role != RoleTypeId.Scp096)
        {
            response = "You must be alive and playing as SCP-096.";
            return false;
        }

        float now = Time.realtimeSinceStartup;

        if (now < nextUseTime)
        {
            int remaining = Mathf.CeilToInt(nextUseTime - now);

            response =
                $"Force Look is on cooldown for {remaining} second(s).";

            return false;
        }

        targets = Player.ReadyList
            .Where(IsValidTarget)
            .ToList();

        if (targets.Count == 0)
        {
            response =
                $"No human targets are within {Range:0} metres.";

            return false;
        }

        nextUseTime = now + Cooldown;
        forceLookEndTime = now + Duration;

        activeForceLook?.Dispose();

        activeForceLook = runtime.Repeat(
            UpdateInterval,
            UpdateForceLook);

        // Run once immediately instead of waiting for the first interval.
        UpdateForceLook();

        response =
            $"Pulled {targets.Count} player(s) toward your face.";

        return true;
    }

    public override string GenerateHud()
    {
        float remaining =
            nextUseTime - Time.realtimeSinceStartup;

        if (remaining <= 0f)
            return $"Selected: {Name} (Ready)";

        return
            $"Selected: {Name} ({Mathf.CeilToInt(remaining)}s cooldown)";
    }

    private void UpdateForceLook()
    {
        if (disposed ||
            !Player.ReadyList.Contains(Player) ||
            !Player.IsAlive ||
            Player.Role != RoleTypeId.Scp096 ||
            Time.realtimeSinceStartup >= forceLookEndTime)
        {
            StopForceLook();
            return;
        }

        Vector3 facePosition =
            Player.Position + Vector3.up * 1.7f;

        foreach (Player target in targets.ToList())
        {
            if (!IsValidTarget(target))
                continue;

            Vector3 eyePosition =
                target.Position + Vector3.up * 1.6f;

            Vector3 direction =
                facePosition - eyePosition;

            if (direction.sqrMagnitude <= 0.001f)
                continue;

            Quaternion desiredRotation =
                Quaternion.LookRotation(direction.normalized);

            Vector3 desiredEuler =
                desiredRotation.eulerAngles;

            float desiredVertical =
                NormalizeAngle(desiredEuler.x);

            float desiredHorizontal =
                NormalizeAngle(desiredEuler.y);

            Vector2 current =
                target.LookRotation;

            Vector2 desired =
                new(desiredVertical, desiredHorizontal);

            target.LookRotation = new Vector2(
                Mathf.LerpAngle(
                    current.x,
                    desired.x,
                    ForceStrength),
                Mathf.LerpAngle(
                    current.y,
                    desired.y,
                    ForceStrength));
        }
    }

    private bool IsValidTarget(Player target)
    {
        if (target == Player ||
            !Player.ReadyList.Contains(target) ||
            !target.IsAlive)
        {
            return false;
        }

        if (target.Role.GetTeam() == Team.SCPs)
            return false;

        float distanceSquared =
            (target.Position - Player.Position).sqrMagnitude;

        return distanceSquared <= Range * Range;
    }

    private void StopForceLook()
    {
        activeForceLook?.Dispose();
        activeForceLook = null;

        targets.Clear();
    }

    private static float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;
        StopForceLook();
    }
}