using System.Collections.Generic;
using System.ComponentModel;

namespace PlayhousePlugin;

public sealed class Config
{
    [Description("Whether PlayhousePlugin is enabled.")]
    public bool IsEnabled { get; set; } = true;

    public bool UseCustomSequence { get; set; }

    public float TagInterval { get; set; } = 0.5f;

    public List<string> ActiveGroups { get; set; } =
        new() { "owner", "admin", "moderator" };

    public List<string> CustomSequence { get; set; } =
        new() { "red", "orange", "yellow", "green", "blue_green", "magenta" };

    public WebhookConfig Webhooks { get; set; } = new();

    public ExternalServicesConfig ExternalServices { get; set; } = new();

    public MessageConfig Messages { get; set; } = new();

    public GameModeConfig GameModes { get; set; } = new();

    public MapFeatureConfig MapFeatures { get; set; } = new();

    public CosmeticConfig Cosmetics { get; set; } = new();
}

public sealed class CosmeticConfig
{
    public bool EnablePets { get; set; } = true;

    public bool EnableHats { get; set; }
}

public sealed class MapFeatureConfig
{
    [Description("Spawns the legacy ProjectMER Gate A/Gate B surface rework while waiting for players.")]
    public bool EnableSurfaceRework { get; set; } = true;

    public bool EnableObjectives { get; set; } = true;

    [Description("Settings for the six decontamination objective terminals.")]
    public ObjectiveConfig Objectives { get; set; } = new();

    public bool EnableContainment106Objective { get; set; } = true;

    public bool EnableRecyclingBins { get; set; } = true;

    public bool EnableVendingMachines { get; set; } = true;
}

public sealed class ObjectiveConfig
{
    [Description("Debug option: spawn every configured objective terminal instead of the normal random amount.")]
    public bool SpawnAllObjectives { get; set; } = false;

    [Description("ProjectMER schematic name used for each objective terminal.")]
    public string SchematicName { get; set; } = "Terminal";

    [Description("Number of objective terminals spawned in Entrance Zone.")]
    public int EntranceObjectiveCount { get; set; } = 2;

    [Description("Number of objective terminals spawned in Heavy Containment Zone.")]
    public int HeavyObjectiveCount { get; set; } = 4;

    [Description("Maximum distance in meters from the objective used for capturing and contesting.")]
    public float CaptureRadius { get; set; } = 2.5f;

    [Description("Total capture progress required before a terminal is enabled.")]
    public float CaptureRequirement { get; set; } = 200f;

    [Description("Base capture progress added during each objective update.")]
    public float CaptureRate { get; set; } = 1.5f;

    [Description("Capture progress removed during each objective update when nobody is capturing.")]
    public float DecayRate { get; set; } = 0.5f;

    [Description("How often objective capture logic updates, in seconds.")]
    public float UpdateInterval { get; set; } = 0.25f;

    [Description("Allows any living human role to activate and capture objectives instead of only Foundation forces.")]
    public bool AllowAllHumansToCapture { get; set; }

    [Description("Pickup item spawned as the terminal interaction button. Coin is recommended.")]
    public string ButtonItem { get; set; } = "Coin";

    [Description("Local X position of the interaction pickup relative to the terminal schematic.")]
    public float ButtonPositionX { get; set; } = -0.116f;

    [Description("Local Y position of the interaction pickup relative to the terminal schematic.")]
    public float ButtonPositionY { get; set; } = 0.237f;

    [Description("Local Z position of the interaction pickup relative to the terminal schematic.")]
    public float ButtonPositionZ { get; set; } = 0.073f;

    [Description("Local X rotation added to the interaction pickup.")]
    public float ButtonRotationX { get; set; }

    [Description("Local Y rotation added to the interaction pickup.")]
    public float ButtonRotationY { get; set; } = -90f;

    [Description("Local Z rotation added to the interaction pickup.")]
    public float ButtonRotationZ { get; set; } = 90f;

    [Description("X scale of the terminal interaction pickup.")]
    public float ButtonScaleX { get; set; } = 0.15f;

    [Description("Y scale of the terminal interaction pickup.")]
    public float ButtonScaleY { get; set; } = 0.15f;

    [Description("Z scale of the terminal interaction pickup.")]
    public float ButtonScaleZ { get; set; } = 0.15f;

    [Description("Seconds after round start before the initial objective announcement.")]
    public float InitialAnnouncementDelay { get; set; } = 10f;

    [Description("Seconds after round start before the five-minute warning.")]
    public float FiveMinuteWarningDelay { get; set; } = 790f;

    [Description("Seconds after round start before objective success or failure is resolved.")]
    public float ResolutionDelay { get; set; } = 1090f;

