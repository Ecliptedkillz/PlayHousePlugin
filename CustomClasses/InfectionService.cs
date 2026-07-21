using CustomPlayerEffects;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public static class InfectionService
{
    public static bool Infect(Player player)
    {
        var state = PlayhousePlugin.Instance?.State;
        if (state is null || !state.InfectedPlayerIds.Add(player.PlayerId)) return false;
        player.EnableEffect<Poisoned>();
        player.EnableEffect<Hemorrhage>();
        player.SendHint("<color=red>You have been infected.</color>", 3f);
        return true;
    }
}
