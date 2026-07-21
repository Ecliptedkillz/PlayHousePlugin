using System;
using System.Linq;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class DemolitionsClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly bool expert;
    private readonly System.Random random = new();
    public DemolitionsClass(Player player, Runtime.PluginRuntime runtime, bool chaos, bool? expert = null) : base(player)
    {
        this.chaos = chaos;
        this.expert = expert ?? chaos;
        player.ClearInventory();
        if (chaos)
        {
            player.AddItem(ItemType.GunAK);
            player.AddItem(ItemType.KeycardChaosInsurgency);
            player.AddItem(ItemType.Medkit);
            player.AddItem(this.expert ? ItemType.Medkit : ItemType.Adrenaline);
            player.SetAmmo(ItemType.Ammo762x39, 120);
            if (this.expert) { player.MaxHealth = 150; player.Health = 150; }
        }
        else
        {
            player.AddItem(ItemType.KeycardMTFOperative);
            player.AddItem(ItemType.GunCrossvec);
            player.AddItem(ItemType.Medkit);
            player.AddItem(ItemType.Radio);
            player.SetAmmo(ItemType.Ammo9x19, (ushort)(this.expert ? 120 : 160));
            if (this.expert) { player.MaxHealth = 150; player.Health = 150; }
        }
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.GrenadeHE);
        player.AddItem(ItemType.GrenadeHE);
        player.AddItem(ItemType.GrenadeHE);
        player.CustomInfo = $"{(this.expert ? "Demolitions Expert" : chaos ? "Chaos Demoman" : "Demoman")}\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{Name}</color>!</i></b></size>", 10);
        Track(runtime.Repeat(60f, GenerateExplosive));
    }
    public override string Name => $"{(chaos ? "Chaos" : "NTF")} {(expert ? "Demolitions Expert" : "Demoman")}";
    private void GenerateExplosive()
    {
        if (!Player.IsAlive || Player.IsDisarmed) return;
        if (Player.Items.Count() >= 8) { Player.SendHint("<color=yellow>Your inventory is full.</color>", 4f); return; }
        ItemType item = ItemType.GrenadeHE;
        if (random.Next(100) <= 20) { ItemType[] special = { ItemType.GrenadeFlash, ItemType.SCP018, ItemType.SCP2176 }; item = special[random.Next(special.Length)]; }
        Player.AddItem(item);
        Player.SendHint($"<color=yellow>{(item == ItemType.GrenadeHE ? "Explosive" : "Special")} Item Generated!</color>", 3f);
    }
}
