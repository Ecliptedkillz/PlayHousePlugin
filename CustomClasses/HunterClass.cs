using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class HunterClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public HunterClass(Player player, bool chaos) : base(player)
    {
        this.chaos = chaos;
        abilities = new AbilityBase[] { new InfraredVisionAbility(player) };
        player.ClearInventory();
        player.AddItem(ItemType.GunShotgun);
        player.AddItem(ItemType.GunRevolver);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Medkit);
        if (chaos) { player.AddItem(ItemType.Medkit); player.AddItem(ItemType.Painkillers); } else player.AddItem(ItemType.Radio);
        player.AddItem(chaos ? ItemType.KeycardChaosInsurgency : ItemType.KeycardMTFOperative);
        player.SetAmmo(ItemType.Ammo12gauge, 56);
        player.SetAmmo(ItemType.Ammo44cal, 30);
        player.EnableEffect<Scp207>();
        player.CustomInfo = $"{(chaos ? string.Empty : "NTF ")}Hunter\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{Name}</color>!</i></b></size>", 10);
    }
    public override string Name => chaos ? "Chaos Hunter" : "NTF Hunter";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    public override void Dispose() { Player.DisableEffect<Scp207>(); base.Dispose(); }
}

public sealed class InfraredVisionAbility : CooldownAbilityBase
{
    public InfraredVisionAbility(Player player) : base(player) { }
    public override string Name => "Infrared Vision";
    public override double CooldownSeconds => 30;
    protected override bool UseCooldownAbility(out string response)
    {
        Player.EnableEffect<NightVision>(3, 10);
        response = "Infrared Vision activated for 10 seconds.";
        Player.SendHint("<color=yellow>Infrared Vision Activated</color>", 3f);
        return true;
    }
}
