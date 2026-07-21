using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class NtfCaptainClass : CustomClassBase
{
    private readonly IReadOnlyList<AbilityBase> abilities;
    public NtfCaptainClass(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        abilities = new AbilityBase[] { new HotBulletsAbility(player, runtime) };
        player.CustomInfo = "NTF Captain\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You are the <color=navy>NTF Captain</color>. Use .ability for Hot Bullets.</i></b></size>", 10);
    }
    public override string Name => "NTF Captain";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

public sealed class HotBulletsAbility : CooldownAbilityBase, IDisposable
{
    private readonly Runtime.PluginRuntime runtime;
    private Runtime.ScheduledHandle? expiry;
    public HotBulletsAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Hot Bullets";
    public override double CooldownSeconds => 30;
    public bool IsActive { get; private set; }
    protected override bool UseCooldownAbility(out string response)
    {
        IsActive = true;
        expiry?.Dispose();
        expiry = runtime.Schedule(10, () => IsActive = false);
        response = "Hot Bullets active for 10 seconds.";
        return true;
    }
    public void Dispose() { expiry?.Dispose(); IsActive = false; }
}

public sealed class NtfSergeantClass : CustomClassBase
{
    public NtfSergeantClass(Player player) : base(player) => player.CustomInfo = "NTF Sergeant\n(Custom Class)";
    public override string Name => "NTF Sergeant";
}

public sealed class GuardManagerClass : CustomClassBase
{
    public GuardManagerClass(Player player) : base(player)
    {
        player.ClearInventory();
        player.AddItem(ItemType.KeycardMTFOperative);
        player.AddItem(ItemType.GunE11SR);
        player.AddItem(ItemType.GunCOM15);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Radio);
        player.AddItem(ItemType.ArmorHeavy);
        player.AddItem(ItemType.GrenadeFlash);
        player.SetAmmo(ItemType.Ammo556x45, 60);
        player.SetAmmo(ItemType.Ammo9x19, 60);
        player.CustomInfo = "Guard Manager\n(Custom Class)";
    }
    public override string Name => "Guard Manager";
}

public sealed class ClassDJanitorClass : CustomClassBase
{
    private readonly Vector3 originalScale;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public ClassDJanitorClass(Player player) : base(player)
    {
        abilities = new AbilityBase[] { new JanitorCleanupAbility(player) };
        originalScale = player.Scale;
        player.Scale = Vector3.one * 0.9f;
        player.MaxHealth = 120;
        player.Health = Math.Min(120, Math.Max(player.Health, 100));
        player.AddItem(ItemType.KeycardJanitor);
        player.AddItem(ItemType.Flashlight);
        player.AddItem(ItemType.Coin);
        if (UnityEngine.Random.Range(0, 100) < 2) player.AddItem(ItemType.Coin);
        player.CustomInfo = "Janitor\n(Custom Class)";
        player.SendBroadcast("<size=40><b><i>You have spawned as a <color=orange>Class D Janitor</color>! Use .ability to clean nearby bodies.</i></b></size>", 10);
    }
    public override string Name => "Class D Janitor";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    public override void Dispose() { Player.Scale = originalScale; Player.MaxHealth = 100; base.Dispose(); }
}

public sealed class MajorScientistClass : CustomClassBase
{
    public MajorScientistClass(Player player) : base(player)
    {
        bool hadO5 = player.Items.Any(item => item.Type == ItemType.KeycardO5);
        player.ClearInventory();
        player.AddItem(ItemType.KeycardResearchCoordinator);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.SCP207);
        player.AddItem(ItemType.SCP500);
        player.AddItem(ItemType.Flashlight);
        if (hadO5) player.AddItem(ItemType.KeycardO5);
        player.CustomInfo = "Major Scientist Jr.\n(Custom Class)";
    }
    public override string Name => "Major Scientist Jr.";
}
