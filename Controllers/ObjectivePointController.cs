using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Events.Arguments.PlayerEvents;
using Logger = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using PlayhousePlugin.Components;
using PluginLightBlink = global::PlayhousePlugin.Components.LightBlink;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Runtime;
using UnityEngine;
using PlayhousePlugin.Commands;

namespace PlayhousePlugin.Controllers;

public sealed class ObjectivePointController : IDisposable
{
    public const int TotalObjectives = 6;

    private static readonly System.Random Random = new();

    private readonly SchematicService schematics;
    private readonly PluginRuntime runtime;
    private readonly List<ObjectivePointComponent> objectives = new();
    private readonly Dictionary<ushort, ObjectivePointComponent> buttons = new();
    private readonly List<ScheduledHandle> timeline = new();

    private ScheduledHandle? objectiveLoop;
    private ScheduledHandle? yellowFadeLoop;
    private readonly Dictionary<LightsController, Color> originalLightColors = new();

    public ObjectivePointController(SchematicService schematics, PluginRuntime runtime)
    {
        this.schematics = schematics;
        this.runtime = runtime;
    
        ObjectivePlacementEditor.Configure(schematics);
    }

    public int ObjectivesCaptured { get; private set; }
    public bool RapidSpawnWaves { get; private set; }
    public bool FailedObjectives { get; private set; }
    public bool DisableElevators { get; private set; }
    public Team WinningTeam { get; private set; } = Team.Dead;
    public IReadOnlyList<ObjectivePointComponent> Objectives => objectives;

    public void Spawn()
    {
        Destroy();

        ObjectiveConfig config =
            PlayhousePlugin.Instance?.Config.MapFeatures.Objectives
            ?? new ObjectiveConfig();

        if (config.SpawnAllObjectives)
        {
            SpawnAllEntrance();
            SpawnAllHeavy();

            Logger.Warn(
                $"[Objectives] DEBUG MODE: Spawned all {objectives.Count} configured terminals.");
        }
        else
        {
            SpawnEntrance(config.EntranceObjectiveCount);
            SpawnHeavy(config.HeavyObjectiveCount);

            Logger.Info(
                $"[Objectives] Spawned {objectives.Count}/{TotalObjectives} terminals.");
        }

        float interval = Math.Max(0.05f, config.UpdateInterval);
        objectiveLoop = runtime.Repeat(interval, Tick);
    }

    public void StartTimeline()
    {
        ClearTimeline();

        // Initial warning: 10 seconds after the timeline starts.
        timeline.Add(runtime.Schedule(10f, () =>
        {
            Announcer.Message(
                "pitch_0.3 .g5 .g5 pitch_0.95 alert alert . facility will attempt a site wide decontamination . all decontamination terminals must be enabled in 18 minutes pitch_0.3 .g5 .g5",
                string.Empty,
                true,
                0,
                1);
        
            foreach (PluginLightBlink light in PluginLightBlink.Instances.ToArray())
            {
                if (light != null)
                    light.ForceFlicker(5f);
            }
        
            foreach (Player player in Player.ReadyList)
            {
                player.SendHint(
                    "<color=red>The facility will try to decontaminate in 18 minutes.\nAll six terminals must be active before then!</color>",
                    10f);
            }
        }));

        // 10 minutes remaining.
        timeline.Add(runtime.Schedule(490f, () =>
        {
            Server.SendBroadcast(
                "<color=red><b><i>Attempted Decontamination in 10 Minutes</i></b></color>",
                6);

            Announcer.Message(
                "pitch_0.6 .g6 .g6 pitch_0.95 warning . attempted facility decontamination will start in 10 minutes pitch_0.6 .g6 .g6",
                string.Empty,
                true,
                0,
                1);
        }));

        // 5 minutes remaining.
        timeline.Add(runtime.Schedule(790f, () =>
        {
            Server.SendBroadcast(
                "<color=red><b><i>Attempted Decontamination in 5 Minutes</i></b></color>",
                6);

            Announcer.Message(
                "pitch_0.6 .g6 .g6 pitch_0.95 danger danger . attempted facility decontamination will start in 5 minutes pitch_0.6 .g6 .g6",
                string.Empty,
                true,
                0,
                1);
        }));

        // 3 minutes remaining.
        timeline.Add(runtime.Schedule(910f, () =>
        {
            Server.SendBroadcast(
                "<color=red><b><i>Attempted Decontamination in 3 Minutes</i></b></color>",
                6);

            Announcer.Message(
                "pitch_0.6 .g6 .g6 pitch_0.95 warning . attempted facility decontamination will start in 3 minutes pitch_0.6 .g6 .g6",
                string.Empty,
                true,
                0,
                1);
        }));

        // At 18 minutes, determine whether decontamination succeeds or fails.
        timeline.Add(runtime.Schedule(1090f, Resolve));
    }

