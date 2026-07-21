using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayhousePlugin.Runtime;

namespace PlayhousePlugin.CustomClasses;

public abstract class CustomClassBase : IDisposable
{
    private readonly List<ScheduledHandle> scheduledWork = new();
    protected CustomClassBase(Player player) => Player = player;
    public Player Player { get; }
    public abstract string Name { get; }
    public virtual IReadOnlyList<AbilityBase> ActiveAbilities => Array.Empty<AbilityBase>();
    protected void Track(ScheduledHandle handle) => scheduledWork.Add(handle);
    public virtual void Escape() { }
    public virtual void Dispose()
    {
        foreach (ScheduledHandle handle in scheduledWork) handle.Dispose();
        scheduledWork.Clear();
        foreach (AbilityBase ability in ActiveAbilities)
            if (ability is IDisposable disposable) disposable.Dispose();
        if (Player is not null) Player.CustomInfo = string.Empty;
    }
}
