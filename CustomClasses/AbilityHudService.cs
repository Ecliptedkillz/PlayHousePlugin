using System;
using System.Linq;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class AbilityHudService : IDisposable
{
    private readonly Runtime.PluginRuntime runtime;
    private readonly CustomClassManager manager;

    private bool disposed;

    public AbilityHudService(
        Runtime.PluginRuntime runtime,
        CustomClassManager manager)
    {
        this.runtime = runtime;
        this.manager = manager;

        ScheduleNextUpdate();
    }

    public void Dispose()
    {
        disposed = true;
    }

    private void ScheduleNextUpdate()
    {
        if (disposed)
            return;

        UpdateHud();

        runtime.Schedule(
            0.8f,
            ScheduleNextUpdate);
    }

    private void UpdateHud()
    {
        foreach (PlayerClassState state in manager.Active.ToArray())
        {
            Player player = state.Player;

            if (!Player.ReadyList.Contains(player))
                continue;

            if (!player.IsAlive)
                continue;

            if (!manager.TryGetSelectedAbility(
                    player,
                    out AbilityBase? ability) ||
                ability is null)
            {
                continue;
            }

            player.SendHint(
                ability.GenerateHud(),
                1f);
        }
    }
}