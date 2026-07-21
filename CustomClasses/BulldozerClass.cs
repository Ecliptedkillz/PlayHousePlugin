using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;
using System.Linq;

namespace PlayhousePlugin.CustomClasses;

public sealed class BulldozerClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public BulldozerClass(Player player, Runtime.PluginRuntime runtime, bool chaos) : base(player)
    {
        this.chaos = chaos;
        abilities = new AbilityBase[] { new MorphineShotAbility(player, runtime) };
        player.ClearInventory();
        player.AddItem(ItemType.GunLogicer);
        player.AddItem(chaos ? ItemType.GunAK : ItemType.GunE11SR);
        player.AddItem(ItemType.ArmorHeavy);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Medkit);
        if (!chaos) player.AddItem(ItemType.Radio);
        player.AddItem(chaos ? ItemType.KeycardChaosInsurgency : ItemType.KeycardMTFOperative);
        player.SetAmmo(ItemType.Ammo762x39, 200);
        if (!chaos) player.SetAmmo(ItemType.Ammo556x45, 200);
        player.MaxHealth = 200; player.Health = 200;
        player.Scale = new Vector3(1.1f, 1.1f, 1.1f);
        player.CustomInfo = $"{(chaos ? string.Empty : "NTF ")}Bulldozer\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{Name}</color>!</i></b></size>", 10);
    }
    public override string Name => chaos ? "Chaos Bulldozer" : "NTF Bulldozer";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    public override void Dispose() { Player.Scale = Vector3.one; base.Dispose(); }
}

public sealed class MorphineShotAbility : CooldownAbilityBase
{
    private readonly Runtime.PluginRuntime runtime;
    public MorphineShotAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Morphine Shot";
    public override double CooldownSeconds => 45;
    protected override bool UseCooldownAbility(out string response)
    {
        Player.EnableEffect<Ensnared>(1, 5);
        Player.EnableEffect<AmnesiaVision>(1, 5);
        Player.EnableEffect<Concussed>(1, 2);
        runtime.Schedule(2f, () => { if (LabApi.Features.Wrappers.Player.ReadyList.Contains(Player)) Player.EnableEffect<Blindness>(1, 3); });
        runtime.Schedule(5f, () =>
        {
            if (!LabApi.Features.Wrappers.Player.ReadyList.Contains(Player)) return;
            Player.EnableEffect<Invigorated>(1, 10);
            int ticks = 0;
            Runtime.ScheduledHandle? regen = null;
            regen = runtime.Repeat(1f, () => { if (++ticks > 10) { regen?.Cancel(); return; } Player.Health = Math.Min(Player.MaxHealth, Player.Health + 2); });
        });
        response = "Injecting morphine; regeneration begins in five seconds.";
        Player.SendHint("<color=yellow>Injecting Morphine</color>", 5f);
        return true;
    }
}
