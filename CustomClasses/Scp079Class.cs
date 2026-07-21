using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayerRoles.PlayableScps.Scp079;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class Scp079Class : CustomClassBase
{
    private readonly IReadOnlyList<AbilityBase> abilities;
    public Scp079Class(Player player, Runtime.PluginRuntime runtime) : base(player)
    {
        abilities = new AbilityBase[]
        {
            new Scp079StalkAbility(player),
            new Scp079GeneratorOverrideAbility(player, runtime),
            new Scp079WarheadJamAbility(player),
            new Scp079NeurotoxinAbility(player, runtime),
        };
        player.SendBroadcast("<size=40><b><i>You have spawned as <color=red>SCP-079</color> with four custom abilities.</i></b></size>", 10);
        player.SendConsoleMessage("SCP-079 abilities: Stalk (Tier 2), Generator Override (Tier 3), Warhead Jam (Tier 4), Neurotoxin (Tier 5).", "yellow");
    }
    public override string Name => "SCP-079";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

internal static class Scp079Systems
{
    public static bool TryGet(Player player, out Scp079TierManager tier, out Scp079AuxManager aux, out Scp079CurrentCameraSync camera)
    {
        tier = null!; aux = null!; camera = null!;
        if (player.RoleBase is not Scp079Role role) return false;
        return role.SubroutineModule.TryGetSubroutine(out tier) && role.SubroutineModule.TryGetSubroutine(out aux) && role.SubroutineModule.TryGetSubroutine(out camera);
    }
}

public sealed class Scp079StalkAbility : NonCooldownAbilityBase
{
    private readonly System.Random random = new();
    public Scp079StalkAbility(Player player) : base(player) { }
    public override string Name => "Stalk";
    protected override bool UseAbility(out string response)
    {
        if (!Scp079Systems.TryGet(Player, out Scp079TierManager tier, out Scp079AuxManager aux, out Scp079CurrentCameraSync camera) || tier.AccessTierIndex < 1)
        { response = "You need Tier 2 to use Stalk."; return false; }
        Player[] targets = LabApi.Features.Wrappers.Player.ReadyList.Where(p => p.IsAlive && p.Team != Team.SCPs && p.Role != RoleTypeId.Tutorial && p.Room is not null).ToArray();
        if (targets.Length == 0) { response = "There are no valid players to stalk."; return false; }
        Player target = targets[random.Next(targets.Length)];
        LabApi.Features.Wrappers.Camera? targetCamera = LabApi.Features.Wrappers.Camera.List.FirstOrDefault(c => c.Room == target.Room);
        if (targetCamera is null) { response = "No camera covers the selected player."; return false; }
        float cost = camera.GetSwitchCost(targetCamera.Base);
        if (aux.CurrentAux < cost) { response = $"You need {Math.Ceiling(cost - aux.CurrentAux)} more AP."; return false; }
        aux.CurrentAux -= cost;
        camera.ClientSwitchTo(targetCamera.Base);
        response = $"Stalking {target.Nickname}.";
        return true;
    }
}

public sealed class Scp079GeneratorOverrideAbility : CooldownAbilityBase
{
    private readonly Runtime.PluginRuntime runtime;
    public Scp079GeneratorOverrideAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Generator Override";
    public override double CooldownSeconds => 60;
    protected override bool UseCooldownAbility(out string response)
    {
        if (!Scp079Systems.TryGet(Player, out Scp079TierManager tier, out Scp079AuxManager aux, out _) || tier.AccessTierIndex < 2)
        { response = "You need Tier 3 to use Generator Override."; return false; }
        if (aux.CurrentAux < 70) { response = "You need 70 AP."; return false; }
        FacilityZone zone = Player.Room!.Zone;
        if (zone == FacilityZone.Surface) { response = "Generator Override cannot be used on surface."; return false; }
        Map.TurnOffLights(30f, zone);
        int ticks = 0;
        Runtime.ScheduledHandle? drain = null;
        drain = runtime.Repeat(0.1f, () => { if (++ticks >= 300) { drain?.Cancel(); return; } if (aux.CurrentAux > 10) aux.CurrentAux = 10; });
        Announcer.Message($"SCP 0 7 9 power override detected at {zone} . backup generators engaged . standby", string.Empty, true, 0f, 1f);
        response = $"Blacked out {zone} for 30 seconds.";
        return true;
    }
}

public sealed class Scp079WarheadJamAbility : CooldownAbilityBase
{
    public Scp079WarheadJamAbility(Player player) : base(player) { }
    public override string Name => "Warhead Jam";
    public override double CooldownSeconds => 60;
    protected override bool UseCooldownAbility(out string response)
    {
        if (!Scp079Systems.TryGet(Player, out Scp079TierManager tier, out Scp079AuxManager aux, out _) || tier.AccessTierIndex < 3)
        { response = "You need Tier 4 to use Warhead Jam."; return false; }
        if (!Warhead.IsDetonationInProgress) { response = "The warhead is not active."; return false; }
        if (Warhead.DetonationTime <= 10) { response = "Detonation is inevitable."; return false; }
        if (aux.CurrentAux < 90) { response = "You need 90 AP."; return false; }
        aux.CurrentAux -= 90;
        Warhead.DetonationTime += 15;
        Server.SendBroadcast("<i>SCP-079 delayed the warhead detonation timer!</i>", 6);
        response = "Added 15 seconds to the warhead timer.";
        return true;
    }
}

public sealed class Scp079NeurotoxinAbility : CooldownAbilityBase
{
    private static readonly HashSet<Room> PoisonedRooms = new();
    private readonly Runtime.PluginRuntime runtime;
    public Scp079NeurotoxinAbility(Player player, Runtime.PluginRuntime runtime) : base(player) => this.runtime = runtime;
    public override string Name => "Neurotoxin";
    public override double CooldownSeconds => 20;
    protected override bool UseCooldownAbility(out string response)
    {
        if (!Scp079Systems.TryGet(Player, out Scp079TierManager tier, out Scp079AuxManager aux, out _) || tier.AccessTierIndex < 4)
        { response = "You need Tier 5 to use Neurotoxin."; return false; }
        Room room = Player.Room!;
        if (room.Zone == FacilityZone.Surface) { response = "This space is too large for Neurotoxin."; return false; }
        if (aux.CurrentAux < 120) { response = "You need 120 AP."; return false; }
        if (!PoisonedRooms.Add(room)) { response = "This room is already poisoned."; return false; }
        aux.CurrentAux -= 120;
        int ticks = 0;
        Runtime.ScheduledHandle? poison = null;
        poison = runtime.Repeat(0.1f, () =>
        {
            if (++ticks > 150) { poison?.Cancel(); PoisonedRooms.Remove(room); return; }
            foreach (Player target in room.Players.ToArray())
            {
                if (!target.IsAlive || target.Team == Team.SCPs) continue;
                if (target.Health <= 1) target.Damage(float.MaxValue, "Neurotoxin Gas", string.Empty);
                else { target.Health -= 1; target.SendHint("<color=yellow>You are breathing toxic gas from the air vents</color>", 1f); }
            }
        });
        response = "Poisoning the current room for 15 seconds.";
        return true;
    }
}
