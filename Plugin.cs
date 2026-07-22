using System;
using System.Linq;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using LabApi.Loader.Features.Plugins;
using PlayhousePlugin.Controllers;
using PlayhousePlugin.Cosmetics;
using PlayhousePlugin.CustomClasses;
using PlayhousePlugin.Events;
using PlayhousePlugin.External;
using PlayhousePlugin.GameModes;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Patches;
using PlayhousePlugin.Runtime;
using PlayhousePlugin.Settings;
using PlayhousePlugin.Webhooks;
using UnityObject = UnityEngine.Object;

namespace PlayhousePlugin;

public sealed class PlayhousePlugin : Plugin<Config>
{
    public static PlayhousePlugin? Instance { get; private set; }

    public WebhookService? Webhooks { get; private set; }
    public DonatorRepository Donators { get; } = new();
    public StatisticsBridge? Statistics { get; private set; }
    public RuntimeState State { get; } = new();
    public PluginRuntime? Runtime { get; private set; }
    public BreakoutBlitzState BreakoutBlitz { get; } = new();
    public SillySundayService? SillySunday { get; private set; }
    public SchematicService Schematics { get; } = new();
    public CustomClassManager CustomClasses { get; } = new();
    public SurfaceReworkController? SurfaceRework { get; private set; }
    public RecyclingBinController? RecyclingBins { get; private set; }
    public VendingMachineController? VendingMachines { get; private set; }
    public ObjectivePointController? Objectives { get; private set; }
    public Containment106ObjectiveController? Containment106 { get; private set; }
    public CosmeticService? Cosmetics { get; private set; }
    public RainbowTagService? RainbowTags { get; private set; }
    public RainbowLightService? RainbowLights { get; private set; }
    public SprayService? Sprays { get; private set; }

    private CoreEventHandlers? eventHandlers;
    private PatchManager? patchManager;
    private AbilityKeybindSettings? abilityKeybindSettings;
    private ScheduledHandle? mapFeatureSpawnHandle;
    private bool serverEventsRegistered;
    private bool disabling;

    public override string Name => "PlayhousePlugin";

    public override string Description =>
        "Playhouse server gameplay plugin, ported to Northwood LabAPI.";

    public override string Author => "";

    public override Version Version => new(1, 1, 19);

    public override Version RequiredApiVersion =>
        new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        Instance = this;
        disabling = false;

        if (!Config.IsEnabled)
        {
            Logger.Info(
                "PlayhousePlugin is disabled in its configuration.");
            return;
        }

