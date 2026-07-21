using System.Linq;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class JanitorCleanupAbility : NonCooldownAbilityBase
{
    public JanitorCleanupAbility(Player player) : base(player) { }
    public override string Name => "Body Cleanup";

    protected override bool UseAbility(out string response)
    {
        Ragdoll? ragdoll = Ragdoll.List
            .Where(item => Vector3.Distance(item.Position, Player.Position) <= 3f)
            .OrderBy(item => Vector3.Distance(item.Position, Player.Position))
            .FirstOrDefault();
        if (ragdoll is null)
        {
            response = "There are no bodies nearby.";
            Player.SendHint("<color=yellow>There are no bodies nearby</color>", 3);
            return false;
        }

        int chance = UnityEngine.Random.Range(0, 100);
        ItemType reward = chance > 50 ? ItemType.Coin
            : chance > 25 ? ItemType.KeycardScientist
            : chance > 13 ? ItemType.KeycardZoneManager
            : chance > 6 ? ItemType.KeycardResearchCoordinator
            : chance > 3 ? ItemType.KeycardMTFOperative
            : chance > 2 ? ItemType.KeycardContainmentEngineer
            : ItemType.KeycardO5;
        Player.AddItem(reward);
        ragdoll.Destroy();
        Player.SendHint("<color=yellow>Body Cleaned!</color>", 3);
        response = $"Body cleaned. You found {reward}.";
        return true;
    }
}
