using System;
using CommandSystem;
using LabApi.Features.Wrappers;
using PlayhousePlugin.CustomClasses;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(ClientCommandHandler))]
public sealed class ActivateAbilityCommand : ICommand
{
    public string Command => "activateability";

    public string[] Aliases => new[]
    {
        "ability",
        "useability",
    };

    public string Description =>
        "Activates your selected custom-class ability.";

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
                "Your class does not have any active abilities.";

            return false;
        }

        bool result =
            manager.TryActivate(player, out response);

        AbilityDisplay.Show(
            player,
            response,
            2f,
            result ? Color.green : Color.red);

        return result;
    }
}