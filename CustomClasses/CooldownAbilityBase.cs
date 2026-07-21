using System;
using System.Diagnostics;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public abstract class CooldownAbilityBase : AbilityBase
{
    private readonly Stopwatch cooldownTimer = new();

    protected CooldownAbilityBase(Player player)
        : base(player)
    {
    }

    public abstract double CooldownSeconds { get; }

    public double RemainingSeconds
    {
        get
        {
            if (!cooldownTimer.IsRunning)
                return 0;

            double remaining =
                CooldownSeconds -
                cooldownTimer.Elapsed.TotalSeconds;

            if (remaining > 0)
                return remaining;

            cooldownTimer.Reset();
            return 0;
        }
    }

    public bool IsOnCooldown =>
        RemainingSeconds > 0;

    public override string GenerateHud()
    {
        string status;

        if (IsOnCooldown)
        {
            status =
                $"<color=red>COOLDOWN: " +
                $"{Math.Ceiling(RemainingSeconds):0}s</color>";
        }
        else
        {
            status = "<color=green>READY</color>";
        }

        return
            "<align=right>" +
            "<size=25><b><color=yellow>" +
            "SELECTED ABILITY" +
            "</color></b></size>\n" +
            $"<size=23><b>{Name}</b></size>\n" +
            $"<size=21>{status}</size>" +
            "</align>";
    }

    public override bool Use(out string response)
    {
        if (IsOnCooldown)
        {
            double seconds =
                Math.Ceiling(RemainingSeconds);

            response =
                $"You must wait {seconds:0} " +
                $"{(seconds == 1 ? "second" : "seconds")} " +
                $"before using {Name} again.";

            return false;
        }

        if (!UseCooldownAbility(out response))
            return false;

        cooldownTimer.Restart();
        return true;
    }

    protected abstract bool UseCooldownAbility(
        out string response);

    protected void ResetCooldown()
    {
        cooldownTimer.Reset();
    }

    protected void StartCooldown()
    {
        cooldownTimer.Restart();
    }
}