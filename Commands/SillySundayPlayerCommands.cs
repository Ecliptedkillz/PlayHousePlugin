using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayerRoles;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ZombieFriendlyExplosionCommand : ICommand
{
    public string Command => "zfe";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Silly Sunday SCP self-explosion.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null) { response = "Player only."; return false; }
        if (PlayhousePlugin.Instance?.SillySunday?.IsEnabled != true || player.Team != Team.SCPs)
        { response = "You cannot use this command."; return false; }
        if (player.Role != RoleTypeId.Scp0492 && player.Health > 200)
        { response = "Non-zombie SCPs can only explode at 200 HP or less."; return false; }
        Utils.ExplosionUtils.ServerExplode(player.ReferenceHub, ExplosionType.Grenade);
        response = "KABOOOM!";
        return true;
    }
}

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class BossDeathCommand : ICommand
{
    public string Command => "die";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Runs the legacy Silly Sunday boss defeat sequence.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        PlayhousePlugin? plugin = PlayhousePlugin.Instance;
        if (player is null || plugin?.Runtime is null) { response = "Player only."; return false; }
        if (!player.UserId.Split('@')[0].Equals("Eclipted", StringComparison.OrdinalIgnoreCase))
        { response = "You cannot use this command."; return false; }
        if (plugin.SillySunday?.IsEnabled != true) { response = "Not Sunday."; return false; }

        player.IsNoclipEnabled = true;
        player.IsGodModeEnabled = true;
        Server.SendBroadcast("<b><color=red><i>Boss has been defeated!!</i></color></b>", 10);
        plugin.Runtime.Schedule(.5f, () => Effect(player));
        plugin.Runtime.Schedule(1.5f, () => { Effect(player); Sink(player, plugin.Runtime); RepeatEffect(player, plugin.Runtime); });
        plugin.Runtime.Schedule(12f, () =>
        {
            if (!Player.ReadyList.Contains(player)) return;
            player.IsGodModeEnabled = false;
            player.Kill("too much cringe", string.Empty);
            player.SetRole(RoleTypeId.Spectator);
            Round.IsLocked = false;
        });
        response = "Death sequence started.";
        return true;
    }

    private static void Effect(Player player) => Utils.ExplosionUtils.ServerSpawnEffect(player.Position, ItemType.GrenadeHE);
    private static void Sink(Player player, Runtime.PluginRuntime runtime)
    {
        Runtime.ScheduledHandle? handle = null;
        handle = runtime.Repeat(.1f, () =>
        {
            if (!player.IsAlive) { handle?.Cancel(); return; }
            player.Position += Vector3.down * .2f;
        });
    }
    private static void RepeatEffect(Player player, Runtime.PluginRuntime runtime)
    {
        if (!player.IsAlive) return;
        Effect(player);
        runtime.Schedule(UnityEngine.Random.Range(.4f, 1.4f), () => RepeatEffect(player, runtime));
    }
}
