using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class RoundStatsCommand : ICommand
{
    public string Command => "roundstats";
    public string[] Aliases => new[] { "rs" };
    public string Description => "Shows the current Breakout Blitz objectives.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        var mode = PlayhousePlugin.Instance?.BreakoutBlitz;
        if (mode is null || !mode.IsEnabled)
        {
            response = "Breakout Blitz is not enabled for this round.";
            return false;
        }

        TimeSpan elapsed = mode.RoundTime;
        int reinforcementSeconds = Math.Max(0, 600 - (int)elapsed.TotalSeconds);
        response = $"Round time: {elapsed:mm\\:ss}\n" +
                   $"SCP kills: {mode.ScpKills}/{mode.RequiredScpKills}\n" +
                   $"Class-D escapes: {mode.ClassDEscapes}/{mode.RequiredClassDEscapes}\n" +
                   $"Scientist escapes: {mode.ScientistEscapes}/{mode.RequiredScientistEscapes}\n" +
                   $"NTF/Chaos arrival window: {TimeSpan.FromSeconds(reinforcementSeconds):mm\\:ss}";
        return true;
    }
}
