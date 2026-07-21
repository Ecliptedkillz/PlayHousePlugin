using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Runtime;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.Controllers;

public sealed class RecyclingBinController : IDisposable
{
    private static readonly System.Random Random = new();
    private readonly SchematicService schematics;
    private readonly PluginRuntime runtime;
    private readonly List<Bin> bins = new();
    private ScheduledHandle? loop;
    public RecyclingBinController(SchematicService schematics, PluginRuntime runtime) { this.schematics = schematics; this.runtime = runtime; }

    public void Spawn()
    {
        Destroy();
        SpawnFrom(LightLocations, FacilityZone.LightContainment, 2);
        SpawnFrom(HeavyLocations, FacilityZone.HeavyContainment, 1);
        SpawnFrom(EntranceLocations, FacilityZone.Entrance, 1);
        SpawnFrom(GateLocations, FacilityZone.Entrance, 1);
        Room? room173 = Room.List.FirstOrDefault(room => room.Name == RoomName.Lcz173);
        if (room173 is not null) SpawnBin(room173, new Vector3(8, 19.7f, 21.9f), false);
        loop = runtime.Repeat(1, RecyclePickups);
    }

    private void SpawnFrom(Dictionary<RoomName, Vector3[]> locations, FacilityZone zone, int count)
    {
        var candidates = Room.List.Where(room => room.Zone == zone && locations.ContainsKey(room.Name)).OrderBy(_ => Random.Next()).Take(count).ToArray();
        foreach (Room room in candidates) { Vector3[] positions = locations[room.Name]; SpawnBin(room, positions[Random.Next(positions.Length)], true); }
    }

    private void SpawnBin(Room room, Vector3 localPosition, bool active)
    {
        Vector3 position = room.Transform.TransformPoint(localPosition);
        Vector3 direction = room.Position - position;
        Quaternion rotation = direction.sqrMagnitude > 0.01f ? Quaternion.LookRotation(direction.normalized) : room.Rotation;
        SchematicObject? schematic = schematics.Spawn(active ? "TrashWithLight" : "Trash", position, Quaternion.Euler(0, rotation.eulerAngles.y, 0), Vector3.one);
        if (schematic is not null) bins.Add(new Bin(schematic, active));
    }

    private void RecyclePickups()
    {
        foreach (Bin bin in bins.Where(bin => bin.Active))
        foreach (Pickup pickup in Pickup.List.ToArray())
        {
            if (pickup.IsDestroyed || pickup.IsLocked || Vector3.Distance(pickup.Position, bin.Schematic.Position) > 1.1f) continue;
            bool ammo = pickup.Type is ItemType.Ammo9x19 or ItemType.Ammo556x45 or ItemType.Ammo762x39 or ItemType.Ammo12gauge or ItemType.Ammo44cal;
            Vector3 rewardPosition = bin.Schematic.Position + bin.Schematic.Rotation * Vector3.forward * 2 + Vector3.up;
            pickup.Destroy();
            if (!ammo) Pickup.Create(ItemType.Coin, rewardPosition)?.Spawn();
        }
    }

    public void Destroy()
    {
        loop?.Dispose(); loop = null;
        foreach (Bin bin in bins) schematics.Destroy(bin.Schematic);
        bins.Clear();
    }
    public void Dispose() => Destroy();

    private readonly struct Bin { public Bin(SchematicObject schematic, bool active) { Schematic = schematic; Active = active; } public SchematicObject Schematic { get; } public bool Active { get; } }
    private static readonly Dictionary<RoomName, Vector3[]> EntranceLocations = new() {
        [RoomName.EzOfficeStoried] = new[] { new Vector3(-6.8f,0,9.4f) }, [RoomName.Unnamed] = new[] { new Vector3(-3.2f,0,5), new Vector3(4.8f,0,-3) }, [RoomName.EzOfficeLarge] = new[] { new Vector3(-3.6f,0,2) } };
    private static readonly Dictionary<RoomName, Vector3[]> GateLocations = new() {
        [RoomName.EzGateB] = new[] { new Vector3(5.1f,0,1.2f), new Vector3(5.1f,0,-9.9f) }, [RoomName.EzGateA] = new[] { new Vector3(5.7f,0,-9.8f) } };
    private static readonly Dictionary<RoomName, Vector3[]> HeavyLocations = new() {
        [RoomName.Unnamed] = new[] { new Vector3(-2.9f,0,3), new Vector3(-2.7f,0,2.7f), new Vector3(2.7f,0,2.7f), new Vector3(2.7f,0,-2.7f), new Vector3(-2.7f,0,-2.7f) } };
    private static readonly Dictionary<RoomName, Vector3[]> LightLocations = new() {
        [RoomName.Lcz914] = new[] { new Vector3(-2.2f,0,-9.5f), new Vector3(-9.3f,0,-3.6f) }, [RoomName.LczAirlock] = new[] { new Vector3(0,0,1.6f) }, [RoomName.LczComputerRoom] = new[] { new Vector3(-6.7f,0,6.1f) }, [RoomName.Lcz330] = new[] { new Vector3(-2.9f,0,0) } };
}
