using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayhousePlugin.Runtime;
using UnityEngine;

namespace PlayhousePlugin.Cosmetics;

public sealed class RainbowLightService
{
    private readonly PluginRuntime runtime;
    private readonly Dictionary<Room, Color> originals = new();
    private readonly HashSet<Room> active = new();
    private ScheduledHandle? loop;

    public RainbowLightService(PluginRuntime runtime) => this.runtime = runtime;

    public void StartSundayRooms() => SetRooms(Room.List.Where(IsSundayRoom));

    public void StartAllRooms() => SetRooms(Room.List);

    public void StopWarheadRooms()
    {
        foreach (Room room in active.Where(room => !IsSundayRoom(room)).ToArray())
            Restore(room);
        StopLoopIfEmpty();
    }

    public void Reset()
    {
        loop?.Cancel();
        loop = null;
        foreach (Room room in active.ToArray()) Restore(room);
        active.Clear();
        originals.Clear();
    }

    private void SetRooms(IEnumerable<Room> rooms)
    {
        foreach (Room room in rooms)
        {
            if (room.LightController is null) continue;
            if (!originals.ContainsKey(room)) originals[room] = room.LightController.OverrideLightsColor;
            active.Add(room);
        }
        loop ??= runtime.Repeat(0.05f, Tick);
    }

    private void Tick()
    {
        foreach (Room room in active.ToArray())
        {
            if (room.LightController is null) { active.Remove(room); originals.Remove(room); continue; }
            Color current = room.LightController.OverrideLightsColor;
            Color.RGBToHSV(current, out float hue, out _, out _);
            room.LightController.OverrideLightsColor = Color.HSVToRGB((hue + 0.01f) % 1f, 1f, 1f);
        }
    }

    private void Restore(Room room)
    {
        if (room.LightController is not null && originals.TryGetValue(room, out Color color)) room.LightController.OverrideLightsColor = color;
        active.Remove(room);
        originals.Remove(room);
    }

    private void StopLoopIfEmpty()
    {
        if (active.Count != 0) return;
        loop?.Cancel();
        loop = null;
    }

    private static bool IsSundayRoom(Room room)
    {
        string name = room.Name.ToString();
        return name is "LczGlassBox" or "LczPlants" or "Lcz173" || name.Contains("Checkpoint");
    }
}
