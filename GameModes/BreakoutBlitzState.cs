using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.GameModes;

public sealed class BreakoutBlitzState
{
    private readonly Stopwatch roundTimer = new();
    public bool IsEnabled { get; set; }
    public int ScpKills { get; set; }
    public int ClassDEscapes { get; set; }
    public int ScientistEscapes { get; set; }
    public int RequiredScpKills { get; set; } = 200;
    public int RequiredClassDEscapes { get; set; } = 5;
    public int RequiredScientistEscapes { get; set; } = 5;
    public HashSet<ushort> ProtectedPickupSerials { get; } = new();
    public System.TimeSpan RoundTime => roundTimer.Elapsed;

    public void StartRound()
    {
        Reset();
        roundTimer.Restart();
    }

    public void CleanupWorld()
    {
        foreach (Pickup pickup in Pickup.List.ToArray())
            if (!ProtectedPickupSerials.Contains(pickup.Serial)) pickup.Destroy();
        foreach (Ragdoll ragdoll in Ragdoll.List.ToArray()) ragdoll.Destroy();
    }

    public void Reset()
    {
        ScpKills = 0;
        ClassDEscapes = 0;
        ScientistEscapes = 0;
        ProtectedPickupSerials.Clear();
        roundTimer.Reset();
    }
}
