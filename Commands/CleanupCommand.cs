using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class CleanupCommand : ICommand
{
    public string Command => "cleanup";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Cleans up ragdolls, item pickups, or both.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        if (arguments.Count != 1)
        {
            response = "Usage: cleanup <ragdolls|items|all>";
            return false;
        }

        string target = arguments.At(0).ToLowerInvariant();

        bool cleanupRagdolls = target is "body" or "bodies" or "ragdoll" or "ragdolls" or "all";
        bool cleanupItems = target is "item" or "items" or "all";

        if (!cleanupRagdolls && !cleanupItems)
        {
            response = "Usage: cleanup <ragdolls|items|all>";
            return false;
        }

        int ragdollCount = 0;
        int itemCount = 0;

        if (cleanupRagdolls)
        {
            var ragdolls = Ragdoll.List.ToArray();
            ragdollCount = ragdolls.Length;

            foreach (var ragdoll in ragdolls)
                ragdoll.Destroy();
        }

        if (cleanupItems)
        {
            var pickups = Pickup.List.ToArray();
            itemCount = pickups.Length;

            foreach (var pickup in pickups)
                pickup.Destroy();
        }

        response = $"Cleanup complete. Removed {ragdollCount} ragdolls and {itemCount} pickups.";
        return true;
    }
}