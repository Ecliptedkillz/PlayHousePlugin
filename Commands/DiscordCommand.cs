using System;
using CommandSystem;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class DiscordCommand : ICommand
{
    public string Command => "discord";
    public string[] Aliases => Array.Empty<string>();
    public string Description => "Gives you the link to the Discord server.";

    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = "\nJoin the Discord for some server perks!\nhttps://discord.gg/";
        return true;
    }
}
