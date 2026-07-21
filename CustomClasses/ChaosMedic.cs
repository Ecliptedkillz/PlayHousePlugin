using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ChaosMedic : CustomClassBase
{
    private readonly IReadOnlyList<AbilityBase> abilities;
    private readonly System.Random random = new();
    public ChaosMedic(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        abilities = new AbilityBase[] { new NtfMedicRevive(player, runtime, RoleTypeId.ChaosConscript, "Chaos Medic", "army_green") };
        player.ClearInventory();
        player.AddItem(ItemType.KeycardChaosInsurgency);
        player.AddItem(ItemType.GunAK);
        player.AddItem(ItemType.Adrenaline);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Medkit);
        player.SetAmmo(ItemType.Ammo762x39, 120);
        player.MaxHealth = 105;
        player.Health = 105;
        player.CustomInfo = "Chaos Medic\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You have spawned as a <color=army_green>Chaos Medic</color>!</i></b></size>", 10);
        Track(runtime.Repeat(1f, HealNearby));
        Track(runtime.Repeat(40f, GenerateMedicalItem));
    }
    public override string Name => "Chaos Medic";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    private void HealNearby()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (target == Player || !target.IsAlive || target.IsDisarmed || Vector3.Distance(target.Position, Player.Position) > 7f) continue;
            if (target.Role is not (RoleTypeId.ChaosConscript or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor or RoleTypeId.ChaosRifleman or RoleTypeId.ClassD)) continue;
            target.Health = Math.Min(target.MaxHealth, target.Health + 5);
            if (target.Health >= target.MaxHealth) target.ArtificialHealth = Math.Min(20, target.ArtificialHealth + 5);
            Player.Health = Math.Min(Player.MaxHealth, Player.Health + 1);
        }
    }
    private void GenerateMedicalItem()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        if (Player.Items.Count() >= 8) { Player.SendHint("<color=yellow>Your inventory is full.</color>", 4f); return; }
        ItemType item = ItemType.Medkit;
        if (random.Next(100) <= 20) { ItemType[] special = { ItemType.Adrenaline, ItemType.SCP500, ItemType.SCP207 }; item = special[random.Next(special.Length)]; }
        Player.AddItem(item);
        Player.SendHint("<color=yellow>Medical Item Generated!</color>", 3f);
    }
}
