using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayhousePlugin.Runtime;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class SCP096CustomClass : CustomClassBase
{
    private const float HealingRange = 7f;
    private const float HealingAmount = 7f;
    private const float HealingInterval = 1f;

    private readonly IReadOnlyList<AbilityBase> abilities;

    public SCP096CustomClass(
        Player player,
        PluginRuntime runtime)
        : base(player)
    {
        abilities = new AbilityBase[]
        {
            new ForceLookAbility(player, runtime)
        };

        player.CustomInfo = "SCP-096\n(Custom Class)";

        Track(runtime.Repeat(
            HealingInterval,
            ApplyHealingAura));
    }

    public override string Name => "SCP-096";

    public override IReadOnlyList<AbilityBase> ActiveAbilities =>
        abilities;

    private void ApplyHealingAura()
    {
        if (!Player.ReadyList.Contains(Player) ||
            !Player.IsAlive ||
            Player.Role != RoleTypeId.Scp096)
        {
            return;
        }

        Vector3 ownerPosition = Player.Position;
        float rangeSquared = HealingRange * HealingRange;

        foreach (Player target in Player.ReadyList.ToList())
        {
            if (target == Player ||
                !target.IsAlive ||
                target.Role != RoleTypeId.Scp0492)
            {
                continue;
            }

            float distanceSquared =
                (target.Position - ownerPosition).sqrMagnitude;

            if (distanceSquared > rangeSquared)
                continue;

            target.Health = Mathf.Min(
                target.Health + HealingAmount,
                target.MaxHealth);
        }
    }
}