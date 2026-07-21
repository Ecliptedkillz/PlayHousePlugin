using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;
using CustomPlayerEffects;

namespace PlayhousePlugin.CustomClasses;

public sealed class NtfMedicRevive : CooldownAbilityBase
{
    private readonly Runtime.PluginRuntime runtime;
    private readonly RoleTypeId reviveRole;
    private readonly string medicName;
    private readonly string medicColor;
    public NtfMedicRevive(Player player, Runtime.PluginRuntime runtime, RoleTypeId reviveRole = RoleTypeId.NtfSergeant, string medicName = "NTF Medic", string medicColor = "navy") : base(player)
    {
        this.runtime = runtime;
        this.reviveRole = reviveRole;
        this.medicName = medicName;
        this.medicColor = medicColor;
    }
    public override string Name => "Revive";
    public override double CooldownSeconds => 30;

    protected override bool UseCooldownAbility(out string response)
    {
        if (Player.Room?.Name == MapGeneration.RoomName.Pocket)
        {
            response = "You cannot revive in the Pocket Dimension.";
            Player.SendHint("<color=yellow>You cannot revive in the Pocket Dimension</color>", 3);
            return false;
        }
        Ragdoll? ragdoll = Ragdoll.List.Where(d => !d.IsDestroyed && Vector3.Distance(d.Position, Player.Position) <= 3f)
            .OrderBy(d => Vector3.Distance(d.Position, Player.Position)).FirstOrDefault();
        if (ragdoll is null)
        {
            response = "There are no bodies nearby.";
            Player.SendHint("<color=yellow>There are no bodies nearby</color>", 3f);
            return false;
        }

        Player? patient = LabApi.Features.Wrappers.Player.Get(ragdoll.Base.Info.OwnerHub);
        if (patient is null || patient.IsAlive)
        {
            response = patient?.IsAlive == true ? "This person is already alive." : "This body cannot be revived.";
            ragdoll.Destroy();
            return false;
        }

        Vector3 position = Player.Position;
        patient.SetRole(reviveRole);
        ragdoll.Destroy();
        runtime.Schedule(0.75f, () =>
        {
            if (!LabApi.Features.Wrappers.Player.ReadyList.Contains(patient)) return;
            patient.EnableEffect<MovementBoost>(15, 10);
            patient.ArtificialHealth = 70;
            patient.Position = position + Vector3.up * 1.6f;
            patient.SendBroadcast($"<size=60><b><i>You have been revived by a <color={medicColor}>{medicName}</color>!</i></b></size>", 7);
            Player.SendHint("<color=yellow>Revived Patient!</color>", 3f);
        });
        response = $"Revived {patient.Nickname}.";
        return true;
    }
}
