using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ChaosHeavy : CustomClassBase
{
    public ChaosHeavy(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        player.ClearInventory();
        player.AddItem(ItemType.GunLogicer);
        player.AddItem(ItemType.GunAK);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Adrenaline);
        player.AddItem(ItemType.KeycardChaosInsurgency);
        player.AddItem(ItemType.ArmorHeavy);
        player.SetAmmo(ItemType.Ammo762x39, 200);
        player.MaxHealth = 200;
        player.Health = 200;
        player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
        player.CustomInfo = "Chaos Heavy\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You have spawned as a <color=green>Chaos Heavy</color>!</i></b></size>", 10);
        Track(runtime.Repeat(1f, ReplenishNearbyAmmo));
    }
    public override string Name => "Chaos Heavy";
    private void ReplenishNearbyAmmo()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (target == Player || !target.IsAlive || target.IsDisarmed || Vector3.Distance(target.Position, Player.Position) > 7f) continue;
            if (target.Role is not (RoleTypeId.ChaosConscript or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor or RoleTypeId.ChaosRifleman or RoleTypeId.ClassD)) continue;
            foreach (ItemType ammoType in target.Items.OfType<FirearmItem>().Select(item => item.AmmoType).Distinct()) target.AddAmmo(ammoType, 20);
        }
    }
    public override void Dispose() { Player.Scale = Vector3.one; base.Dispose(); }
}
