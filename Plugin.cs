using System;
using System.Linq;
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

    public override string Name => "PlayhousePlugin";

    public override string Description =>
        "Playhouse server gameplay plugin, ported to Northwood LabAPI.";

    public override string Author => "";

    public override Version Version => new(1, 1, 15);

    public override Version RequiredApiVersion =>
        new(LabApiProperties.CompiledVersion);

    public override void Enable()
    {
        Instance = this;

        if (!Config.IsEnabled)
        {
            Logger.Info(
                "PlayhousePlugin is disabled in its configuration.");

            return;
        }

        try
        {
            Webhooks =
                new WebhookService(Config.Webhooks);

            /*
             * PluginRuntime now only manages scheduled actions.
             * Staff chat is captured through StaffChatLogPatch.
             */
            Runtime =
                PluginRuntime.Create();

            SurfaceRework =
                new SurfaceReworkController(Schematics);

            RecyclingBins =
                new RecyclingBinController(
                    Schematics,
                    Runtime);

            VendingMachines =
                new VendingMachineController(
                    Schematics,
                    Runtime);

            Objectives =
                new ObjectivePointController(
                    Schematics,
                    Runtime);

            Containment106 =
                new Containment106ObjectiveController(
                    Schematics,
                    Runtime);

            Cosmetics =
                new CosmeticService(
                    Runtime,
                    Schematics,
                    Donators);

            RainbowTags =
                new RainbowTagService(Config);

            RainbowLights =
                new RainbowLightService(Runtime);

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
                Config.GameModes
                    .AutomaticallyEnableSillySundayOnSunday &&
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

            Runtime.Repeat(
                0.25f,
                RechargePlayerItems);

            Donators.Load(
                Config.ExternalServices.DonatorsCsvPath);

            if (Config.ExternalServices.EnableStatisticsBridge)
            {
                Statistics =
                    new StatisticsBridge(
                        Config.ExternalServices
                            .StatisticsWebSocketUrl);
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

            /*
             * This applies StaffChatLogPatch along with the rest
             * of your Harmony patches.
             */
            patchManager =
                new PatchManager();

            patchManager.Apply();

            abilityKeybindSettings =
                new AbilityKeybindSettings(
                    CustomClasses);

            Logger.Info(
                $"{Name} {Version} enabled with LabAPI " +
                $"{LabApiProperties.CurrentVersion}.");
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Unable to enable {Name}: {exception}");

            Disable();
        }
    }

    private void RechargePlayerItems()
    {
        foreach (Player player in Player.ReadyList)
        {
            foreach (RadioItem radio in
                     player.Items.OfType<RadioItem>())
            {
                radio.BatteryPercent = 100;
            }

            bool refillFirearms =
                SillySunday?.NerfWar == true ||
                SillySunday?.Slaughterhouse == true ||
                SillySunday?.OhFiveRescue == true;

            if (!refillFirearms)
                continue;

            foreach (FirearmItem firearm in
                     player.Items.OfType<FirearmItem>())
            {
                firearm.StoredAmmo =
                    firearm.MaxAmmo;
            }
        }
    }

    public override void Disable()
    {
        eventHandlers?.Unregister();
        eventHandlers = null;

        /*
         * Remove Harmony patches before disposing Webhooks,
         * because StaffChatLogPatch uses the shared service.
         */
        patchManager?.Remove();
        patchManager = null;

        abilityKeybindSettings?.Dispose();
        abilityKeybindSettings = null;

        Statistics?.Dispose();
        Statistics = null;

        if (Runtime is not null)
        {
            UnityObject.Destroy(
                Runtime.gameObject);

            Runtime = null;
        }

        Webhooks?.Dispose();
        Webhooks = null;

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

        Schematics.DestroyAll();

        SurfaceRework = null;

        CustomClasses.Clear();

        BreakoutBlitz.IsEnabled = false;

        Logger.Info(
            $"{Name} disabled.");

        Instance = null;
    }
}