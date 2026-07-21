using System;
using CommandSystem;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(GameConsoleCommandHandler))]
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class FrameTimeCommand : ICommand
{
    public string Command => "frametime";
    public string[] Aliases => new[] { "ft" };
    public string Description => "Displays server frame time and approximate FPS.";
    public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
    {
        response = $"Frame time: {Time.deltaTime:F4}s ~ {(Time.deltaTime > 0 ? 1f / Time.deltaTime : 0):F1} FPS";
        return true;
    }
}
