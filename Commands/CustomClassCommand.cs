using System;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using PlayhousePlugin.CustomClasses;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class CustomClassCommand : ICommand
{
    public string Command => "customclass";

    public string[] Aliases => new[]
    {
        "cc"
    };

    public string Description => "Assigns a Playhouse custom class.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.customclass"))
        {
            response = "Missing permission: at.customclass";
            return false;
        }

        if (arguments.Count != 2)
        {
            response =
                "Usage: customclass <player ID/name/Steam ID> <class name|clear>\n" +
                "Example: customclass 2 medic";
            return false;
        }

        string playerArgument = arguments.At(0);
        string className = arguments.At(1).Trim().ToLowerInvariant();

        Player? player = ResolvePlayer(playerArgument);

        if (player is null)
        {
            response = $"Player not found: {playerArgument}";
            return false;
        }

        PlayhousePlugin? plugin = PlayhousePlugin.Instance;

        if (plugin?.Runtime is null)
        {
            response = "The Playhouse custom-class runtime is unavailable.";
            return false;
        }

        if (className == "clear")
        {
            plugin.CustomClasses.Remove(player);

            response =
                $"Cleared {player.Nickname}'s custom class " +
                $"(Player ID: {player.PlayerId}).";

            return true;
        }

        CustomClassBase? customClass = CreateCustomClass(
            className,
            player,
            plugin);

        if (customClass is null)
        {
            response =
                $"Unknown custom class: {className}\n" +
                "Use the exact class name or one of its aliases.";

            return false;
        }

        // Remove the player's previous custom class first.
        plugin.CustomClasses.Remove(player);

        plugin.CustomClasses.Assign(player, customClass);

        response =
            $"Assigned {customClass.Name} to {player.Nickname} " +
            $"(Player ID: {player.PlayerId}).";

        return true;
    }

    private static Player? ResolvePlayer(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.Trim();

        // Resolve the numeric ID shown in Remote Admin.
        if (int.TryParse(input, out int playerId))
        {
            Player? playerById = Player.List.FirstOrDefault(
                player => player.PlayerId == playerId);

            if (playerById is not null)
                return playerById;
        }

        // Resolve an exact Steam/Discord User ID.
        Player? playerByUserId = Player.List.FirstOrDefault(
            player => string.Equals(
                player.UserId,
                input,
                StringComparison.OrdinalIgnoreCase));

        if (playerByUserId is not null)
            return playerByUserId;

        // Resolve an exact nickname.
        Player? playerByExactName = Player.List.FirstOrDefault(
            player => string.Equals(
                player.Nickname,
                input,
                StringComparison.OrdinalIgnoreCase));

        if (playerByExactName is not null)
            return playerByExactName;

        // Finally, permit a partial nickname when only one player matches.
        Player[] partialMatches = Player.List
            .Where(player =>
                !string.IsNullOrWhiteSpace(player.Nickname) &&
                player.Nickname.IndexOf(
                    input,
                    StringComparison.OrdinalIgnoreCase) >= 0)
            .Take(2)
            .ToArray();

        return partialMatches.Length == 1
            ? partialMatches[0]
            : null;
    }

    private static CustomClassBase? CreateCustomClass(
        string className,
        Player player,
        PlayhousePlugin plugin)
    {
        switch (className)
        {
            case "medic":
            case "ntfmedic":
                return new NtfMedic(player, plugin.Runtime!);

            case "heavy":
            case "ntfheavy":
                return new NtfHeavy(player, plugin.Runtime!);

            case "scout":
            case "ntfscout":
                return new ScoutClass(player, false);

            case "demoman":
            case "demo":
            case "ntfdemoman":
                return new DemolitionsClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "ntfdemolitionsexpert":
            case "ntfexpert":
                return new DemolitionsClass(
                    player,
                    plugin.Runtime!,
                    false,
                    true);

            case "chaosmedic":
                return new ChaosMedic(player, plugin.Runtime!);

            case "chaosheavy":
                return new ChaosHeavy(player, plugin.Runtime!);

            case "chaosscout":
                return new ScoutClass(player, true);

            case "chaosdemo":
            case "demolitionsexpert":
                return new DemolitionsClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "chaosdemoman":
                return new DemolitionsClass(
                    player,
                    plugin.Runtime!,
                    true,
                    false);

            case "containment":
            case "containmentspecialist":
                return new ContainmentSpecialistClass(player, false);

            case "chaoscontainment":
                return new ContainmentSpecialistClass(player, true);

            case "bulldozer":
            case "chaosbulldozer":
                return new BulldozerClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "ntfbulldozer":
                return new BulldozerClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "hunter":
            case "chaoshunter":
                return new HunterClass(player, true);

            case "ntfhunter":
                return new HunterClass(player, false);

            case "exterminator":
            case "chaosexterminator":
                return new ExterminatorClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "ntfexterminator":
                return new ExterminatorClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "heretic":
            case "chaosheretic":
                return new HereticClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "ntfheretic":
                return new HereticClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "chad":
            case "classdchad":
                return new ClassDChad(player);

            case "seniorguard":
            case "senior":
                return new SeniorGuard(player);

            case "captain":
            case "ntfcaptain":
                return new NtfCaptainClass(
                    player,
                    plugin.Runtime!);

            case "sergeant":
            case "ntfsergeant":
                return new NtfSergeantClass(player);

            case "guardmanager":
                return new GuardManagerClass(player);

            case "janitor":
            case "classdjanitor":
                return new ClassDJanitorClass(player);

            case "majorscientist":
            case "majorscientistjr":
                return new MajorScientistClass(player);

            case "engineer":
            case "ntfengineer":
                return new EngineerClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "chaosengineer":
                return new EngineerClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "machinist":
            case "chaosmachinist":
                return new MachinistClass(
                    player,
                    plugin.Runtime!,
                    true);

            case "ntfmachinist":
                return new MachinistClass(
                    player,
                    plugin.Runtime!,
                    false);

            case "manager":
            case "ntfmanager":
                return new ManagerClass(player, false);

            case "chaosmanager":
                return new ManagerClass(player, true);

            case "boomer":
                return new ZombieClass(
                    player,
                    plugin.Runtime!,
                    ZombieArchetype.Boomer);

            case "medicalstudent":
            case "zombiemedic":
                return new ZombieClass(
                    player,
                    plugin.Runtime!,
                    ZombieArchetype.MedicalStudent);

            case "overclocker":
                return new ZombieClass(
                    player,
                    plugin.Runtime!,
                    ZombieArchetype.Overclocker);

            case "overdoser":
                return new ZombieClass(
                    player,
                    plugin.Runtime!,
                    ZombieArchetype.Overdoser);

            case "sprinter":
                return new ZombieClass(
                    player,
                    plugin.Runtime!,
                    ZombieArchetype.Sprinter);
            
            case "scp096":
            case "096":
            case "shyguy":
                return new SCP096CustomClass(
                    player,
                    plugin.Runtime!);

            default:
                return null;
        }
    }
}