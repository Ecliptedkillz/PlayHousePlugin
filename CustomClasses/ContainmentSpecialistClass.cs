using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class ContainmentSpecialistClass : CustomClassBase
{
    private readonly bool chaos;
    public ContainmentSpecialistClass(Player player, bool chaos) : base(player)
    {
        this.chaos = chaos;
        player.ClearInventory();
        player.AddItem(ItemType.KeycardFacilityManager);
        player.AddItem(ItemType.GunCOM18);
        player.AddItem(ItemType.GunRevolver);
        player.AddItem(ItemType.Medkit);
        player.AddItem(chaos ? ItemType.Adrenaline : ItemType.Radio);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.GrenadeFlash);
        player.SetAmmo(ItemType.Ammo9x19, 160);
        player.SetAmmo(ItemType.Ammo44cal, 48);
        player.CustomInfo = $"{(chaos ? "Chaos " : string.Empty)}Containment Specialist\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>Containment Specialist</color>!</i></b></size>", 10);
    }
    public override string Name => chaos ? "Chaos Containment Specialist" : "NTF Containment Specialist";
}
