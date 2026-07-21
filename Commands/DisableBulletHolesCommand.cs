using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class DisableBulletHolesCommand : ICommand
{
    public string Command => "disablebh";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Toggles bullet-hole suppression.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        RuntimeState? state = PlayhousePlugin.Instance?.State;
        if (state is null) { response = "Plugin is unavailable."; return false; }
        state.BulletHolesDisabled = !state.BulletHolesDisabled;
        response = $"Bullet-hole suppression: {state.BulletHolesDisabled}.";
        return true;
    }
}
