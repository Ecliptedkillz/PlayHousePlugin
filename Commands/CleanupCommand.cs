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
        if (target is "bodies" or "body" or "ragdoll" or "ragdolls" or "all")
            foreach (Ragdoll ragdoll in Ragdoll.List.ToArray()) ragdoll.Destroy();
        if (target is "items" or "all")
            foreach (Pickup pickup in Pickup.List.ToArray()) pickup.Destroy();
        if (target is not ("bodies" or "body" or "ragdoll" or "ragdolls" or "items" or "all"))
        {
            response = "Usage: cleanup <ragdolls|items|all>";
            return false;
        }
        response = $"Cleaned up {target}.";
        return true;
    }
}
