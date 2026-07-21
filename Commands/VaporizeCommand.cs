using System;
using CommandSystem;
using Footprinting;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.ShotEvents;
using InventorySystem.Items.Firearms.Modules;
using LabApi.Features.Wrappers;
using PlayerStatsSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class VaporizeCommand : ICommand
{
    public string Command => "vaporize";
    public string[] Aliases => new[] { "vapourize", "vape" };
    public string Description => "Vaporizes you.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null || !player.IsAlive || player.IsGodModeEnabled)
        {
            response = "You cannot use this command right now.";
            return false;
        }
        var shot = new DisruptorShotEvent(default, new Footprint(player.ReferenceHub), DisruptorActionModule.FiringState.FiringSingle);
        player.Damage(new DisruptorDamageHandler(shot, player.Position, int.MaxValue));
        if (!player.DoNotTrack) PlayhousePlugin.Instance?.Statistics?.Enqueue(player.UserId, player.Nickname, "killbinds", 1);
        response = $"{player.Nickname} bid farewell to this cruel world.";
        return true;
    }
}
