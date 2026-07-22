using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class NtfHeavy : CustomClassBase
{
    private const ushort AmmoPerTick = 20;

    private static readonly Dictionary<ItemType, ushort> AmmoLimits = new()
    {
        [ItemType.Ammo9x19] = 200,
        [ItemType.Ammo556x45] = 160,
        [ItemType.Ammo762x39] = 100,
        [ItemType.Ammo44cal] = 48,
        [ItemType.Ammo12gauge] = 54,
    };

    public NtfHeavy(Player player, Runtime.PluginRuntime runtime)
        : base(player)
    {
        player.ClearInventory();

        player.AddItem(ItemType.GunLogicer);
        player.AddItem(ItemType.GunE11SR);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Radio);
        player.AddItem(ItemType.KeycardMTFOperative);
        player.AddItem(ItemType.ArmorHeavy);

        player.SetAmmo(ItemType.Ammo556x45, 200);
        player.SetAmmo(ItemType.Ammo762x39, 200);

        player.MaxHealth = 200;
        player.Health = 200;
        player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
        player.CustomInfo = "Heavy\n(Custom Class)";

        player.SendBroadcast(
            "<size=40><b><i>You have spawned as an " +
            "<color=navy>NTF Heavy</color>!</i></b></size>",
            10);

        player.SendConsoleMessage(
            "Name: NTF Heavy\n\n" +
            "200 HP, larger size, heavy weapons, and a nearby ammunition aura.",
            "yellow");

        Track(runtime.Repeat(1f, ReplenishNearbyAmmo));
    }

    public override string Name => "NTF Heavy";

    private void ReplenishNearbyAmmo()
    {
        if (!Player.IsAlive)
            return;

        foreach (Player target in Player.ReadyList)
        {
            if (target == Player ||
                !target.IsAlive ||
                Vector3.Distance(target.Position, Player.Position) > 7f)
            {
                continue;
            }

            RoleTypeId role = target.Role;

            if (role is not (
                RoleTypeId.NtfCaptain or
                RoleTypeId.NtfSergeant or
                RoleTypeId.NtfSpecialist or
                RoleTypeId.NtfPrivate or
                RoleTypeId.FacilityGuard or
                RoleTypeId.Scientist))
            {
                continue;
            }

            IEnumerable<ItemType> requiredAmmo = target.Items
                .OfType<FirearmItem>()
                .Select(firearm => firearm.AmmoType)
                .Distinct();

            foreach (ItemType ammoType in requiredAmmo)
            {
                GiveAmmoWithoutOverflow(target, ammoType);
            }
        }
    }

    private static void GiveAmmoWithoutOverflow(
        Player target,
        ItemType ammoType)
    {
        if (!AmmoLimits.TryGetValue(ammoType, out ushort maximumAmmo))
            return;

        ushort currentAmmo = target.GetAmmo(ammoType);

        if (currentAmmo >= maximumAmmo)
            return;

        ushort availableSpace = (ushort)(maximumAmmo - currentAmmo);
        ushort amountToGive = (ushort)Mathf.Min(AmmoPerTick, availableSpace);

        target.AddAmmo(ammoType, amountToGive);
    }

    public override void Dispose()
    {
        Player.Scale = Vector3.one;
        base.Dispose();
    }
}