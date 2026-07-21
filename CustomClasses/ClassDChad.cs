using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ClassDChad : CustomClassBase
{
    public ClassDChad(Player player) : base(player)
    {
        player.MaxHealth = 125;
        player.Health = 125;
        player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
        player.AddItem(ItemType.KeycardJanitor);
        player.AddItem(ItemType.Flashlight);
        player.AddItem(ItemType.Coin);
        if (UnityEngine.Random.Range(0, 100) < 2) player.AddItem(ItemType.Coin);
        player.CustomInfo = "Chad\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You have spawned as a <color=orange>Class D Chad</color>!</i></b></size>", 10);
        player.SendConsoleMessage("Name: Class D Chad\n\nYou have 125 HP, a larger model, and a janitor keycard.", "yellow");
    }
    public override string Name => "Class D Chad";
    public override void Dispose()
    {
        Player.Scale = Vector3.one;
        base.Dispose();
    }
}
