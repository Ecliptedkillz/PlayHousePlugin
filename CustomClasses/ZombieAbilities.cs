using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class InfectiousSneezeAbility : CooldownAbilityBase
{
    public InfectiousSneezeAbility(Player player) : base(player)
    {
    }

    public override string Name => "Infectious Sneeze";

    public override double CooldownSeconds => 30;

    protected override bool UseCooldownAbility(out string response)
    {
        int infected = 0;

        foreach (Player target in Player.ReadyList)
        {
            if (target == Player ||
                !target.IsAlive ||
                target.Team == Team.SCPs ||
                Vector3.Distance(target.Position, Player.Position) > 4f)
            {
                continue;
            }

            if (InfectionService.Infect(target))
                infected++;
        }

        Player.SendHint(
            "<size=32><b><color=yellow>ACHOO!</color></b></size>",
            3f);

        response = $"Infected {infected} nearby player(s).";
        return true;
    }
}

public sealed class DrugDoseAbility : CooldownAbilityBase
{
    public DrugDoseAbility(Player player) : base(player)
    {
    }

    public override string Name => "Overdose";

    public override double CooldownSeconds => 30;

    protected override bool UseCooldownAbility(out string response)
    {
        Player.ArtificialHealth = System.Math.Min(
            100f,
            Player.ArtificialHealth + 75f);

        response = "Injected 75 artificial health.";

        Player.SendHint(
            $"<size=28><b><color=#4DE6FF>{response}</color></b></size>",
            2f);

        return true;
    }
}

public sealed class OverclockToggleAbility : NonCooldownAbilityBase
{
    public OverclockToggleAbility(Player player) : base(player)
    {
    }

    public override string Name => "Overclock Toggle";

    public bool Enabled { get; private set; }

    protected override bool UseAbility(out string response)
    {
        Enabled = !Enabled;

        if (Enabled)
        {
            Player.EnableEffect<Hemorrhage>();
            Player.EnableEffect<Scp207>(2);
        }
        else
        {
            Player.DisableEffect<Hemorrhage>();
            Player.DisableEffect<Scp207>();
        }

        response = $"Overclock is now {(Enabled ? "ON" : "OFF")}.";

        string color = Enabled ? "green" : "red";

        Player.SendHint(
            $"<size=28><b><color={color}>{response}</color></b></size>",
            2f);

        return true;
    }
}

public sealed class ZombieReviveAbility : CooldownAbilityBase
{
    private readonly Runtime.PluginRuntime runtime;

    public ZombieReviveAbility(
        Player player,
        Runtime.PluginRuntime runtime)
        : base(player)
    {
        this.runtime = runtime;
    }

    public override string Name => "Zombie Revive";

    public override double CooldownSeconds => 30;

    protected override bool UseCooldownAbility(out string response)
    {
        Ragdoll? ragdoll = Ragdoll.List
            .Where(ragdoll =>
                !ragdoll.IsDestroyed &&
                ragdoll.Role == RoleTypeId.Scp0492 &&
                Vector3.Distance(
                    ragdoll.Position,
                    Player.Position) <= 3f)
            .OrderBy(ragdoll =>
                Vector3.Distance(
                    ragdoll.Position,
                    Player.Position))
            .FirstOrDefault();

        if (ragdoll is null)
        {
            response = "There are no cured zombie bodies nearby.";

            Player.SendHint(
                $"<size=26><b><color=red>{response}</color></b></size>",
                2.5f);

            return false;
        }

        Player? patient = Player.Get(ragdoll.Base.Info.OwnerHub);

        if (patient is null || patient.IsAlive)
        {
            response = "This body cannot be revived.";

            Player.SendHint(
                $"<size=26><b><color=red>{response}</color></b></size>",
                2.5f);

            return false;
        }

        Vector3 revivePosition = Player.Position;
        string patientName = patient.Nickname;

        patient.SetRole(RoleTypeId.Scp0492);
        ragdoll.Destroy();

        runtime.Schedule(0.75f, () =>
        {
            if (!Player.ReadyList.Contains(patient))
            return;

            patient.Position = revivePosition + Vector3.up * 1.6f;
            patient.ArtificialHealth = 70f;

            PlayhousePlugin.Instance?.CustomClasses.Assign(
                patient,
                new ZombieClass(
                    patient,
                    runtime,
                    ZombieArchetype.Overclocker));

            patient.SendHint(
                "<size=32><b>You have been revived by a\n" +
                "<color=red>Medical Student Zombie</color>!</b></size>",
                7f);
        });

        response = $"Revived {patientName}.";

        Player.SendHint(
            $"<size=26><b><color=green>{response}</color></b></size>",
            2.5f);

        return true;
    }
}