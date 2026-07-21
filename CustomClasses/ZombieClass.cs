using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public enum ZombieArchetype { Boomer, MedicalStudent, Overclocker, Overdoser, Sprinter }

public sealed class ZombieClass : CustomClassBase
{
    private readonly ZombieArchetype archetype;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public ZombieClass(Player player, Runtime.PluginRuntime runtime, ZombieArchetype archetype) : base(player)
    {
        this.archetype = archetype;
        switch (archetype)
        {
            case ZombieArchetype.Boomer:
                abilities = new AbilityBase[] { new InfectiousSneezeAbility(player) };
                player.MaxHealth = 600; player.Health = 600;
                player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
                player.AddItem(ItemType.GrenadeHE);
                break;
            case ZombieArchetype.MedicalStudent:
                abilities = new AbilityBase[] { new ZombieReviveAbility(player, runtime) };
                player.MaxHealth = 550; player.Health = 550;
                player.AddItem(ItemType.Medkit);
                Track(runtime.Repeat(1f, HealNearbyZombies));
                break;
            case ZombieArchetype.Overclocker:
                abilities = new AbilityBase[] { new OverclockToggleAbility(player) };
                player.AddItem(ItemType.Painkillers);
                break;
            case ZombieArchetype.Overdoser:
                abilities = new AbilityBase[] { new DrugDoseAbility(player) };
                player.MaxHealth = 525; player.Health = 525;
                player.Scale = new Vector3(0.8f, 1f, 0.8f);
                player.AddItem(ItemType.Adrenaline);
                Track(runtime.Repeat(1f, OverhealNearbyScps));
                break;
            default:
                abilities = Array.Empty<AbilityBase>();
                player.MaxHealth = 275; player.Health = 275;
                player.Scale = new Vector3(0.8f, 0.8f, 0.8f);
                player.AddItem(ItemType.SCP207);
                player.EnableEffect<Scp207>(2);
                break;
        }
        player.CustomInfo = $"{DisplayName}\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color=red>{DisplayName}</color>!</i></b></size>", 10);
        player.SendConsoleMessage($"Name: {Name}\n\nBind .activateability and .changeability to use active abilities.", "yellow");
    }
    private string DisplayName => archetype switch
    {
        ZombieArchetype.MedicalStudent => "Medical Student",
        _ => archetype.ToString(),
    };
    public override string Name => $"Zombie {DisplayName}";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;

    private void HealNearbyZombies()
    {
        if (!Player.IsAlive) return;
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (target == Player || target.Role != RoleTypeId.Scp0492 || Vector3.Distance(target.Position, Player.Position) > 5f) continue;
            target.Health = Math.Min(target.MaxHealth, target.Health + 30);
            target.SendHint("<color=red>+HP</color>", 1f);
        }
    }
    private void OverhealNearbyScps()
    {
        if (!Player.IsAlive) return;
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (target == Player || target.Team != Team.SCPs || target.Role == RoleTypeId.Scp106 || Vector3.Distance(target.Position, Player.Position) > 5f) continue;
            target.ArtificialHealth = Math.Min(100, target.ArtificialHealth + 5);
            target.SendHint("<color=red>+AHP</color>", 1f);
        }
    }
    public override void Dispose()
    {
        Player.Scale = Vector3.one;
        if (archetype is ZombieArchetype.Overclocker or ZombieArchetype.Sprinter)
        {
            Player.DisableEffect<Scp207>();
            Player.DisableEffect<Hemorrhage>();
        }
        base.Dispose();
    }
}
