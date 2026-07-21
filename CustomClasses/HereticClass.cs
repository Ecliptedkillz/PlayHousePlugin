using System;
using System.Collections.Generic;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class HereticClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public HereticClass(Player player, Runtime.PluginRuntime runtime, bool chaos) : base(player)
    {
        this.chaos = chaos;
        abilities = new AbilityBase[] { new TonicShotAbility(player, runtime) };
        player.ClearInventory();
        player.AddItem(chaos ? ItemType.GunAK : ItemType.GunE11SR);
        player.AddItem(ItemType.GunRevolver);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Painkillers);
        player.AddItem(ItemType.Painkillers);
        player.AddItem(chaos ? ItemType.KeycardChaosInsurgency : ItemType.KeycardMTFOperative);
        player.SetAmmo(ItemType.Ammo44cal, 48);
        player.SetAmmo(chaos ? ItemType.Ammo762x39 : ItemType.Ammo556x45, 120);
        player.CustomInfo = $"{(chaos ? string.Empty : "NTF ")}Heretic\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{Name}</color>!</i></b></size>", 10);
    }
    public override string Name => chaos ? "Chaos Heretic" : "NTF Heretic";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

public sealed class TonicShotAbility : CooldownAbilityBase, IDisposable
{
    private readonly Runtime.PluginRuntime runtime;
    private readonly List<Runtime.ScheduledHandle> handles = new();
    private int withdrawal;
    private int secondsRemaining;
    public TonicShotAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Tonic Shot";
    public override double CooldownSeconds => 40;
    public override string GenerateHud() => secondsRemaining > 0 ? $"Selected: {Name} ({secondsRemaining}s remaining)" : base.GenerateHud();
    protected override bool UseCooldownAbility(out string response)
    {
        Player.DisableEffect<HeavyFooted>();
        Player.MaxHealth = 125;
        Player.Health = Math.Max(Player.Health, 100);
        Player.EnableEffect<MovementBoost>(50, 20);
        Player.EnableEffect<Scp1853>(1, 20);
        secondsRemaining = 20;
        int regenTicks = 0;
        Runtime.ScheduledHandle? tonic = null;
        tonic = runtime.Repeat(1f, () =>
        {
            secondsRemaining--;
            if (regenTicks++ < 10) Player.Health = Math.Min(Player.MaxHealth, Player.Health + 2);
            if (secondsRemaining > 0) return;
            tonic?.Cancel();
            ApplyWithdrawal();
        });
        handles.Add(tonic);
        response = "Tonic active for 20 seconds.";
        return true;
    }
    private void ApplyWithdrawal()
    {
        withdrawal++;
        float maxHealth = Math.Max(50, 100 - 5 * withdrawal);
        Player.MaxHealth = maxHealth;
        Player.Health = Math.Min(Player.Health, maxHealth);
        if (withdrawal <= 4) Player.EnableEffect<HeavyFooted>((byte)Math.Min(byte.MaxValue, withdrawal), 0);
        Player.SendHint($"<color=red>Tonic withdrawal level {withdrawal}: max health {maxHealth:0}</color>", 4f);
    }
    public void Dispose()
    {
        foreach (Runtime.ScheduledHandle handle in handles) handle.Dispose();
        handles.Clear();
        Player.DisableEffect<MovementBoost>();
        Player.DisableEffect<HeavyFooted>();
        Player.DisableEffect<Scp1853>();
    }
}
