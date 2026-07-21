using System;
using CommandSystem;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class SuicideCommand : ICommand
{
    private static readonly string[] Reasons =
    {
        "too much cringe", "extremely high levels of cringe", "heart attack", "twitter user",
        "told a yo mama joke", "didn't touch grass", "this is so sad", "being a corpse",
        "unironically played fortnite", "The Bite of '87", "stepped on a lego", "skill issue",
        "ඞ amogus ඞ",
    };

    public string Command => "kill";
    public string[] Aliases => new[] { "suicide" };
    public string Description => "Kills you instantly.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        Player? player = Player.Get(sender);
        if (player is null || !player.IsAlive || player.IsGodModeEnabled)
        {
            response = "You cannot use this command right now.";
            return false;
        }

        string reason = Reasons[new Random().Next(Reasons.Length)];
        player.Kill(reason, string.Empty);
        if (!player.DoNotTrack)
            PlayhousePlugin.Instance?.Statistics?.Enqueue(player.UserId, player.Nickname, "killbinds", 1);

        response = $"{player.Nickname} bid farewell to this cruel world.";
        return true;
    }
}
