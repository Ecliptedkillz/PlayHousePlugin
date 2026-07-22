using System;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using UnityEngine;
using LabLogger = LabApi.Features.Console.Logger;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
[CommandHandler(typeof(GameConsoleCommandHandler))]
public sealed class PlayhousePluginCommand : ICommand
{
    public string Command => "playhouseplugin";
    public string[] Aliases => new[] { "pp" };
    public string Description => "PlayhousePlugin LabAPI debug commands.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.playhouseplugin"))
        {
            response = "Missing permission: at.playhouseplugin";
            return false;
        }

        if (arguments.Count < 1)
        {
            response = GetUsage();
            return false;
        }

        Player? player = Player.Get(sender);

        if (player is null)
        {
            response = "This command can only be used by an in-game player.";
            return false;
        }

        string subcommand = arguments.At(0).ToLowerInvariant();

        try
        {
            switch (subcommand)
            {
                case "room":
                    return ShowRoom(player, out response);

                case "viewpoint":
                    return ShowViewpoint(player, out response);

                case "force":
                    return ApplyForce(player, out response);

                case "envirovar":
                    string? value = Environment.GetEnvironmentVariable(
                        "GOOGLE_APPLICATION_CREDENTIALS");

                    response = string.IsNullOrWhiteSpace(value)
                        ? "GOOGLE_APPLICATION_CREDENTIALS is not set."
                        : $"GOOGLE_APPLICATION_CREDENTIALS: {value}";
                    return true;

                case "console":
                case "message":
                    player.SendConsoleMessage(
                        "[PlayhousePlugin] LabAPI debug message.",
                        "white");
                    response = "Sent a console message.";
                    return true;

                default:
                    response = $"Unknown subcommand: {subcommand}\n\n{GetUsage()}";
                    return false;
            }
        }
        catch (Exception exception)
        {
            LabLogger.Error(
                $"PlayhousePlugin command '{subcommand}' failed:\n{exception}");

            response = $"Command failed: {exception.Message}";
            return false;
        }
    }

    private static bool ShowRoom(Player player, out string response)
    {
        Room? room = player.Room;

        if (room is null)
        {
            response = "Your current room could not be determined.";
            return false;
        }

        if (!TryRaycast(player, out RaycastHit hit))
        {
            response = $"Room: {room.Name}\nNothing was found in front of you.";
            return true;
        }

        Vector3 localPosition = room.Transform.InverseTransformPoint(hit.point);

        response =
            $"Room: {room.Name}\n" +
            $"Local position: {localPosition}\n" +
            $"World position: {hit.point}";

        LabLogger.Info(response);
        return true;
    }

    private static bool ShowViewpoint(Player player, out string response)
    {
        if (!TryRaycast(player, out RaycastHit hit))
        {
            response = "Nothing was found in front of you.";
            return false;
        }

        response = $"Viewpoint: {hit.point}";
        LabLogger.Info(response);
        return true;
    }

    private static bool ApplyForce(Player player, out string response)
    {
        Rigidbody? rigidbody = player.GameObject.GetComponent<Rigidbody>();

        if (rigidbody is null)
        {
            response = "The player object does not have a Rigidbody.";
            return false;
        }

        rigidbody.AddForce(
            player.Camera.forward * 4f + Vector3.up * 2f,
            ForceMode.Impulse);

        response = "Applied force.";
        return true;
    }

    private static bool TryRaycast(Player player, out RaycastHit hit)
    {
        return Physics.Raycast(
            new Ray(player.Camera.position, player.Camera.forward),
            out hit,
            100f);
    }

    private static string GetUsage()
    {
        return
            "Usage: pp <subcommand>\n" +
            "- pp room\n" +
            "- pp viewpoint\n" +
            "- pp force\n" +
            "- pp envirovar\n" +
            "- pp console";
    }
}
