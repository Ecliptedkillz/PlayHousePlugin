using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ScoutClass : CustomClassBase
{
    private readonly string name;
    public ScoutClass(Player player, bool chaos) : base(player)
    {
        name = chaos ? "Chaos Scout" : "NTF Scout";
        player.ClearInventory();
        player.AddItem(ItemType.GunShotgun);
        player.AddItem(ItemType.Medkit);
        if (chaos) player.AddItem(ItemType.Adrenaline); else player.AddItem(ItemType.Radio);
        player.AddItem(chaos ? ItemType.KeycardChaosInsurgency : ItemType.KeycardMTFOperative);
        player.AddItem(ItemType.ArmorCombat);
        player.SetAmmo(ItemType.Ammo12gauge, 56);
        player.Scale = new Vector3(0.9f, 0.9f, 0.9f);
        player.EnableEffect<Scp207>();
        player.CustomInfo = $"{name.Replace("NTF ", string.Empty)}\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{name}</color>!</i></b></size>", 10);
    }
    public override string Name => name;
    public override void Dispose() { Player.Scale = Vector3.one; base.Dispose(); }
}
