using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayerRoles;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ExterminatorClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;
    public ExterminatorClass(Player player, Runtime.PluginRuntime runtime, bool chaos) : base(player)
    {
        this.chaos = chaos;
        abilities = new AbilityBase[] { new GasGrenadeAbility(player, runtime, chaos) };
        player.ClearInventory();
        player.AddItem(chaos ? ItemType.GunAK : ItemType.GunCrossvec);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Medkit);
        if (chaos) { player.AddItem(ItemType.Medkit); player.AddItem(ItemType.Medkit); } else player.AddItem(ItemType.Radio);
        player.AddItem(chaos ? ItemType.KeycardChaosInsurgency : ItemType.KeycardMTFOperative);
        player.SetAmmo(chaos ? ItemType.Ammo762x39 : ItemType.Ammo9x19, 120);
        player.CustomInfo = $"{(chaos ? string.Empty : "NTF ")}Exterminator\n(Custom Class)";
        player.SendBroadcast($"<size=40><b><i>You have spawned as a <color={(chaos ? "green" : "navy")}>{Name}</color>!</i></b></size>", 10);
    }
    public override string Name => chaos ? "Chaos Exterminator" : "NTF Exterminator";
    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

public sealed class GasGrenadeAbility : AbilityBase, IDisposable
{
    private readonly Runtime.PluginRuntime runtime;
    private readonly bool chaos;
    private readonly List<Runtime.ScheduledHandle> handles = new();
    private int gasAmount;
    public GasGrenadeAbility(Player player, Runtime.PluginRuntime runtime, bool chaos) : base(player)
    {
        this.runtime = runtime; this.chaos = chaos;
        handles.Add(runtime.Repeat(1f, () => { if (gasAmount < 60) gasAmount++; }));
    }
    public override string Name => "Gas Grenade";
    public override string GenerateHud() => gasAmount >= 60 ? "Selected: Gas Grenade (2 canisters ready)" : gasAmount >= 30 ? $"Selected: Gas Grenade (1 ready, second {(gasAmount - 30) / 30f:P0})" : $"Selected: Gas Grenade ({gasAmount / 30f:P0})";
    public override bool Use(out string response)
    {
        if (gasAmount < 30) { response = "Gas is recharging."; return false; }
        gasAmount -= 30;
        SchematicObject? grenade = PlayhousePlugin.Instance?.Schematics.Spawn(chaos ? "GasGrenade" : "GasGrenadeBlue", Player.Camera.position + Player.Camera.forward + Vector3.up * 1.4f, Quaternion.Euler(0, Player.Camera.rotation.eulerAngles.y - 90, 0), Vector3.one);
        if (grenade is null) { gasAmount += 30; response = "The gas-grenade schematic is unavailable."; return false; }
        Rigidbody body = grenade.gameObject.AddComponent<Rigidbody>();
        body.mass = 0.3f; body.angularDamping = 0.2f; body.linearDamping = 0.2f;
        body.AddForce(Player.Camera.forward * 5, ForceMode.Impulse);
        var collider = grenade.gameObject.AddComponent<CapsuleCollider>(); collider.radius = 0.25f; collider.direction = 0;
        handles.Add(runtime.Schedule(3f, () => StartGas(grenade)));
        response = "Throwing Gas Grenade.";
        return true;
    }
    private void StartGas(SchematicObject grenade)
    {
        int ticks = 0;
        Runtime.ScheduledHandle? gas = null;
        gas = runtime.Repeat(0.5f, () =>
        {
            if (++ticks > 30) { gas?.Cancel(); PlayhousePlugin.Instance?.Schematics.Destroy(grenade); return; }
            Vector3 position = grenade.Position;
            foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
            {
                if (!target.IsAlive || target == Player || Vector3.Distance(target.Position, position) > 4f || IsAlly(target)) continue;
                if (target.Health <= 4) target.Kill("Military Grade Bio-Weapon", string.Empty); else target.Health -= 4;
                target.SendHint("<color=yellow>You are being poisoned by a military grade bio-weapon</color>", 1f);
            }
        });
        handles.Add(gas);
    }
    private bool IsAlly(Player target) => chaos
        ? target.Role is RoleTypeId.ChaosConscript or RoleTypeId.ChaosMarauder or RoleTypeId.ChaosRepressor or RoleTypeId.ChaosRifleman or RoleTypeId.ClassD
        : target.Role is RoleTypeId.NtfCaptain or RoleTypeId.NtfSergeant or RoleTypeId.NtfSpecialist or RoleTypeId.NtfPrivate or RoleTypeId.FacilityGuard or RoleTypeId.Scientist;
    public void Dispose() { foreach (Runtime.ScheduledHandle handle in handles) handle.Dispose(); handles.Clear(); }
}
