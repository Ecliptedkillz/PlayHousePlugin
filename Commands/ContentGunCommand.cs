using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ContentGunCommand : ICommand
{
    private static readonly int[] Allowances = { 0, 5, 10, 25, 50, 100, 200 };
    public string Command => "contentgun";
    public string[] Aliases => new[] { "cgun", "contentg", "cttg" };
    public string Description => "Activates the donor ragdoll gun for this round.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        PlayhousePlugin? plugin = PlayhousePlugin.Instance;
        if (player is null || plugin is null) { response = "Player only."; return false; }
        if (plugin.State.ContentGunRounds.ContainsKey(player.PlayerId)) { response = "You have already used the content gun this round."; return false; }
        if (!plugin.Donators.TryGet(player.UserId, out External.Donator donor) || donor.Tier < 1)
        { response = "This command requires donor tier 1 or higher."; return false; }
        int allowance = Allowances[Math.Min(donor.Tier, Allowances.Length - 1)];
        plugin.State.ContentGunRounds[player.PlayerId] = allowance;
        player.SendBroadcast($"<i>Content gun activated for {allowance} ragdolls.</i>", 4);
        response = $"Content gun activated for {allowance} ragdolls.";
        return true;
    }
}
