using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class SeniorGuard : CustomClassBase
{
    public SeniorGuard(Player player) : base(player)
    {
        player.ClearInventory();
        player.AddItem(ItemType.KeycardMTFOperative);
        player.AddItem(ItemType.GunCrossvec);
        player.AddItem(ItemType.GrenadeFlash);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Radio);
        player.AddItem(ItemType.ArmorCombat);
        player.SetAmmo(ItemType.Ammo9x19, 80);
        player.CustomInfo = "Senior Guard\n(Custom Class)";
    }
    public override string Name => "Senior Guard";
}
