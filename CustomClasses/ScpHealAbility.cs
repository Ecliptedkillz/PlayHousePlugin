using System;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class ScpHealAbility : NonCooldownAbilityBase
{
    private float multiplier = 1f;
    public ScpHealAbility(Player player) : base(player) { }
    public override string Name => "SCP Heal";
    public override string GenerateHud() => $"Selected: {Name} ({multiplier * 100:0}% Efficiency - {100 * multiplier:0} HP next)";
    protected override bool UseAbility(out string response)
    {
        if (Player.Health <= 200) { response = "You do not have enough health to heal."; return false; }
        Player? patient = LabApi.Features.Wrappers.Player.ReadyList
            .Where(p => p != Player && p.IsAlive && p.Team == Team.SCPs && p.Role != RoleTypeId.Scp0492 && Vector3.Distance(p.Position, Player.Position) <= 5f)
            .Where(p => Vector3.Dot(Player.Camera.forward, (p.Position - Player.Camera.position).normalized) > 0.75f)
            .OrderBy(p => Vector3.Distance(p.Position, Player.Position)).FirstOrDefault();
        if (patient is null) { response = "There are no SCP patients within range."; return false; }
        float amount = (patient.Role == RoleTypeId.Scp106 ? 10f : 100f) * multiplier;
        patient.Health = Math.Min(patient.MaxHealth, patient.Health + amount);
        Player.Health -= 100;
        multiplier = Math.Max(0.2f, multiplier - 0.04f);
        response = $"Healed {patient.Nickname} for {amount:0.0} HP.";
        Player.SendHint($"<color=green>{response}</color>", 4f);
        return true;
    }
}
