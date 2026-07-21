using System;
using System.Collections.Generic;
using System.Diagnostics;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using PlayerRoles;

namespace PlayhousePlugin.CustomClasses;

public sealed class Scp106Class : CustomClassBase
{
    private readonly IReadOnlyList<AbilityBase> abilities;
    public Scp106Class(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        VanishAbility vanish = new(player, runtime);
        abilities = new AbilityBase[] { vanish };
        player.MaxHealth = 1000;
        player.Health = 1000;
        player.MaxHumeShield = 1500;
        player.HumeShield = 1500;
        player.HumeShieldRegenRate = 20;
        player.HumeShieldRegenCooldown = 20;
        player.EnableEffect<MovementBoost>(10);
        Track(runtime.Repeat(1f, HealNearbyZombies));
        player.SendBroadcast("<size=40><b><i>You have spawned as <color=red>SCP-106</color> with Vanish.</i></b></size>", 10);
    }
    public override string Name => "SCP-106";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
    public VanishAbility Vanish => (VanishAbility)abilities[0];
    private void HealNearbyZombies()
    {
        foreach (Player zombie in LabApi.Features.Wrappers.Player.ReadyList)
            if (zombie.Role == RoleTypeId.Scp0492 && UnityEngine.Vector3.Distance(zombie.Position, Player.Position) <= 7f)
                zombie.Health = Math.Min(zombie.MaxHealth, zombie.Health + 7);
    }
}

public sealed class VanishAbility : AbilityBase, IDisposable
{
    private readonly Runtime.PluginRuntime runtime;
    private readonly Stopwatch cooldown = Stopwatch.StartNew();
    private readonly List<Runtime.ScheduledHandle> handles = new();
    public VanishAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Vanish";
    public bool IsVanished { get; private set; }
    public override string GenerateHud() => IsVanished ? "Selected: Vanish (Active)" : cooldown.Elapsed.TotalSeconds < 30 ? $"Selected: Vanish ({Math.Ceiling(30 - cooldown.Elapsed.TotalSeconds)}s)" : "Selected: Vanish (Ready)";
    public override bool Use(out string response)
    {
        if (IsVanished) { response = "Vanish is already active."; return false; }
        if (cooldown.Elapsed.TotalSeconds < 30) { response = $"Vanish is ready in {Math.Ceiling(30 - cooldown.Elapsed.TotalSeconds)} seconds."; return false; }
        IsVanished = true;
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (state is null) { response = "Plugin state unavailable."; IsVanished = false; return false; }
        foreach (Player other in LabApi.Features.Wrappers.Player.ReadyList)
            if (other != Player && other.IsAlive && other.Team != Team.SCPs && other.Health > 40)
                state.HiddenPlayerPairs.Add(Key(Player, other));
        Player.EnableEffect<MovementBoost>(50);
        Player.EnableEffect<Scp207>(4);
        Player.HumeShieldRegenRate = 50;
        handles.Add(runtime.Schedule(17f, EndVanish));
        response = "Vanish activated for 17 seconds.";
        return true;
    }
    private void EndVanish()
    {
        IsVanished = false;
        ClearPairs();
        Player.DisableEffect<Scp207>();
        Player.EnableEffect<MovementBoost>(10);
        Player.HumeShieldRegenRate = 20;
        cooldown.Restart();
    }
    public static string Key(Player hidden, Player observer) => $"{hidden.PlayerId}:{observer.PlayerId}";
    private void ClearPairs()
    {
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (state is null) return;
        state.HiddenPlayerPairs.RemoveWhere(value => value.StartsWith(Player.PlayerId + ":", StringComparison.Ordinal));
    }
    public void Dispose()
    {
        foreach (Runtime.ScheduledHandle handle in handles) handle.Dispose();
        handles.Clear();
        ClearPairs();
        Player.DisableEffect<Scp207>();
        IsVanished = false;
    }
}