    [Description("Delay before the automatic warhead begins after objective failure.")]
    public float FailureWarheadDelay { get; set; } = 420f;

    [Description("Delay before successful decontamination begins.")]
    public float SuccessfulDecontaminationDelay { get; set; } = 60f;

    [Description("Blinking objective light range.")]
    public float LightRange { get; set; } = 3f;

    [Description("Blinking objective light intensity.")]
    public float LightIntensity { get; set; } = 1f;
}

public sealed class GameModeConfig
{
    public bool EnableSillySunday { get; set; }

    public bool AutomaticallyEnableSillySundayOnSunday { get; set; } = true;

    [Description("Enables Breakout Blitz escape/kill objectives and automatic win restarts.")]
    public bool EnableBreakoutBlitz { get; set; }

    public bool EnableBreakoutBlitzCleanup { get; set; }

    public float BreakoutBlitzCleanupInterval { get; set; } = 60f;

    public int BreakoutBlitzRequiredScpKills { get; set; } = 200;

    public int BreakoutBlitzRequiredClassDEscapes { get; set; } = 5;

    public int BreakoutBlitzRequiredScientistEscapes { get; set; } = 5;
}

public sealed class MessageConfig
{
    public string WaitingForPlayers { get; set; } = ":hourglass: Waiting for players...";

    public string RoundStarted { get; set; } = ":arrow_forward: Round started.";

    public string RoundEnded { get; set; } = ":stop_button: Round ended. Winner: {0}.";

    public string PlayerJoined { get; set; } = ":arrow_right: **{0} joined the game.**";

    public string PlayerLeft { get; set; } = ":arrow_left: **{0} left the server.**";

    public string PlayerChangedRole { get; set; } = ":mens: {0} changed role to {1}.";

    public string PlayerKilled { get; set; } = ":skull_crossbones: **{0} killed {1}.**";

    public string PickedUpItem { get; set; } = "{0} picked up **{1}**.";

    public string DroppedItem { get; set; } = "{0} dropped **{1}**.";

    public string UsedItem { get; set; } = ":medical_symbol: {0} used {1}.";

    public string EnteredPocketDimension { get; set; } = ":door: {0} entered the pocket dimension.";

    public string LeftPocketDimension { get; set; } = ":high_brightness: {0} {1} the pocket dimension.";

    public string TriggeredTesla { get; set; } = ":zap: {0} triggered a Tesla gate.";

    public string InteractedDoor { get; set; } = ":door: {0} interacted with {1}.";

    public string CalledElevator { get; set; } = ":elevator: {0} called {1}.";

    public string UsedLocker { get; set; } = "{0} interacted with a locker.";

    public string WarheadStarted { get; set; } = ":radioactive: **Alpha-warhead countdown initiated.**";

    public string PlayerWarheadStarted { get; set; } =
        ":radioactive: **{0} started the alpha-warhead countdown.**";

    public string WarheadStopped { get; set; } =
        ":no_entry: **Warhead detonation sequence canceled.**";

    public string PlayerWarheadStopped { get; set; } =
        ":no_entry: **{0} canceled the warhead detonation sequence.**";

    public string WarheadDetonated { get; set; } =
        ":radioactive: **The Alpha-warhead has detonated.**";
}

public sealed class WebhookConfig
{
    public bool IsEnabled { get; set; }

    [Description("Discord webhook used for general game events. Leave empty to disable this destination.")]
    public string GameLogsUrl { get; set; } = string.Empty;

    [Description("Discord webhook used for damage and kill events. Leave empty to disable this destination.")]
    public string PvpLogsUrl { get; set; } = string.Empty;

    [Description("Discord webhook used for staff-chat messages. Leave empty to disable this destination.")]
    public string StaffChatUrl { get; set; } = string.Empty;

    [Description("Discord webhook used for detained-kill and suspected undetain-to-kill alerts. Leave empty to disable this destination.")]
    public string DetainedKillsUrl { get; set; } = string.Empty;

    public string Username { get; set; } = "PlayhousePlugin";

    public string AvatarUrl { get; set; } = string.Empty;
}

public sealed class ExternalServicesConfig
{
    [Description("Enables the legacy statistics/account WebSocket bridge.")]
    public bool EnableStatisticsBridge { get; set; }

    public string StatisticsWebSocketUrl { get; set; } = "ws://127.0.0.1:8765";

    [Description("CSV containing user ID, donor tier, and booster status. Empty disables CSV loading.")]
    public string DonatorsCsvPath { get; set; } = string.Empty;

    [Description("Directory containing legacy spray point CSV files.")]
    public string SpraysDirectory { get; set; } = string.Empty;
}
