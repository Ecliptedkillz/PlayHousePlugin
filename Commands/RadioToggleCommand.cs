using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class RadioToggleCommand : ICommand
{
    public string Command => "radiotoggle";
    public string[] Aliases => new[] { "radiot", "toggleradio" };
    public string Description => "Toggles removing radios when players spawn.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (state is null) { response = "Plugin is unavailable."; return false; }
        state.WipeRadiosOnSpawn = !state.WipeRadiosOnSpawn;
        response = $"Radio removal on spawn: {state.WipeRadiosOnSpawn}.";
        return true;
    }
}
