using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayhousePlugin.CustomClasses;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ChangeAbilityCommand : ICommand
{
    public string Command => "changeability";

    public string[] Aliases => new[]
    {
        "nextability",
        "cycleability",
    };

    public string Description =>
        "Changes your selected custom-class ability.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        Player? player = Player.Get(sender);
        CustomClassManager? manager =
            PlayhousePlugin.Instance?.CustomClasses;

        if (player is null || manager is null)
        {
            response =
                "You are not a custom class or have no active abilities.";

            return false;
        }

        bool result = manager.TryCycle(player, out response);

        if (result)
        {
            AbilityDisplay.Show(
                player,
                response,
                3f,
                Color.yellow);
        }
        else
        {
            AbilityDisplay.Show(
                player,
                response,
                2f,
                Color.red);
        }

        return result;
    }
}