    public void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        if (!buttons.TryGetValue(
                ev.Pickup.Serial,
                out ObjectivePointComponent? objective))
        {
            return;
        }

        ev.IsAllowed = false;
        ev.Pickup.IsInUse = false;

        Logger.Info(
            $"[Objectives] Terminal button used. Player={ev.Player.Nickname}, " +
            $"Role={ev.Player.Role}, Serial={ev.Pickup.Serial}.");

        objective.TryActivate(ev.Player);
    }

    internal void RegisterButton(
        ObjectivePointComponent objective,
        ushort oldSerial,
        ushort newSerial)
    {
        if (oldSerial != 0)
            buttons.Remove(oldSerial);

        if (newSerial != 0)
            buttons[newSerial] = objective;
    }

    public bool CanUseElevator(Player player)
    {
        if (!DisableElevators)
            return true;

        player.SendHint(
            "<color=red>Elevators are disabled during decontamination.</color>",
            3f);

        return false;
    }

    internal void ObjectiveCaptured(ObjectivePointComponent objective)
    {
        if (objective.HasReportedCapture)
            return;

        objective.HasReportedCapture = true;
        ObjectivesCaptured++;

        if (RapidSpawnWaves)
            return;

        if (ObjectivesCaptured >= TotalObjectives)
        {
            Announcer.Message(
                "6 of 6 decontamination terminals online all terminals have been successfully engaged completing decontamination sequence Heavy and Entrance Zone will proceed with Decontamination at 18 minutes",
                string.Empty,
                true,
                0,
                1);
        }
        else
        {
            Announcer.Message(
                $"{ObjectivesCaptured} of 6 decontamination terminals online",
                string.Empty,
                true,
                0,
                1);
        }

        Logger.Info(
            $"[Objectives] Terminal captured ({ObjectivesCaptured}/{TotalObjectives}).");
    }

    private void Tick()
    {
        for (int i = objectives.Count - 1; i >= 0; i--)
        {
            ObjectivePointComponent objective = objectives[i];

            if (objective.IsDestroyed)
            {
                objectives.RemoveAt(i);
                continue;
            }

            objective.Tick(FailedObjectives);
        }
    }

    private void SpawnEntrance(int count)
    {
        var possibleRooms = new List<(Room Room, Placement[] Placements)>();

        foreach (Room room in Room.List)
        {
            if (room.Zone != FacilityZone.Entrance)
                continue;

            Placement[]? placements = GetEntrancePlacements(room);

            if (placements is not null && placements.Length > 0)
                possibleRooms.Add((room, placements));
        }

        SpawnRandom(possibleRooms, count);
    }

    private void SpawnHeavy(int count)
    {
        var possibleRooms = new List<(Room Room, Placement[] Placements)>();

        foreach (Room room in Room.List)
        {
            if (room.Zone != FacilityZone.HeavyContainment)
                continue;

            Placement[]? placements = GetHeavyPlacements(room);

            if (placements is not null && placements.Length > 0)
                possibleRooms.Add((room, placements));
        }

        SpawnRandom(possibleRooms, count);
    }

    private void SpawnRandom(
        List<(Room Room, Placement[] Placements)> possibleRooms,
        int count)
    {
        foreach ((Room room, Placement[] placements) in possibleRooms
                     .OrderBy(_ => Random.Next())
                     .Take(count))
        {
            Placement placement = placements[Random.Next(placements.Length)];
            CreateObjective(room, placement);
        }
    }

    private void SpawnAllEntrance()
    {
        foreach (Room room in Room.List)
        {
            if (room.Zone != FacilityZone.Entrance)
                continue;

            Placement[]? placements = GetEntrancePlacements(room);

            if (placements is null)
                continue;

            foreach (Placement placement in placements)
                CreateObjective(room, placement);
        }
    }

    private void SpawnAllHeavy()
    {
        foreach (Room room in Room.List)
        {
            if (room.Zone != FacilityZone.HeavyContainment)
                continue;

            string originalName = room.GameObject.name;
            string normalizedName = NormalizePrefabName(originalName);
            string checkpointName = NormalizeCheckpointName(originalName);

            Logger.Info(
                $"[Objectives] Found Heavy room. Prefab={originalName}, " +
                $"RoomName={room.Name}, Normalized={normalizedName}, " +
                $"CheckpointNormalized={checkpointName}.");

            Placement[]? placements = GetHeavyPlacements(room);

            if (placements is null || placements.Length == 0)
                continue;

            foreach (Placement placement in placements)
                CreateObjective(room, placement);
        }
    }

    private static Placement[]? GetEntrancePlacements(Room room)
    {
        string prefab = NormalizeCheckpointName(room.GameObject.name);
    
        if (EntrancePrefabLocations.TryGetValue(prefab, out Placement[]? prefabPlacements))
        return prefabPlacements;

        if (room.Name == RoomName.Unnamed)
        return GetEntranceUnnamedPlacements(room);

        return EntranceLocations.TryGetValue(room.Name, out Placement[]? placements)
        ? placements
        : null;
    }

    private static Placement[]? GetHeavyPlacements(Room room)
    {
        string originalName = room.GameObject.name;

        // Checkpoint rooms can have a meaningful (0) or (1) suffix, so try
        // that normalized name before stripping numbered instance suffixes.
        string checkpointName = NormalizeCheckpointName(originalName);

        if (HeavyLocations.TryGetValue(
                checkpointName,
                out Placement[]? checkpointPlacements))
        {
            return checkpointPlacements;
        }

        string normalizedName = NormalizePrefabName(originalName);

        if (HeavyLocations.TryGetValue(
                normalizedName,
                out Placement[]? placements))
        {
            return placements;
        }

        Logger.Warn(
            $"[Objectives] No Heavy placement configured. " +
            $"Prefab={originalName}, Normalized={normalizedName}, " +
            $"CheckpointNormalized={checkpointName}, RoomName={room.Name}.");

        return null;
    }

    private static string NormalizePrefabName(string objectName)
    {
        string name = RemoveCloneSuffix(objectName);

        // Remove a final numbered instance suffix such as " (0)" or " (1)".
        int suffixStart = name.LastIndexOf(" (", StringComparison.Ordinal);

        if (suffixStart >= 0 &&
            name.EndsWith(")", StringComparison.Ordinal))
        {
            string suffix = name.Substring(suffixStart + 2, name.Length - suffixStart - 3);

            if (int.TryParse(suffix, out _))
                name = name.Substring(0, suffixStart).Trim();
        }

        return name;
    }

    private static string NormalizeCheckpointName(string objectName)
    {
        // Remove Unity's clone suffix, but preserve checkpoint instance
        // numbers because (0) and (1) use different placements.
        return RemoveCloneSuffix(objectName);
    }

    private static string RemoveCloneSuffix(string objectName)
    {
        string name = objectName.Trim().ToUpperInvariant();

        while (name.EndsWith("(CLONE)", StringComparison.Ordinal))
        {
            name = name.Substring(0, name.Length - "(CLONE)".Length).Trim();
        }

        return name;
    }

    private static Placement[]? GetEntranceUnnamedPlacements(Room room)
    {
        string objectName = room.GameObject.name;

        bool tShape = objectName.IndexOf(
            "ENTRANCE_TShape_UNNAMED",
            StringComparison.OrdinalIgnoreCase) >= 0;

        bool xShape = objectName.IndexOf(
            "ENTRANCE_XShape_UNNAMED",
            StringComparison.OrdinalIgnoreCase) >= 0;

        bool zero = objectName.IndexOf(
            "(0)",
            StringComparison.OrdinalIgnoreCase) >= 0;

        bool one = objectName.IndexOf(
            "(1)",
            StringComparison.OrdinalIgnoreCase) >= 0;

        if (tShape && zero)
            return EntranceTShapeLocations;

        if (xShape || (tShape && one))
            return EntranceXShapeLocations;

        return null;
    }

    private void CreateObjective(Room room, Placement placement)
    {
        Vector3 position = room.Transform.TransformPoint(
            placement.Position + Vector3.down);

        Quaternion rotation =
            room.Rotation * Quaternion.Euler(placement.Rotation);

        var host = new GameObject($"objective_{objectives.Count + 1}");
        host.transform.SetPositionAndRotation(position, rotation);

        ObjectivePointComponent component =
            host.AddComponent<ObjectivePointComponent>();

        bool initialized = component.Initialize(
            this,
            schematics,
            position,
            rotation);

        if (!initialized)
        {
            UnityEngine.Object.Destroy(host);
            return;
        }

        objectives.Add(component);
        RegisterButton(component, 0, component.ButtonSerial);

        Logger.Info(
            $"[Objectives] Spawned terminal. Prefab={room.GameObject.name}, " +
            $"RoomName={room.Name}, Serial={component.ButtonSerial}, Position={position}.");
    }

    private void Resolve()
    {
        RapidSpawnWaves = true;
        FailedObjectives = ObjectivesCaptured < TotalObjectives;

        WinningTeam = FailedObjectives
            ? Team.ChaosInsurgency
            : Team.FoundationForces;

        if (FailedObjectives)
        {
            Server.SendBroadcast(
                "<color=red><b><i>Failed to prepare for decontamination.\nSite Wide Decontamination Protocol Failure.</i></b></color>",
                8);

            Announcer.Message(
                "pitch_0.6 .g6 .g6 pitch_0.95 site wide decontamination protocol failure . information systems offline . radio systems offline . warhead system failure . detonation in 7 minutes pitch_0.6 .g6 .g6",
                string.Empty,
                true,
                0,
                1);

            RespawnWaves.PrimaryChaosWave?.InitiateRespawn();

            timeline.Add(runtime.Schedule(420f, () =>
            {
                if (Warhead.IsDetonated)
                    return;

                if (!Warhead.IsDetonationInProgress)
                    Warhead.Start();

                Warhead.IsLocked = true;

                Server.SendBroadcast(
                    "<color=red><b><i>Auto Nuke started and cannot be turned off!\nESCAPE THE FACILITY!</i></b></color>",
                    10);

                Announcer.Message(
                    "automatic warhead sequence engaged . warhead controls locked . evacuation is advised",
                    string.Empty,
                    true,
                    0,
                    1);
            }));

            return;
        }

        // Successful terminals: the 1-minute warning starts here.
        Server.SendBroadcast(
            "<color=red><b><i>Decontamination in 1 Minute!</i></b></color>",
            8);

        Announcer.Message(
            "pitch_0.6 .g6 .g6 pitch_0.85 danger danger . facility decontamination in 1 minute . evacuate now . pitch_0.2 .g4 yd_2.5 .g4 yd_2.5 .g4",
            string.Empty,
            true,
            0,
            1);

        RespawnWaves.PrimaryMtfWave?.InitiateRespawn();

        timeline.Add(runtime.Schedule(30f, () =>
        {
            StartFadeToYellow();

            Server.SendBroadcast(
                "<color=red><b><i>30 Seconds to Decontamination</i></b></color>",
                6);
        }));

        timeline.Add(runtime.Schedule(40f, () =>
        {
            Server.SendBroadcast(
                "<color=red><b><i>20 Seconds to Decontamination</i></b></color>",
                6);
        }));

        timeline.Add(runtime.Schedule(50f, StartFinalCountdown));
    }

    private void StartFinalCountdown()
    {
        Announcer.Message(
            "10 pitch_0.95 . 9 . 8 . 7 . 6 . 5 . 4 . 3 . 2 . 1",
            string.Empty,
            true,
            0,
            1);

        for (int seconds = 10; seconds >= 1; seconds--)
        {
            int remaining = seconds;
            float delay = 10 - seconds;

            timeline.Add(runtime.Schedule(delay, () =>
            {
                string suffix = remaining == 1 ? string.Empty : "s";

                Server.SendBroadcast(
                    $"<color=red><b><i>{remaining} Second{suffix} to Decontamination</i></b></color>",
                    1);
            }));
        }

        timeline.Add(runtime.Schedule(10f, BeginDecon));
    }


    private void StartFadeToYellow()
    {
        yellowFadeLoop?.Dispose();
        yellowFadeLoop = null;
        originalLightColors.Clear();

        LightsController[] affectedLights = Room.List
            .Where(room =>
                room.Zone == FacilityZone.HeavyContainment ||
                room.Zone == FacilityZone.Entrance)
            .SelectMany(room => room.AllLightControllers)
            .Where(controller => controller is not null)
            .Distinct()
            .ToArray();

        foreach (LightsController controller in affectedLights)
            originalLightColors[controller] = controller.OverrideLightsColor;

        const int totalSteps = 300;
        const float interval = 0.1f;
        int step = 0;
        Color targetColor = Color.yellow;

        yellowFadeLoop = runtime.Repeat(interval, () =>
        {
            step++;
            float progress = Mathf.Clamp01(step / (float)totalSteps);

            foreach (LightsController controller in affectedLights)
            {
                if (!originalLightColors.TryGetValue(controller, out Color startColor))
                    continue;

                controller.OverrideLightsColor = Color.Lerp(startColor, targetColor, progress);
            }

            if (step < totalSteps)
                return;

            yellowFadeLoop?.Dispose();
            yellowFadeLoop = null;

            Logger.Info("[Objectives] Heavy and Entrance Zone lights finished fading to yellow.");
        });
    }

    private void BeginDecon()
    {
        DisableElevators = true;

        Server.SendBroadcast(
            "<color=red><b><i>Decontamination Has Started</i></b></color>",
            5);

        Announcer.Message(
            "danger . site wide decontamination sequence has begun",
            string.Empty,
            true,
            0,
            1);

        foreach (Player player in Player.ReadyList)
        {
            if (!player.IsAlive)
                continue;

            if (player.Zone != FacilityZone.HeavyContainment &&
                player.Zone != FacilityZone.Entrance)
            {
                continue;
            }

            player.Kill("Facility decontamination", string.Empty);
        }
    }

    private void ClearTimeline()
    {
        foreach (ScheduledHandle handle in timeline)
            handle.Dispose();

        timeline.Clear();
    }

    public void Destroy()
    {
        objectiveLoop?.Dispose();
        objectiveLoop = null;

        yellowFadeLoop?.Dispose();
        yellowFadeLoop = null;
        originalLightColors.Clear();

        ClearTimeline();

        foreach (ObjectivePointComponent objective in objectives.ToArray())
            objective.Dispose();

        objectives.Clear();
        buttons.Clear();

        ObjectivesCaptured = 0;
        RapidSpawnWaves = false;
        FailedObjectives = false;
        DisableElevators = false;
        WinningTeam = Team.Dead;
    }

    public void Dispose()
    {
        Destroy();
    }

    private readonly struct Placement
    {
        public Placement(Vector3 position, Vector3 rotation)
        {
            Position = position;
            Rotation = rotation;
        }

        public Vector3 Position { get; }
        public Vector3 Rotation { get; }
    }

    private static readonly Dictionary<RoomName, Placement[]> EntranceLocations = new()
    {
        [RoomName.EzOfficeLarge] = new[]
        {
            new Placement(
                new Vector3(-7.3f, 1.9f, -1.8f),
                new Vector3(0f, 90f, 0f))
        },

        [RoomName.EzIntercom] = new[]
        {
            new Placement(
                new Vector3(-0.1f, 2f, 1.8f),
                new Vector3(0f, 180f, 0f))
        }
    };

    private static readonly Dictionary<string, Placement[]> EntrancePrefabLocations =
    new(StringComparer.OrdinalIgnoreCase)
    {
        ["EZ_HCZ_CHECKPOINT PART"] = new[]
        {
            new Placement(
                new Vector3(-2.912f, 1.965f, 4.162f),
                new Vector3(0f, 58.599f, 0f))
        },

        ["EZ_UPSTAIRS"] = new[]
        {
            new Placement(
                new Vector3(7.494f, 4.815f, 4.268f),
                new Vector3(0f, 269.602f, 0f)),

            new Placement(
                new Vector3(4.283f, 1.96f, 2.14f),
                new Vector3(0f, 269.497f, 0f))
        },

        ["EZ_PCS_SMALL"] = new[]
        {
            new Placement(
                new Vector3(7.412f, 0.532f, 6.243f),
                new Vector3(0f, 268.798f, 0f))
        },
    };

    private static readonly Placement[] EntranceTShapeLocations =
    {
        new Placement(
            new Vector3(2.9f, 1.9f, -2.9f),
            new Vector3(0f, 270f, 0f))
    };

    private static readonly Placement[] EntranceXShapeLocations =
    {
        new Placement(
            new Vector3(2.9f, 1.9f, -2.9f),
            new Vector3(0f, 315f, 0f))
    };

    // Heavy contains no RoomName.Unnamed fallback. Only these known prefabs can spawn terminals.
    private static readonly Dictionary<string, Placement[]> HeavyLocations =
        new(StringComparer.OrdinalIgnoreCase)
        {
            // Both HCZ/EZ checkpoint rooms use the same prefab name in the current game.
            // They are intentionally excluded here so the two old per-instance placements are not
            // duplicated into both checkpoint rooms.

            ["HCZ_MICROHID_NEW"] = new[]
            {
                new Placement(
                    new Vector3(-4.872f, 2.044f, 2.452f),
                    new Vector3(0f, 179.099f, 0f))
            },

            ["HCZ_106_REWORK"] = new[]
            {
                new Placement(
                    new Vector3(20.192f, 1.963f, 3.355f),
                    new Vector3(0f, 179.302f, 0f))
            },

            ["HCZ_049"] = new[]
            {
                new Placement(
                    new Vector3(-1.75f, 1.95f, 2.1f),
                    new Vector3(0f, 180f, 0f))
            },

            ["HCZ_TARMORY"] = new[]
            {
                new Placement(
                    new Vector3(-0.43f, 2.257f, 1.849f),
                    new Vector3(0f, 271.398f, 0f))
            },

            ["HCZ_SERVERROOM"] = new[]
            {
                new Placement(
                    new Vector3(3.859f, 1.983f, -6.827f),
                    new Vector3(0f, 0.6f, 0f))
            },

            ["HCZ_NUKE"] = new[]
            {
                new Placement(
                    new Vector3(0.43f, 1.95f, -4.7f),
                    new Vector3(0f, 45f, 0f)),

                new Placement(
                    new Vector3(0.57f, 2.14f, 4.781f),
                    new Vector3(0f, 131.3f, 0f))
            },

            ["HCZ_CROSSROOM_WATER"] = new[]
            {
                new Placement(
                    new Vector3(-2.074f, 1.96f, 6.582f),
                    new Vector3(0f, 135.7f, 0f))
            },

            ["HCZ_TESTROOM"] = new[]
            {
                new Placement(
                    new Vector3(0.049f, 1.96f, -7.252f),
                    new Vector3(0f, 0.3f, 0f))
            },

            ["HCZ_INTERSECTION_RAMP"] = new[]
            {
                new Placement(
                    new Vector3(2.201f, 1.96f, 2.323f),
                    new Vector3(0f, 0.3f, 0f)),

                new Placement(
                    new Vector3(-2.738f, 2.342f, -2.075f),
                    new Vector3(0f, 360f, 0f))
            },

            ["HCZ_127"] = new[]
            {
                new Placement(
                    new Vector3(-0.221f, 1.969f, -1.976f),
                    new Vector3(0f, 230f, 0f))
            },

            ["HCZ_939"] = new[]
            {
                new Placement(
                    new Vector3(6.442f, 1.96f, 1.239f),
                    new Vector3(0f, 180.219f, 0f))
            },
        };
}