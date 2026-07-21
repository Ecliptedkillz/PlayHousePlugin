using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using System.Linq;

namespace PlayhousePlugin.CustomClasses;

public sealed class NtfHeavy : CustomClassBase
{
    public NtfHeavy(Player player, Runtime.PluginRuntime runtime) : base(player)
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
        player.SendBroadcast("<size=40><b><i>You have spawned as an <color=navy>NTF Heavy</color>!</i></b></size>", 10);
        player.SendConsoleMessage("Name: NTF Heavy\n\n200 HP, larger size, heavy weapons, and a nearby ammunition aura.", "yellow");
        Track(runtime.Repeat(1f, ReplenishNearbyAmmo));
    }
    public override string Name => "NTF Heavy";

    private void ReplenishNearbyAmmo()
    {
        if (!Player.IsAlive) return;
        foreach (Player target in Player.ReadyList)
        {
            if (target == Player || !target.IsAlive || Vector3.Distance(target.Position, Player.Position) > 7f) continue;
            RoleTypeId role = target.Role;
            if (role is not (RoleTypeId.NtfCaptain or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist or RoleTypeId.NtfPrivate or RoleTypeId.FacilityGuard or RoleTypeId.Scientist)) continue;
            foreach (ItemType ammoType in target.Items.OfType<FirearmItem>().Select(item => item.AmmoType).Distinct())
                target.AddAmmo(ammoType, 20);
        }
    }

    public override void Dispose()
    {
        Player.Scale = Vector3.one;
        base.Dispose();
    }
}
