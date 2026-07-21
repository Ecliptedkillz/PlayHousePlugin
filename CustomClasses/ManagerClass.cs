using CustomPlayerEffects;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class ManagerClass : CustomClassBase
{
    private readonly bool chaos;
    public ManagerClass(Player player,bool chaos):base(player)
    {
        this.chaos=chaos;player.ClearInventory();
        if(chaos){player.AddItem(ItemType.KeycardChaosInsurgency);player.AddItem(ItemType.GunAK);player.AddItem(ItemType.GunRevolver);player.AddItem(ItemType.Medkit);player.AddItem(ItemType.Adrenaline);player.AddItem(ItemType.ArmorHeavy);player.AddItem(ItemType.GrenadeFlash);player.SetAmmo(ItemType.Ammo762x39,120);player.SetAmmo(ItemType.Ammo44cal,42);}
        else{player.AddItem(ItemType.KeycardO5);player.AddItem(ItemType.GunE11SR);player.AddItem(ItemType.GunCrossvec);player.AddItem(ItemType.Medkit);player.AddItem(ItemType.Medkit);player.AddItem(ItemType.Radio);player.AddItem(ItemType.ArmorHeavy);player.AddItem(ItemType.GrenadeFlash);player.SetAmmo(ItemType.Ammo556x45,200);player.SetAmmo(ItemType.Ammo9x19,200);player.MaxHealth=400;player.Health=400;player.EnableEffect<MovementBoost>(25,0);}
        player.CustomInfo=$"{(chaos?"Chaos":"NTF")} Manager\n(Custom Class)";
    }
    public override string Name=>chaos?"Chaos Manager":"NTF Manager";
    public override void Dispose(){if(!chaos){Player.DisableEffect<MovementBoost>();Player.MaxHealth=100;}base.Dispose();}
}
