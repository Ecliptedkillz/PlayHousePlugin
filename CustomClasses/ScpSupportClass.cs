using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ScpSupportClass : CustomClassBase
{
    private readonly string name;
    private readonly bool healOwner;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public ScpSupportClass(Player player, Runtime.PluginRuntime runtime, RoleTypeId role) : base(player)
    {
        name = role switch
        {
            RoleTypeId.Scp049 => "SCP-049",
            RoleTypeId.Scp096 => "SCP-096",
            RoleTypeId.Scp173 => "SCP-173",
            RoleTypeId.Scp939 => "SCP-939",
            _ => role.ToString(),
        };
        healOwner = role == RoleTypeId.Scp049;
        abilities = healOwner ? new AbilityBase[] { new ScpHealAbility(player) } : Array.Empty<AbilityBase>();
        Track(runtime.Repeat(1f, HealNearbyZombies));
        if (healOwner)
        {
            player.SendBroadcast("<size=40><b><i>You have spawned as <color=red>SCP-049</color> with special abilities.</i></b></size>", 10);
            player.SendConsoleMessage("SCP-049: nearby zombies heal with you; SCP Heal transfers your health to another SCP.", "yellow");
        }
    }
    public override string Name => name;
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    private void HealNearbyZombies()
    {
        if (!Player.IsAlive) return;
        foreach (Player zombie in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (zombie == Player || zombie.Role != RoleTypeId.Scp0492 || Vector3.Distance(zombie.Position, Player.Position) > 7f) continue;
            zombie.Health = Math.Min(zombie.MaxHealth, zombie.Health + 7);
            if (healOwner) Player.Health = Math.Min(Player.MaxHealth, Player.Health + 5);
        }
    }
}
