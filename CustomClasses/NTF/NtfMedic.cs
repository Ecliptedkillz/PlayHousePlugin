using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using System.Linq;

namespace PlayhousePlugin.CustomClasses;

public sealed class NtfMedic : CustomClassBase
{
    private readonly IReadOnlyList<AbilityBase> abilities;
    private readonly System.Random random = new();
    public NtfMedic(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        abilities = new AbilityBase[] { new NtfMedicRevive(player, runtime) };
        player.ClearInventory();
        player.AddItem(ItemType.KeycardMTFOperative);
        player.AddItem(ItemType.GunFSP9);
        player.AddItem(ItemType.Radio);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Medkit);
        player.SetAmmo(ItemType.Ammo9x19, 120);
        player.MaxHealth = 105;
        player.Health = 105;
        player.CustomInfo = "Medic\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You have spawned as an <color=navy>NTF Medic</color>!</i></b></size>", 10);
        player.SendConsoleMessage("Name: NTF Medic\n\nHealing aura, medical-item generation, and Revive (30s cooldown). Bind .activateability and .changeability.", "yellow");
        Track(runtime.Repeat(1f, HealNearby));
        Track(runtime.Repeat(40f, GenerateMedicalItem));
    }
    public override string Name => "NTF Medic";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;

    private void HealNearby()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
        {
            if (target == Player || !target.IsAlive || target.IsDisarmed || Vector3.Distance(target.Position, Player.Position) > 7f) continue;
            RoleTypeId role = target.Role;
            if (role is not (RoleTypeId.NtfCaptain or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist or RoleTypeId.NtfPrivate or RoleTypeId.FacilityGuard or RoleTypeId.Scientist)) continue;
            float previousHealth = target.Health;
            target.Health = Math.Min(target.MaxHealth, target.Health + 5);
            if (target.Health >= target.MaxHealth) target.ArtificialHealth = Math.Min(20, target.ArtificialHealth + 5);
            Player.Health = Math.Min(Player.MaxHealth, Player.Health + 1);
            if (target.Health > previousHealth) target.SendHint("<color=yellow>Healing Aura</color>", 1f);
        }
    }

    private void GenerateMedicalItem()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        if (Player.Items.Count() >= 8)
        {
            Player.SendHint("<color=yellow>You would have generated an item, but your inventory is full.</color>", 4f);
            return;
        }
        ItemType item = ItemType.Medkit;
        if (random.Next(100) <= 20)
        {
            ItemType[] special = { ItemType.Adrenaline, ItemType.SCP500, ItemType.SCP207 };
            item = special[random.Next(special.Length)];
        }
        Player.AddItem(item);
        Player.SendHint($"<color=yellow>{(item == ItemType.Medkit ? "Medical" : "Special")} Item Generated!</color>", 3f);
    }
}