        try
        {
            Webhooks = new WebhookService(Config.Webhooks);
            Runtime = PluginRuntime.Create();

            SurfaceRework = new SurfaceReworkController(Schematics);
            RecyclingBins = new RecyclingBinController(Schematics, Runtime);
            VendingMachines = new VendingMachineController(Schematics, Runtime);
            Objectives = new ObjectivePointController(Schematics, Runtime);
            Containment106 =
                new Containment106ObjectiveController(Schematics, Runtime);
            Cosmetics =
                new CosmeticService(Runtime, Schematics, Donators);
            RainbowTags = new RainbowTagService(Config);
            RainbowLights = new RainbowLightService(Runtime);
            Sprays =
                new SprayService(
                    Config.ExternalServices.SpraysDirectory,
                    Donators);

            BreakoutBlitz.IsEnabled =
                Config.GameModes.EnableBreakoutBlitz;
            BreakoutBlitz.RequiredScpKills =
                Config.GameModes.BreakoutBlitzRequiredScpKills;
            BreakoutBlitz.RequiredClassDEscapes =
                Config.GameModes.BreakoutBlitzRequiredClassDEscapes;
            BreakoutBlitz.RequiredScientistEscapes =
                Config.GameModes.BreakoutBlitzRequiredScientistEscapes;

            bool automaticallyEnableSillySunday =
                Config.GameModes.AutomaticallyEnableSillySundayOnSunday &&
                DateTime.Now.DayOfWeek == DayOfWeek.Sunday;

            SillySunday =
                new SillySundayService(Runtime)
                {
                    IsEnabled =
                        Config.GameModes.EnableSillySunday ||
                        automaticallyEnableSillySunday,
                };

            if (Config.GameModes.EnableBreakoutBlitzCleanup)
            {
                Runtime.Repeat(
                    Config.GameModes.BreakoutBlitzCleanupInterval,
                    BreakoutBlitz.CleanupWorld);
            }

            Runtime.Repeat(0.25f, RechargePlayerItems);

            Donators.Load(Config.ExternalServices.DonatorsCsvPath);

            if (Config.ExternalServices.EnableStatisticsBridge)
            {
                Statistics =
                    new StatisticsBridge(
                        Config.ExternalServices.StatisticsWebSocketUrl);
            }

            eventHandlers =
                new CoreEventHandlers(
                    Webhooks,
                    Statistics,
                    State,
                    Config.Messages,
                    Runtime,
                    BreakoutBlitz,
                    CustomClasses);

            eventHandlers.Register();
            RegisterServerEvents();

            patchManager = new PatchManager();
            patchManager.Apply();

            abilityKeybindSettings =
                new AbilityKeybindSettings(CustomClasses);

            Logger.Info(
                $"{Name} {Version} enabled with LabAPI " +
                $"{LabApiProperties.CurrentVersion}.");
        }
        catch (Exception exception)
        {
            Logger.Error($"Unable to enable {Name}: {exception}");
            Disable();
        }
    }

    private void RegisterServerEvents()
    {
        if (serverEventsRegistered)
            return;

        ServerEvents.MapGenerated += OnMapGenerated;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.RoundEnded += OnRoundEnded;
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundRestarted += OnRoundRestarted;

        serverEventsRegistered = true;
    }

    private void UnregisterServerEvents()
    {
        if (!serverEventsRegistered)
            return;

        ServerEvents.MapGenerated -= OnMapGenerated;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.RoundEnded -= OnRoundEnded;
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundRestarted -= OnRoundRestarted;

        serverEventsRegistered = false;
    }

    private void OnMapGenerated(MapGeneratedEventArgs ev)
    {
        // Prepare map features after room generation. Do not start the
        // objective timeline here because the round has not started yet.
        ScheduleMapFeatureSpawn(1f, "map generated", false);
    }

    private void OnRoundStarted()
    {
        // This replaces the MapGenerated spawn callback, performs one final
        // clean spawn, and then starts the CASSIE objective timeline.
        ScheduleMapFeatureSpawn(1f, "round started", true);
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        CleanupRoundMapFeatures("round ended");
    }

    private void OnWaitingForPlayers()
    {
        CleanupRoundMapFeatures("waiting for players");
    }

    private void OnRoundRestarted()
    {
        CleanupRoundMapFeatures("round restarted");
    }

    private void ScheduleMapFeatureSpawn(
        float delaySeconds,
        string reason,
        bool startObjectiveTimeline)
    {
        if (disabling || Runtime is null)
            return;

        mapFeatureSpawnHandle?.Dispose();
        mapFeatureSpawnHandle = Runtime.Schedule(
            delaySeconds,
            () =>
            {
                mapFeatureSpawnHandle = null;

                if (disabling)
                    return;

                try
                {
                    // Each Spawn method first destroys the previous round's
                    // objects, so duplicate event calls cannot duplicate them.
                    VendingMachines?.Spawn();
                    Objectives?.Spawn();

                    // Spawn() clears the old objective timeline, so this must
                    // always be called after Spawn(), never before it.
                    if (startObjectiveTimeline)
                    {
                        Objectives?.StartTimeline();
                        Logger.Info(
                            "[Objectives] CASSIE timeline scheduled after round start.");
                    }

                    Logger.Info(
                        $"[Map Features] Vending machines and terminals " +
                        $"started spawning after {reason}.");
                }
                catch (Exception exception)
                {
                    Logger.Error(
                        $"[Map Features] Failed to spawn vending machines " +
                        $"or terminals after {reason}: {exception}");
                }
            });
    }

    private void CleanupRoundMapFeatures(string reason)
    {
        mapFeatureSpawnHandle?.Dispose();
        mapFeatureSpawnHandle = null;

        try
        {
            VendingMachines?.Destroy();
            Objectives?.Destroy();

            Logger.Info(
                $"[Map Features] Cleaned vending machines and terminals: {reason}.");
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"[Map Features] Cleanup failed during {reason}: {exception}");
        }
    }

    private void RechargePlayerItems()
    {
        foreach (Player player in Player.ReadyList)
        {
            foreach (RadioItem radio in player.Items.OfType<RadioItem>())
                radio.BatteryPercent = 100;

            bool refillFirearms =
                SillySunday?.NerfWar == true ||
                SillySunday?.Slaughterhouse == true ||
                SillySunday?.OhFiveRescue == true;

            if (!refillFirearms)
                continue;

            foreach (FirearmItem firearm in player.Items.OfType<FirearmItem>())
                firearm.StoredAmmo = firearm.MaxAmmo;
        }
    }

    public override void Disable()
    {
        if (disabling)
            return;

        disabling = true;

        UnregisterServerEvents();
        CleanupRoundMapFeatures("plugin disabled");

        eventHandlers?.Unregister();
        eventHandlers = null;

        patchManager?.Remove();
        patchManager = null;

        abilityKeybindSettings?.Dispose();
        abilityKeybindSettings = null;

        Statistics?.Dispose();
        Statistics = null;

        // Dispose controllers before destroying the runtime because their
        // cleanup methods cancel ScheduledHandle instances owned by it.
        RecyclingBins?.Dispose();
        RecyclingBins = null;

        VendingMachines?.Dispose();
        VendingMachines = null;

        Objectives?.Dispose();
        Objectives = null;

        Containment106?.Dispose();
        Containment106 = null;

        Cosmetics?.Dispose();
        Cosmetics = null;

        RainbowTags?.Dispose();
        RainbowTags = null;

        RainbowLights?.Reset();
        RainbowLights = null;

        SillySunday?.Reset();
        SillySunday = null;

        Sprays?.Reset();
        Sprays = null;

        if (Runtime is not null)
        {
            UnityObject.Destroy(Runtime.gameObject);
            Runtime = null;
        }

        Webhooks?.Dispose();
        Webhooks = null;

        Schematics.DestroyAll();
        SurfaceRework = null;
        CustomClasses.Clear();
        BreakoutBlitz.IsEnabled = false;

        Logger.Info($"{Name} disabled.");
        Instance = null;
    }
}