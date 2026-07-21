using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Arguments.WarheadEvents;
using PlayhousePlugin.Webhooks;
using PlayhousePlugin.External;
using PlayerStatsSystem;
using PlayhousePlugin.Runtime;
using PlayhousePlugin.GameModes;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayhousePlugin.CustomClasses;
using System.Collections.Generic;
using UnityEngine;
using CustomPlayerEffects;
using VoiceChat;
using LabApi.Events.Arguments.Scp049Events;
using LabApi.Events.Arguments.Scp096Events;
using LabApi.Events.Arguments.Scp914Events;
using LabApi.Events.Arguments.ObjectiveEvents;
using LabApi.Events.Handlers;
using Interactables.Interobjects.DoorUtils;
using PlayhousePlugin;

namespace PlayhousePlugin.Events;

public sealed class CoreEventHandlers
{
    private readonly WebhookService webhooks;
    private readonly StatisticsBridge? statistics;
    private readonly RuntimeState state;
    private readonly MessageConfig messages;
    private readonly PluginRuntime runtime;
    private readonly BreakoutBlitzState breakoutBlitz;
    private readonly CustomClassManager customClasses;
    private readonly Dictionary<int, CustomClassType> escapingClasses = new();
    private readonly HashSet<int> recentlyHitBy106 = new();
    private readonly System.Random random = new();
    private readonly Dictionary<int, string> recentlyUncuffedBy = new();
    private readonly Dictionary<int, int> generalKills = new();
    private readonly Dictionary<int, int> scpKills = new();
    private readonly Dictionary<int, int> humanKills = new();
    private int mapSpawnAttempts;

    public CoreEventHandlers(WebhookService webhooks, StatisticsBridge? statistics, RuntimeState state, MessageConfig messages, PluginRuntime runtime, BreakoutBlitzState breakoutBlitz, CustomClassManager customClasses)
    {
        this.webhooks = webhooks;
        this.statistics = statistics;
        this.state = state;
        this.messages = messages;
        this.runtime = runtime;
        this.breakoutBlitz = breakoutBlitz;
        this.customClasses = customClasses;
    }

    public void Register()
    {
        ServerEvents.WaitingForPlayers += OnWaitingForPlayers;
        ServerEvents.RoundStarted += OnRoundStarted;
        ServerEvents.RoundEnded += OnRoundEnded;
        ServerEvents.WaveRespawned += OnWaveRespawned;
        PlayerEvents.Joined += OnJoined;
        PlayerEvents.Left += OnLeft;
        PlayerEvents.Death += OnDeath;
        PlayerEvents.Hurt += OnHurt;
        PlayerEvents.Hurting += OnHurting;
        PlayerEvents.ChangedRole += OnChangedRole;
        PlayerEvents.Spawned += OnSpawned;
        PlayerEvents.Escaped += OnEscaped;
        PlayerEvents.Escaping += OnEscaping;
        PlayerEvents.PlacingBulletHole += OnPlacingBulletHole;
        PlayerEvents.PickedUpItem += OnPickedUpItem;
        PlayerEvents.PickingUpItem += OnPickingUpItem;
        PlayerEvents.SearchingPickup += OnSearchingPickup;
        PlayerEvents.DroppedItem += OnDroppedItem;
        PlayerEvents.DroppingItem += OnDroppingItem;
        PlayerEvents.ShootingWeapon += OnShootingWeapon;
        PlayerEvents.ValidatedVisibility += OnValidatedVisibility;
        PlayerEvents.UsedItem += OnUsedItem;
        PlayerEvents.SendingVoiceMessage += OnSendingVoiceMessage;
        PlayerEvents.UsedIntercom += OnUsedIntercom;
        PlayerEvents.Cuffed += OnCuffed;
        PlayerEvents.Cuffing += OnCuffing;
        PlayerEvents.Uncuffed += OnUncuffed;
        PlayerEvents.ChangedItem += OnChangedItem;
        PlayerEvents.ThrewItem += OnThrewItem;
        PlayerEvents.Banned += OnBanned;
        PlayerEvents.Muted += OnMuted;
        PlayerEvents.Unmuted += OnUnmuted;
        PlayerEvents.EnteringPocketDimension += OnEnteringPocketDimension;
        PlayerEvents.LeftPocketDimension += OnLeftPocketDimension;
        PlayerEvents.TriggeringTesla += OnTriggeringTesla;
        PlayerEvents.InteractingDoor += OnInteractingDoor;
        PlayerEvents.InteractingElevator += OnInteractingElevator;
        PlayerEvents.InteractingLocker += OnInteractingLocker;
        WarheadEvents.Starting += OnWarheadStarting;
        WarheadEvents.Stopping += OnWarheadStopping;
        WarheadEvents.Detonated += OnWarheadDetonated;
        Scp049Events.ResurrectedBody += OnScp049ResurrectedBody;
        Scp096Events.AddingTarget += OnScp096AddingTarget;
        Scp096Events.Enraging += OnScp096Enraging;
        Scp914Events.KnobChanged += OnScp914KnobChanged;
        Scp914Events.Activated += OnScp914Activated;
        ObjectiveEvents.ActivatedGeneratorCompleted += OnGeneratorActivated;
        ServerEvents.LczDecontaminationStarted += OnLczDecontaminationStarted;
    }

    public void Unregister()
    {
        ServerEvents.WaitingForPlayers -= OnWaitingForPlayers;
        ServerEvents.RoundStarted -= OnRoundStarted;
        ServerEvents.RoundEnded -= OnRoundEnded;
        ServerEvents.WaveRespawned -= OnWaveRespawned;
        PlayerEvents.Joined -= OnJoined;
        PlayerEvents.Left -= OnLeft;
        PlayerEvents.Death -= OnDeath;
        PlayerEvents.Hurt -= OnHurt;
        PlayerEvents.Hurting -= OnHurting;
        PlayerEvents.ChangedRole -= OnChangedRole;
        PlayerEvents.Spawned -= OnSpawned;
        PlayerEvents.Escaped -= OnEscaped;
        PlayerEvents.Escaping -= OnEscaping;
        PlayerEvents.PlacingBulletHole -= OnPlacingBulletHole;
        PlayerEvents.PickedUpItem -= OnPickedUpItem;
        PlayerEvents.PickingUpItem -= OnPickingUpItem;
        PlayerEvents.SearchingPickup -= OnSearchingPickup;
        PlayerEvents.DroppedItem -= OnDroppedItem;
        PlayerEvents.DroppingItem -= OnDroppingItem;
        PlayerEvents.ShootingWeapon -= OnShootingWeapon;
        PlayerEvents.ValidatedVisibility -= OnValidatedVisibility;
        PlayerEvents.UsedItem -= OnUsedItem;
        PlayerEvents.SendingVoiceMessage -= OnSendingVoiceMessage;
        PlayerEvents.UsedIntercom -= OnUsedIntercom;
        PlayerEvents.Cuffed -= OnCuffed;
        PlayerEvents.Cuffing -= OnCuffing;
        PlayerEvents.Uncuffed -= OnUncuffed;
        PlayerEvents.ChangedItem -= OnChangedItem;
        PlayerEvents.ThrewItem -= OnThrewItem;
        PlayerEvents.Banned -= OnBanned;
        PlayerEvents.Muted -= OnMuted;
        PlayerEvents.Unmuted -= OnUnmuted;
        PlayerEvents.EnteringPocketDimension -= OnEnteringPocketDimension;
        PlayerEvents.LeftPocketDimension -= OnLeftPocketDimension;
        PlayerEvents.TriggeringTesla -= OnTriggeringTesla;
        PlayerEvents.InteractingDoor -= OnInteractingDoor;
        PlayerEvents.InteractingElevator -= OnInteractingElevator;
        PlayerEvents.InteractingLocker -= OnInteractingLocker;
        WarheadEvents.Starting -= OnWarheadStarting;
        WarheadEvents.Stopping -= OnWarheadStopping;
        WarheadEvents.Detonated -= OnWarheadDetonated;
        Scp049Events.ResurrectedBody -= OnScp049ResurrectedBody;
        Scp096Events.AddingTarget -= OnScp096AddingTarget;
        Scp096Events.Enraging -= OnScp096Enraging;
        Scp914Events.KnobChanged -= OnScp914KnobChanged;
        Scp914Events.Activated -= OnScp914Activated;
        ObjectiveEvents.ActivatedGeneratorCompleted -= OnGeneratorActivated;
        ServerEvents.LczDecontaminationStarted -= OnLczDecontaminationStarted;
    }

    private void OnWaitingForPlayers()
    {
        GameLog(messages.WaitingForPlayers);
        mapSpawnAttempts = 0;
        SpawnMapFeaturesWhenReady();
    }

    private void SpawnMapFeaturesWhenReady()
    {
        if (ProjectMER.Features.PrefabManager.PrimitiveObject is null)
        {
            if (++mapSpawnAttempts >= 80)
            {
                LabApi.Features.Console.Logger.Error("ProjectMER prefabs were not ready after 20 seconds; map features were not spawned.");
                return;
            }
            runtime.Schedule(.25f, SpawnMapFeaturesWhenReady);
            return;
        }
        SpawnMapFeatures();
    }

    private static void SpawnMapFeatures()
    {
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableSurfaceRework == true)
            PlayhousePlugin.Instance.SurfaceRework?.Spawn();
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableRecyclingBins == true)
            PlayhousePlugin.Instance.RecyclingBins?.Spawn();
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableVendingMachines == true)
            PlayhousePlugin.Instance.VendingMachines?.Spawn();
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableObjectives == true)
            PlayhousePlugin.Instance.Objectives?.Spawn();
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableContainment106Objective == true)
            PlayhousePlugin.Instance.Containment106?.Spawn();
    }

    private void OnPickingUpItem(PlayerPickingUpItemEventArgs ev)
    {
        PlayhousePlugin.Instance?.Containment106?.OnPickingUpItem(ev);
    }
    
    private void OnSearchingPickup(PlayerSearchingPickupEventArgs ev)
    {
        PlayhousePlugin.Instance?.Objectives?.OnSearchingPickup(ev);
        PlayhousePlugin.Instance?.VendingMachines?.OnSearchingPickup(ev);
    }

    private void OnRoundStarted()
    {
        state.ScpSwapAllowed = true;
        runtime.Schedule(120f, () => state.ScpSwapAllowed = false);
        breakoutBlitz.StartRound();
        foreach (Player player in Player.ReadyList)
            PlayhousePlugin.Instance?.RainbowTags?.TryAttach(player);
        if (IsSillySundayToday())
            PlayhousePlugin.Instance?.RainbowLights?.StartSundayRooms();
        if (PlayhousePlugin.Instance?.Config.MapFeatures.EnableObjectives == true)
            PlayhousePlugin.Instance.Objectives?.StartTimeline();
        runtime.Schedule(3f, PromoteInitialPlayers);
        GameLog(messages.RoundStarted);
    }

    private void OnRoundEnded(RoundEndedEventArgs ev)
    {
        // Stop custom decon timers and remove objective terminals.
        PlayhousePlugin.Instance?.Objectives?.Destroy();
    
        // Remove vending machines, buttons, coin slots, and their loops.
        PlayhousePlugin.Instance?.VendingMachines?.Destroy();
    
        GameLog(string.Format(messages.RoundEnded, ev.LeadingTeam));
        BroadcastRoundMvp();
        EnqueueMvpStatistic(generalKills);
        EnqueueMvpStatistic(scpKills);
        EnqueueMvpStatistic(humanKills);
    
        foreach (Pickup pickup in Pickup.List.ToArray())
        {
            if (!pickup.IsLocked)
                pickup.Destroy();
        }
    
        foreach (Ragdoll ragdoll in Ragdoll.List.ToArray())
            ragdoll.Destroy();
    
        foreach (string userId in state.PendingStatisticsDeletion)
            statistics?.Enqueue(userId, "Deleted player", "delete", 0);
    
        state.PendingStatisticsDeletion.Clear();
        state.InfiniteDropPlayerIds.Clear();
        state.InfiniteAmmoPlayerIds.Clear();
        state.InvisiblePlayerIds.Clear();
        state.InfectedPlayerIds.Clear();
        state.HiddenPlayerPairs.Clear();
        state.ContentGunRounds.Clear();
        state.WipeRadiosOnSpawn = false;
        state.ScpSwapAllowed = false;
        state.BulletHolesDisabled = false;
    
        global::PlayhousePlugin.Commands.ScpSwapCommand.ClearAll();
    
        breakoutBlitz.Reset();
        customClasses.Clear();
    
        PlayhousePlugin.Instance?.SillySunday?.Reset();
        PlayhousePlugin.Instance?.Sprays?.Reset();
        PlayhousePlugin.Instance?.RainbowLights?.Reset();
    
        escapingClasses.Clear();
        recentlyHitBy106.Clear();
        recentlyUncuffedBy.Clear();
        generalKills.Clear();
        scpKills.Clear();
        humanKills.Clear();
    }

    private void OnJoined(PlayerJoinedEventArgs ev)
    {
        string nickname = ev.Player.Nickname.ToLowerInvariant();
        string[] advertisingDomains = { "hellcase.org", "hellcase.com", "velk.ca", "pvpro.com", "banditcamp.com", "bandit.camp", "rustchance.com", "flx.gg" };
        string? detected = advertisingDomains.FirstOrDefault(nickname.Contains);
        if (detected is not null)
        {
            ev.Player.Disconnect($"<color=cyan>Detected advertising ({detected}) in your username. Please change it before joining the server.\nIf you believe this was a mistake, contact staff: https://discord.gg/</color>");
            return;
        }
        RecordJoin(ev);
        runtime.Schedule(1f, () => PlayhousePlugin.Instance?.RainbowTags?.TryAttach(ev.Player));
        SendWelcome(ev.Player);
        if (Round.IsRoundInProgress && PlayhousePlugin.Instance?.SillySunday is not { NerfWar: true } and not { OhFiveRescue: true } and not { Slaughterhouse: true })
            runtime.Schedule(8f, () => SpawnLateJoiner(ev.Player));
    }

    private void SpawnLateJoiner(Player player)
    {
        if (!Player.ReadyList.Contains(player) || !Round.IsRoundInProgress || Round.Duration.TotalSeconds >= 180 || player.IsAlive) return;
        int chance = random.Next(100);
        RoleTypeId role = chance < 20 ? RoleTypeId.FacilityGuard : chance < 50 ? RoleTypeId.Scientist : RoleTypeId.ClassD;
        player.SetRole(role, RoleChangeReason.RoundStart, RoleSpawnFlags.All);
        if (PlayhousePlugin.Instance?.SillySunday?.SugarRush == true)
            runtime.Schedule(.5f, () => player.EnableEffect<MovementBoost>(30, 1800));
    }

    private void SendWelcome(Player player)
    {
        bool sunday = IsSillySundayToday();
        string[] colors = { "#00FFFF", "#ff96de", "#ff96de", "#00FFFF", "orange" };
        string color = colors[random.Next(colors.Length)];
        player.SendBroadcast($"<size={(sunday ? 50 : 70)}><color={color}>Welcome <b>{player.Nickname}</b> to Eclipteds Play House and Site-{(sunday ? "69" : "79")}{(sunday ? "\nEnjoy the events!" : string.Empty)}</color></size>", 5);
        runtime.Schedule(15f, () =>
        {
            if (!Player.ReadyList.Contains(player)) return;
            player.SendBroadcast("<size=48><color=#00FFFF>Please <color=#ff96de><b>read the rules</b></color> before playing. Open Escape, then Server Info.</color></size>", 7);
        });
    }

    private void RecordJoin(PlayerJoinedEventArgs ev)
    {
        GameLog(string.Format(messages.PlayerJoined, Describe(ev.Player)));
        if (!ev.Player.DoNotTrack)
            statistics?.Enqueue(ev.Player.UserId, ev.Player.Nickname, "join", 0);
    }

    private void OnLeft(PlayerLeftEventArgs ev)
    {
        state.InfiniteDropPlayerIds.Remove(ev.Player.PlayerId);
        state.InfiniteAmmoPlayerIds.Remove(ev.Player.PlayerId);
        state.InvisiblePlayerIds.Remove(ev.Player.PlayerId);
        state.InfectedPlayerIds.Remove(ev.Player.PlayerId);
        state.ContentGunRounds.Remove(ev.Player.PlayerId);
        global::PlayhousePlugin.Commands.ScpSwapCommand.ClearFor(ev.Player);
        customClasses.Remove(ev.Player);
        PlayhousePlugin.Instance?.Cosmetics?.Remove(ev.Player);
        PlayhousePlugin.Instance?.RainbowTags?.Remove(ev.Player);
        GameLog(string.Format(messages.PlayerLeft, Describe(ev.Player)));
    }

    private void OnChangedRole(PlayerChangedRoleEventArgs ev)
    {
        customClasses.Remove(ev.Player);
        GameLog(string.Format(messages.PlayerChangedRole, Describe(ev.Player), ev.NewRole.RoleTypeId));
    }

    private void OnSpawned(PlayerSpawnedEventArgs ev)
    {
        var player = ev.Player;
        if (PlayhousePlugin.Instance?.Config.Cosmetics.EnablePets == true)
            runtime.Schedule(1f, () => PlayhousePlugin.Instance?.Cosmetics?.OnSpawned(player));
        if (!customClasses.TryGet(player, out _))
        {
            RoleTypeId role = ev.Role.RoleTypeId;
            if (role is RoleTypeId.Scp049 or RoleTypeId.Scp096 or RoleTypeId.Scp173 or RoleTypeId.Scp939)
                customClasses.Assign(player, new ScpSupportClass(player, runtime, role));
            else if (role == RoleTypeId.Scp106)
                customClasses.Assign(player, new Scp106Class(player, runtime));
            else if (role == RoleTypeId.Scp079)
                customClasses.Assign(player, new Scp079Class(player, runtime));
            else if (role == RoleTypeId.NtfCaptain)
                customClasses.Assign(player, new NtfCaptainClass(player, runtime));
            else if (role == RoleTypeId.NtfSergeant)
                customClasses.Assign(player, new NtfSergeantClass(player));
        }
        if (state.WipeRadiosOnSpawn)
            runtime.Schedule(2f, () => { if (LabApi.Features.Wrappers.Player.ReadyList.Contains(player)) player.RemoveItem(ItemType.Radio, int.MaxValue); });
        if (player.Role == RoleTypeId.Scp0492 && PlayhousePlugin.Instance?.SillySunday?.IsEnabled == true)
            player.SendBroadcast("<b>Type 'cmdbind f.zfe' in console, then press F to explode!</b>", 12);
        string rawUserId = ev.Player.UserId.Split('@')[0];
        if (rawUserId == "76561198434926562" && ev.Role.RoleTypeId != PlayerRoles.RoleTypeId.Scp079)
            runtime.Schedule(5f, () => { if (LabApi.Features.Wrappers.Player.ReadyList.Contains(player)) player.AddItem(ItemType.Flashlight); });
        if (rawUserId == "76561198059742329" && ev.Role.RoleTypeId != PlayerRoles.RoleTypeId.Scp079)
            runtime.Schedule(5f, () => { if (LabApi.Features.Wrappers.Player.ReadyList.Contains(player)) player.AddItem(ItemType.Coin); });
    }

    private void OnPlacingBulletHole(PlayerPlacingBulletHoleEventArgs ev)
    {
        if (state.BulletHolesDisabled)
            ev.IsAllowed = false;
    }

    private void OnSendingVoiceMessage(PlayerSendingVoiceMessageEventArgs ev)
    {
        // Legacy Radio.UserCode_CmdSyncTransmissionStatus patch: SCP radio speech is
        // routed through the native mimicry channel in current game versions.
        if (ev.Player.Team == Team.SCPs && ev.Message.Channel == VoiceChatChannel.Radio)
            ev.Message.Channel = VoiceChatChannel.Mimicry;
    }

    private void OnDeath(PlayerDeathEventArgs ev)
    {
        PlayhousePlugin.Instance?.Cosmetics?.Remove(ev.Player);
        PlayhousePlugin.Instance?.SillySunday?.OnDeath(ev.Player, ev.Attacker);
        string? victimClass = customClasses.TryGet(ev.Player, out PlayerClassState victimState) ? victimState.CustomClass?.Name : null;
        Vector3 deathPosition = ev.Player.Position;
        string victim = Describe(ev.Player);
        string attacker = ev.Attacker is null ? "the environment" : Describe(ev.Attacker);
        webhooks.Enqueue(WebhookDestination.PvpLogs,
            string.Format(messages.PlayerKilled, attacker, victim));
        if (!ev.Player.DoNotTrack)
            statistics?.Enqueue(ev.Player.UserId, ev.Player.Nickname, "death", 1);
        if (ev.Attacker is not null && ev.Attacker != ev.Player && !ev.Attacker.DoNotTrack)
            statistics?.Enqueue(ev.Attacker.UserId, ev.Attacker.Nickname, "kill", 1);
        if (ev.Attacker is not null && ev.Attacker != ev.Player)
        {
            Increment(generalKills, ev.Attacker.PlayerId);
            Increment(ev.Attacker.Team == Team.SCPs ? scpKills : humanKills, ev.Attacker.PlayerId);
        }
        if (ev.Attacker is not null && ev.Attacker != ev.Player && ev.Attacker.Team != Team.SCPs)
        {
            if (ev.Player.IsDisarmed)
                StaffLog($"Detained kill: {Describe(ev.Attacker)} killed {victim}; detainer: {(ev.Player.DisarmedBy is null ? "unknown" : Describe(ev.Player.DisarmedBy))}.");
            else if (recentlyUncuffedBy.TryGetValue(ev.Player.PlayerId, out string uncuffer))
                StaffLog($"Suspected undetain-to-kill: {Describe(ev.Attacker)} killed {victim}; recent uncuffer: {uncuffer}.");
        }

        if (victimClass == "Zombie Boomer") StartToxicZone(deathPosition, ev.Player);
        if (state.InfectedPlayerIds.Remove(ev.Player.PlayerId))
        {
            Vector3 spawnPosition = ev.Attacker?.Position ?? deathPosition;
            runtime.Schedule(0.5f, () =>
            {
                if (!LabApi.Features.Wrappers.Player.ReadyList.Contains(ev.Player)) return;
                ev.Player.SetRole(RoleTypeId.Scp0492);
                runtime.Schedule(0.5f, () =>
                {
                    if (!LabApi.Features.Wrappers.Player.ReadyList.Contains(ev.Player)) return;
                    ev.Player.Position = spawnPosition;
                    customClasses.Assign(ev.Player, new ZombieClass(ev.Player, runtime, ZombieArchetype.Overclocker));
                });
            });
        }

        if (breakoutBlitz.IsEnabled && ev.Attacker is not null && ev.Attacker != ev.Player && ev.Attacker.Team == Team.SCPs)
        {
            breakoutBlitz.ScpKills++;
            if (breakoutBlitz.ScpKills >= breakoutBlitz.RequiredScpKills)
                FinishBreakoutBlitz("<color=red>SCPs Won!</color>", "SCP units have won");
        }
    }

    private void OnScp049ResurrectedBody(Scp049ResurrectedBodyEventArgs ev)
    {
        var silly = PlayhousePlugin.Instance?.SillySunday;
        if (silly?.IsEnabled != true || !silly.RandomRevive) return;
        Vector3 position = ev.Player.Position;
        RoleTypeId role = silly.GetReviveRole();
        runtime.Schedule(0.2f, () => { ev.Target.SetRole(role); runtime.Schedule(0.4f, () => { ev.Target.Position = position; ev.Target.Health *= 0.3f; }); });
    }

    private void OnScp096AddingTarget(Scp096AddingTargetEventArgs ev)
    {
        if (state.InvisiblePlayerIds.Contains(ev.Target.PlayerId))
        {
            ev.IsAllowed = false;
            return;
        }
        List<string> responses = ev.Target.UserId.Split('@')[0] == "76561198059742329"
            ? CustomNotificationMessages.Tony096Responses
            : CustomNotificationMessages.responses096AddTarget;
        if (responses.Count > 0) ev.Target.SendHint(responses[random.Next(responses.Count)], 5);
    }

    private void OnScp096Enraging(Scp096EnragingEventArgs ev)
    {
        var colors = Room.List.Where(room => room.LightController is not null)
            .ToDictionary(room => room, room => room.LightController!.OverrideLightsColor);
        foreach (Room room in colors.Keys) room.LightController!.OverrideLightsColor = Color.black;
        runtime.Schedule(.5f, () =>
        {
            foreach (var pair in colors)
                if (pair.Key.LightController is not null) pair.Key.LightController.OverrideLightsColor = pair.Value;
        });
    }

    private void OnEscaped(PlayerEscapedEventArgs ev)
    {
        if (PlayhousePlugin.Instance?.SillySunday is
            {
                OhFiveRescue: true,
                OhFivePlayer: not null
            } silly &&
            ev.Player == silly.OhFivePlayer)
        {
            Server.SendBroadcast("MTF Wins! The O5 has escaped!", 10);
            Round.End(true);
        }
    
        CustomClassType previousClass = CustomClassType.None;

        if (escapingClasses.TryGetValue(
                ev.Player.PlayerId,
                out CustomClassType escapedClass))
        {
            previousClass = escapedClass;
            escapingClasses.Remove(ev.Player.PlayerId);
        }
    
        Player player = ev.Player;
        RoleTypeId newRole = ev.NewRole;
    
        runtime.Schedule(0.5f, () =>
        {
            if (!Player.ReadyList.Contains(player))
                return;
    
            AssignEscapedClass(player, previousClass, newRole);
        });
    
        if (!ev.Player.DoNotTrack)
            statistics?.Enqueue(
                ev.Player.UserId,
                ev.Player.Nickname,
                "escape",
                1);
    
        if (!breakoutBlitz.IsEnabled)
            return;
    
        if (ev.NewRole == RoleTypeId.ChaosConscript)
        {
            breakoutBlitz.ClassDEscapes++;
    
            if (breakoutBlitz.ClassDEscapes >=
                breakoutBlitz.RequiredClassDEscapes)
            {
                FinishBreakoutBlitz(
                    "<color=green>Chaos Won!</color>",
                    "Chaos insurgency has won");
            }
        }
        else if (ev.NewRole is RoleTypeId.NtfPrivate or RoleTypeId.NtfSpecialist)
        {
            breakoutBlitz.ScientistEscapes++;
    
            if (breakoutBlitz.ScientistEscapes >=
                breakoutBlitz.RequiredScientistEscapes)
            {
                FinishBreakoutBlitz(
                    "<color=blue>NTF Won!</color>",
                    "Mobile task force has won");
            }
        }
    }

    private CustomClassBase? CreateRandomEscapeClass(
        Player player,
        RoleTypeId escapedRole)
    {
        // Scientist escaped and became an NTF role.
        if (escapedRole is RoleTypeId.NtfPrivate
            or RoleTypeId.NtfSpecialist
            or RoleTypeId.NtfSergeant
            or RoleTypeId.NtfCaptain)
        {
            return random.Next(6) switch
            {
                0 => new NtfMedic(player, runtime),
                1 => new NtfHeavy(player, runtime),
                2 => new ScoutClass(player, false),
                3 => new EngineerClass(player, runtime, false),
                4 => new DemolitionsClass(
                    player,
                    runtime,
                    false,
                    false),
                _ => new ContainmentSpecialistClass(player, false)
            };
        }
    
        // Class-D escaped and became Chaos.
        if (escapedRole is RoleTypeId.ChaosConscript
            or RoleTypeId.ChaosRifleman
            or RoleTypeId.ChaosMarauder
            or RoleTypeId.ChaosRepressor)
        {
            return random.Next(8) switch
            {
                0 => new ChaosMedic(player, runtime),
                1 => new ChaosHeavy(player, runtime),
                2 => new ScoutClass(player, true),
                3 => new EngineerClass(player, runtime, true),
                4 => new DemolitionsClass(
                    player,
                    runtime,
                    true,
                    false),
                5 => new ExterminatorClass(
                    player,
                    runtime,
                    true),
                6 => new HereticClass(
                    player,
                    runtime,
                    true),
                _ => new MachinistClass(
                    player,
                    runtime,
                    true)
            };
        }
    
        return null;
    }

    private void OnEscaping(PlayerEscapingEventArgs ev)
    {
        if (customClasses.TryGet(ev.Player, out PlayerClassState state) &&
            state.CustomClass is not null)
        {
            escapingClasses[ev.Player.PlayerId] =
                CustomClassTypeResolver.Resolve(state.CustomClass);
        }
    }

    private void OnWaveRespawned(WaveRespawnedEventArgs ev)
    {
        GameLog($":military_helmet: {ev.Wave} spawned with {ev.Players.Count} players.");
        runtime.Schedule(.6f, () => AssignWaveClasses(ev.Players.ToArray()));
        SoftCleanWaveItems();
    }

    private void SoftCleanWaveItems()
    {
        ItemType[] types = { ItemType.Radio, ItemType.KeycardMTFOperative, ItemType.ArmorCombat };
        foreach (ItemType type in types)
        {
            Pickup[] pickups = Pickup.List.Where(p => p.Type == type).OrderBy(_ => random.Next()).ToArray();
            if (pickups.Length <= 10) continue;
            int removeCount = 3 + pickups.Length / 2;
            for (int index = 0; index < removeCount && index < pickups.Length; index++)
            {
                Pickup pickup = pickups[index];
                runtime.Schedule(index * .1f, () => { if (!pickup.IsDestroyed) pickup.Destroy(); });
            }
        }
    }

    private void OnUsedIntercom(PlayerUsedIntercomEventArgs ev) =>
        GameLog($":studio_microphone: {(ev.Player is null ? "Unknown player" : Describe(ev.Player))} changed intercom state to {ev.State}.");
    private void OnCuffed(PlayerCuffedEventArgs ev) =>
        GameLog($":link: {(ev.Target is null ? "Unknown player" : Describe(ev.Target))} was handcuffed by {Describe(ev.Player)}.");
    private void OnCuffing(PlayerCuffingEventArgs ev)
    {
        if (ev.Target?.Role == RoleTypeId.Tutorial) ev.IsAllowed = false;
    }
    private void OnUncuffed(PlayerUncuffedEventArgs ev) =>
        RecordUncuffed(ev);
    private void RecordUncuffed(PlayerUncuffedEventArgs ev)
    {
        if (ev.Target is null) return;
        GameLog($":unlock: {Describe(ev.Target)} was freed by {Describe(ev.Player)}.");
        int targetId = ev.Target.PlayerId;
        recentlyUncuffedBy[targetId] = Describe(ev.Player);
        runtime.Schedule(5f, () => recentlyUncuffedBy.Remove(targetId));
    }
    private void OnChangedItem(PlayerChangedItemEventArgs ev) =>
        GameLog($":arrows_counterclockwise: {Describe(ev.Player)} changed held item from {ev.OldItem?.Type.ToString() ?? "None"} to {ev.NewItem?.Type.ToString() ?? "None"}.");
    private void OnThrewItem(PlayerThrewItemEventArgs ev) =>
        GameLog($":boom: {Describe(ev.Player)} threw {ev.Pickup.Type}.");
    private void OnScp914KnobChanged(Scp914KnobChangedEventArgs ev) =>
        GameLog($":gear: {Describe(ev.Player)} changed SCP-914 from {ev.OldKnobSetting} to {ev.KnobSetting}.");
    private void OnScp914Activated(Scp914ActivatedEventArgs ev) =>
        GameLog($":gear: {Describe(ev.Player)} activated SCP-914.");
    private void OnGeneratorActivated(GeneratorActivatedObjectiveEventArgs ev) =>
        GameLog($":electric_plug: {Describe(ev.Player)} completed generator activation in {ev.Generator.Room?.Name}.");
    private void OnLczDecontaminationStarted()
    {
        GameLog(":biohazard: Light Containment Zone decontamination has begun.");
        foreach (Pickup pickup in Pickup.List.ToArray())
            if (pickup.Position.y is > -200f and < 200f && !pickup.IsLocked) pickup.Destroy();
        foreach (Ragdoll ragdoll in Ragdoll.List.ToArray())
            if (ragdoll.Position.y is > -200f and < 200f) ragdoll.Destroy();
    }
    private void OnBanned(PlayerBannedEventArgs ev) => StaffLog($"Banned player: {(ev.Player is null ? ev.PlayerId : Describe(ev.Player))}\nReason: {ev.Reason}\nDuration: {ev.Duration} seconds\nIssuer: {(ev.Issuer is null ? "Server" : Describe(ev.Issuer))}");
    private void OnMuted(PlayerMutedEventArgs ev) => StaffLog($"{(ev.IsIntercom ? "Intercom " : string.Empty)}mute applied to {Describe(ev.Player)} by {(ev.Issuer is null ? "Server" : Describe(ev.Issuer))}.");
    private void OnUnmuted(PlayerUnmutedEventArgs ev) => StaffLog($"{(ev.IsIntercom ? "Intercom " : string.Empty)}mute removed from {Describe(ev.Player)} by {(ev.Issuer is null ? "Server" : Describe(ev.Issuer))}.");

    private void AssignWaveClasses(Player[] players)
    {
        var cadets = players.Where(p => p.Role == RoleTypeId.NtfPrivate).OrderBy(_ => random.Next()).ToList();
        if (cadets.Count > 0) Assign(cadets, () => new ContainmentSpecialistClass(cadets[0], false));
        for (int cycle = 0; cycle < 2 && cadets.Count > 0; cycle++)
        {
            Assign(cadets, () => new EngineerClass(cadets[0], runtime, false));
            Assign(cadets, () => new DemolitionsClass(cadets[0], runtime, false, false));
            Assign(cadets, () => new NtfMedic(cadets[0], runtime));
            Assign(cadets, () => new NtfHeavy(cadets[0], runtime));
            Assign(cadets, () => new ScoutClass(cadets[0], false));
        }

        foreach (Player player in players.Where(p => p.Role == RoleTypeId.ChaosMarauder))
            customClasses.Assign(player, new HunterClass(player, true));
        foreach (Player player in players.Where(p => p.Role == RoleTypeId.ChaosRepressor))
            customClasses.Assign(player, new BulldozerClass(player, runtime, true));
        var riflemen = players.Where(p => p.Role == RoleTypeId.ChaosRifleman).OrderBy(_ => random.Next()).ToList();
        while (riflemen.Count > 0)
        {
            Assign(riflemen, () => new DemolitionsClass(riflemen[0], runtime, true, true));
            Skip(riflemen);
            Assign(riflemen, () => new ExterminatorClass(riflemen[0], runtime, true));
            Assign(riflemen, () => new HereticClass(riflemen[0], runtime, true));
            Assign(riflemen, () => new MachinistClass(riflemen[0], runtime, true));
            Skip(riflemen);
        }
    }

    private void PromoteInitialPlayers()
    {
        var classD = Player.ReadyList.Where(p => p.Role == RoleTypeId.ClassD).OrderBy(_ => random.Next()).ToList();
        while (classD.Count > 0)
        {
            Assign(classD, () => new ClassDChad(classD[0]));
            Assign(classD, () => new ClassDJanitorClass(classD[0]));
            GiveCoinAndSkip(classD); GiveCoinAndSkip(classD);
        }
        var guards = Player.ReadyList.Where(p => p.Role == RoleTypeId.FacilityGuard).OrderBy(_ => random.Next()).ToList();
        Assign(guards, () => new GuardManagerClass(guards[0]));
        while (guards.Count > 0) { Assign(guards, () => new SeniorGuard(guards[0])); Skip(guards); Skip(guards); }
        var scientists = Player.ReadyList.Where(p => p.Role == RoleTypeId.Scientist).OrderBy(_ => random.Next()).ToList();
        Assign(scientists, () => new MajorScientistClass(scientists[0]));
        while (scientists.Count > 0) { Assign(scientists, () => new MajorScientistClass(scientists[0])); Skip(scientists); Skip(scientists); }
    }

    private void Assign(List<Player> players, System.Func<CustomClassBase> factory)
    {
        if (players.Count == 0) return;
        Player player = players[0];
        CustomClassBase customClass = factory();
        customClasses.Assign(player, customClass);
        players.RemoveAt(0);
    }
    private static void Skip(List<Player> players) { if (players.Count > 0) players.RemoveAt(0); }
    private void GiveCoinAndSkip(List<Player> players)
    {
        if (players.Count == 0) return;
        if (random.Next(2) == 0) players[0].AddItem(ItemType.Coin);
        players.RemoveAt(0);
    }

    private void AssignEscapedClass(
        Player player,
        CustomClassType previousClass,
        RoleTypeId escapedRole)
    {
        if (!Player.ReadyList.Contains(player))
            return;

        bool escapedAsNtf = escapedRole is
            RoleTypeId.NtfPrivate or
            RoleTypeId.NtfSpecialist or
            RoleTypeId.NtfSergeant or
            RoleTypeId.NtfCaptain;

        bool escapedAsChaos = escapedRole is
            RoleTypeId.ChaosConscript or
            RoleTypeId.ChaosRifleman or
            RoleTypeId.ChaosMarauder or
            RoleTypeId.ChaosRepressor;

        if (!escapedAsNtf && !escapedAsChaos)
            return;

        CustomClassBase? replacement = previousClass switch
        {
            CustomClassType.NtfMedic or CustomClassType.ChaosMedic =>
                escapedAsChaos ? new ChaosMedic(player, runtime) : new NtfMedic(player, runtime),

            CustomClassType.NtfHeavy or CustomClassType.ChaosHeavy =>
                escapedAsChaos ? new ChaosHeavy(player, runtime) : new NtfHeavy(player, runtime),

            CustomClassType.NtfScout or CustomClassType.ChaosScout =>
                new ScoutClass(player, escapedAsChaos),

            CustomClassType.NtfDemoman or CustomClassType.ChaosDemoman =>
                new DemolitionsClass(player, runtime, escapedAsChaos, false),

            CustomClassType.NtfDemolitionsExpert or CustomClassType.ChaosDemolitionsExpert =>
                new DemolitionsClass(player, runtime, escapedAsChaos, true),

            CustomClassType.NtfContainmentSpecialist or CustomClassType.ChaosContainmentSpecialist =>
                new ContainmentSpecialistClass(player, escapedAsChaos),

            CustomClassType.NtfBulldozer or CustomClassType.ChaosBulldozer =>
                new BulldozerClass(player, runtime, escapedAsChaos),

            CustomClassType.NtfHunter or CustomClassType.ChaosHunter =>
                new HunterClass(player, escapedAsChaos),

            CustomClassType.NtfExterminator or CustomClassType.ChaosExterminator =>
                new ExterminatorClass(player, runtime, escapedAsChaos),

            CustomClassType.NtfHeretic or CustomClassType.ChaosHeretic =>
                new HereticClass(player, runtime, escapedAsChaos),

            CustomClassType.NtfEngineer or CustomClassType.ChaosEngineer =>
                new EngineerClass(player, runtime, escapedAsChaos),

            CustomClassType.NtfMachinist or CustomClassType.ChaosMachinist =>
                new MachinistClass(player, runtime, escapedAsChaos),

            CustomClassType.NtfManager or CustomClassType.ChaosManager =>
                new ManagerClass(player, escapedAsChaos),

            _ => null
        };

        replacement ??= CreateRandomEscapeClass(player, escapedRole);

        if (replacement is null)
            return;

        customClasses.Assign(player, replacement);

        player.SendBroadcast(
            $"<size=45><color=yellow>You escaped and became " +
            $"<b>{replacement.Name}</b>!</color></size>",
            8);
    }

    private void FinishBreakoutBlitz(string broadcast, string announcement)
    {
        if (!breakoutBlitz.IsEnabled) return;
        breakoutBlitz.IsEnabled = false;
        Server.SendBroadcast(broadcast, 10);
        Announcer.Message(announcement, string.Empty, true, 0f, 1f);
        runtime.Schedule(10f, () => Round.Restart());
    }

    private void OnHurt(PlayerHurtEventArgs ev)
    {
        if (ev.Attacker is null || ev.Attacker == ev.Player || ev.Attacker.DoNotTrack)
            return;

        int damage = ev.DamageHandler is AttackerDamageHandler attackerDamage
            ? (int)System.Math.Round(attackerDamage.Damage)
            : 0;
        if (damage > 0)
        {
            statistics?.Enqueue(ev.Attacker.UserId, ev.Attacker.Nickname, "damage", damage);
            webhooks.Enqueue(WebhookDestination.PvpLogs,
                $"[{System.DateTime.Now:HH:mm:ss}] {Describe(ev.Attacker)} damaged {Describe(ev.Player)} for {damage} with {ev.DamageHandler.ServerLogsText}.");
        }
    }

    private void OnHurting(PlayerHurtingEventArgs ev)
    {
        string? className = customClasses.TryGet(ev.Player, out PlayerClassState state) ? state.CustomClass?.Name : null;
        string? attackerClass = ev.Attacker is not null && customClasses.TryGet(ev.Attacker, out PlayerClassState attackerState) ? attackerState.CustomClass?.Name : null;
        if (ev.Attacker is not null &&
            (this.state.HiddenPlayerPairs.Contains(VanishAbility.Key(ev.Attacker, ev.Player)) || this.state.HiddenPlayerPairs.Contains(VanishAbility.Key(ev.Player, ev.Attacker))))
        {
            ev.IsAllowed = false;
            return;
        }
        if (ev.Attacker is not null && ev.Attacker != ev.Player && ev.Attacker.Role == RoleTypeId.Scp0492 && ev.Player.Role != RoleTypeId.Tutorial &&
            ev.DamageHandler is UniversalDamageHandler zombieDamage && zombieDamage.TranslationId == DeathTranslations.Zombie.Id)
        {
            InfectionService.Infect(ev.Player);
            if (attackerClass == "Zombie Sprinter") zombieDamage.Damage = 20;
        }
        if (className is "NTF Scout" or "Chaos Scout" or "NTF Hunter" or "Chaos Hunter")
        {
            if (ev.DamageHandler is UniversalDamageHandler damage && damage.TranslationId == DeathTranslations.Scp207.Id)
                ev.IsAllowed = false;
        }
        else if (className == "Zombie Sprinter" && ev.DamageHandler is UniversalDamageHandler sprinterDamage && sprinterDamage.TranslationId == DeathTranslations.Scp207.Id)
            ev.IsAllowed = false;
        else if (className == "Zombie Overclocker" && ev.DamageHandler is UniversalDamageHandler overclockDamage && overclockDamage.TranslationId == DeathTranslations.Scp207.Id)
            overclockDamage.Damage = 18;
        if (attackerClass is "NTF Containment Specialist" or "Chaos Containment Specialist" && ev.Attacker is not null && ev.DamageHandler is FirearmDamageHandler firearm)
        {
            if (firearm.WeaponType == ItemType.GunCOM18) firearm.Damage *= 1.4f;
            else if (firearm.WeaponType == ItemType.GunRevolver && ev.Attacker.Team != ev.Player.Team) ev.Player.EnableEffect<Burned>(1, 10);
        }
        if (attackerClass == "NTF Captain" && ev.Attacker is not null && customClasses.TryGet(ev.Attacker, out PlayerClassState captainState) &&
            captainState.CustomClass?.ActiveAbilities.Count > 0 && captainState.CustomClass.ActiveAbilities[0] is HotBulletsAbility { IsActive: true })
            ev.Player.EnableEffect<Burned>(1, 10);
    }

    private void StartToxicZone(Vector3 position, Player zombie)
    {
        int ticks = 0;
        ScheduledHandle? handle = null;
        handle = runtime.Repeat(0.2f, () =>
        {
            if (++ticks > 50) { handle?.Cancel(); return; }
            foreach (Player target in LabApi.Features.Wrappers.Player.ReadyList)
            {
                if (!target.IsAlive || target.Team == Team.SCPs || target == zombie || Vector3.Distance(target.Position, position) > 4f) continue;
                if (target.Health <= 1) target.Kill("Zombie Infection", string.Empty); else target.Health -= 1;
                InfectionService.Infect(target);
            }
        });
    }

    private void OnPickedUpItem(PlayerPickedUpItemEventArgs ev) =>
        GameLog(string.Format(messages.PickedUpItem, Describe(ev.Player), ev.Item.Type));

    private void OnDroppedItem(PlayerDroppedItemEventArgs ev) =>
        RecordDroppedItem(ev);

    private void OnDroppingItem(PlayerDroppingItemEventArgs ev)
    {
        if (state.InfiniteAmmoPlayerIds.Contains(ev.Player.PlayerId))
            ev.IsAllowed = false;
    }

    private void OnShootingWeapon(PlayerShootingWeaponEventArgs ev)
    {
        if (state.InfiniteAmmoPlayerIds.Contains(ev.Player.PlayerId))
            ev.FirearmItem.StoredAmmo = ev.FirearmItem.MaxAmmo;
        if (!state.ContentGunRounds.TryGetValue(ev.Player.PlayerId, out int remaining)) return;
        if (remaining <= 0)
        {
            state.ContentGunRounds.Remove(ev.Player.PlayerId);
            ev.Player.SendBroadcast("<i>You have run out of content gun rounds!</i>", 5);
            return;
        }
        ev.IsAllowed = false;
        remaining--;
        state.ContentGunRounds[ev.Player.PlayerId] = remaining;
        Vector3 ragdollVelocity = ev.Player.Camera.forward * 10f + ev.Player.Camera.up;
        var damage = new CustomReasonDamageHandler("Spawned by a nearby Patreon supporter", float.MaxValue);
        Ragdoll? ragdoll = Ragdoll.SpawnRagdoll(RoleTypeId.Scientist, ev.Player.Position, ev.Player.Camera.rotation,
            damage, "gaming", ragdollVelocity, null, null);
        if (ragdoll is not null)
        {
            ragdoll.Scale = new Vector3(UnityEngine.Random.Range(.25f, 6f), UnityEngine.Random.Range(.25f, 6f), UnityEngine.Random.Range(.25f, 6f));
            runtime.Schedule(8f, ragdoll.Destroy);
        }
        ev.Player.SendHint($"<color=blue>{remaining} ragdolls left</color>", 1f);
    }

    private void OnValidatedVisibility(PlayerValidatedVisibilityEventArgs ev)
    {
        if (state.InvisiblePlayerIds.Contains(ev.Target.PlayerId)) ev.IsVisible = false;
        if (state.HiddenPlayerPairs.Contains(VanishAbility.Key(ev.Target, ev.Player))) ev.IsVisible = false;
    }

    private void RecordDroppedItem(PlayerDroppedItemEventArgs ev)
    {
        GameLog(string.Format(messages.DroppedItem, Describe(ev.Player), ev.Pickup.Type));
        if (state.InfiniteDropPlayerIds.Contains(ev.Player.PlayerId))
            ev.Player.AddItem(ev.Pickup.Type);
    }

    private void OnUsedItem(PlayerUsedItemEventArgs ev)
    {
        ItemType type = ev.UsableItem.Type;
        GameLog(string.Format(messages.UsedItem, Describe(ev.Player), type));
        if (!ev.Player.DoNotTrack)
            statistics?.Enqueue(ev.Player.UserId, ev.Player.Nickname,
                type is ItemType.SCP207 or ItemType.SCP500 ? "scpitems" : "meditems", 1);
        if (type == ItemType.Adrenaline)
        {
            ev.Player.EnableEffect<MovementBoost>(15, 8);
            ev.Player.SendHint("<color=yellow>+Movement Speed Boost</color>", 4);
        }
        if (type is ItemType.Medkit or ItemType.SCP500)
        {
            state.InfectedPlayerIds.Remove(ev.Player.PlayerId);
            ev.Player.DisableEffect<Poisoned>();
            ev.Player.DisableEffect<Hemorrhage>();
        }
        if (type == ItemType.SCP500)
        {
            ev.Player.Health = ev.Player.MaxHealth;
            string? className = customClasses.TryGet(ev.Player, out PlayerClassState classState) ? classState.CustomClass?.Name : null;
            if (className == "NTF Scout") runtime.Schedule(.2f, () => ev.Player.EnableEffect<Scp207>());
            if (PlayhousePlugin.Instance?.SillySunday?.SugarRush == true)
                runtime.Schedule(.2f, () => ev.Player.EnableEffect<Scp207>(4, 0));
        }
    }

    private static void Increment(Dictionary<int, int> values, int playerId) =>
        values[playerId] = values.TryGetValue(playerId, out int current) ? current + 1 : 1;

    private void BroadcastRoundMvp()
    {
        if (generalKills.Count == 0)
        {
            Server.SendBroadcast("<size=100><b><color=#FF69B4>No one got any kills!</color></b></size>", 10);
            return;
        }
        string Line(Dictionary<int, int> values, string suffix)
        {
            if (values.Count == 0) return string.Empty;
            var best = values.OrderByDescending(pair => pair.Value).First();
            string name = Player.ReadyList.FirstOrDefault(player => player.PlayerId == best.Key)?.Nickname ?? $"Player {best.Key}";
            return $"\n<size=30><b>{name} with <color=red>{best.Value} Kills</color> {suffix}</b></size>";
        }
        Server.SendBroadcast(Line(generalKills, "in Total") + Line(scpKills, "as SCP") + Line(humanKills, "as a Human"), 15);
    }

    private void EnqueueMvpStatistic(Dictionary<int, int> values)
    {
        foreach (var candidate in values.OrderByDescending(pair => pair.Value))
        {
            Player? player = Player.ReadyList.FirstOrDefault(item => item.PlayerId == candidate.Key);
            if (player is null || player.DoNotTrack) continue;
            statistics?.Enqueue(player.UserId, player.Nickname, "mvp", candidate.Value);
            break;
        }
    }

    private void OnEnteringPocketDimension(PlayerEnteringPocketDimensionEventArgs ev)
    {
        GameLog(string.Format(messages.EnteredPocketDimension, Describe(ev.Player)));
        PlayerClassState? ownerState = customClasses.Active.FirstOrDefault(x => x.CustomClass is Scp106Class);
        if (ownerState?.CustomClass is not Scp106Class || ev.Player == ownerState.Player) return;
        ev.IsAllowed = false;
        if (!recentlyHitBy106.Add(ev.Player.PlayerId)) return;
        ev.Player.Damage(70, ownerState.Player, Vector3.zero, DeathTranslations.PocketDecay.Id);
        ev.Player.EnableEffect<Burned>(1, 15);
        ev.Player.EnableEffect<Concussed>(1, 10);
        ev.Player.EnableEffect<Deafened>(1, 15);
        ev.Player.EnableEffect<AmnesiaVision>(1, 6);
        runtime.Schedule(2f, () => recentlyHitBy106.Remove(ev.Player.PlayerId));
    }

    private void OnLeftPocketDimension(PlayerLeftPocketDimensionEventArgs ev) =>
        GameLog(string.Format(messages.LeftPocketDimension, Describe(ev.Player), ev.IsSuccessful ? "escaped" : "failed to escape"));

    private void OnTriggeringTesla(PlayerTriggeringTeslaEventArgs ev)
    {
        if (customClasses.TryGet(ev.Player, out PlayerClassState classState) && classState.CustomClass is Scp106Class scp106 && scp106.Vanish.IsVanished)
            ev.IsAllowed = false;
        GameLog(string.Format(messages.TriggeredTesla, Describe(ev.Player)));
    }

    private void OnInteractingDoor(PlayerInteractingDoorEventArgs ev)
    {
        string doorName = $"{ev.Door.DoorName} {ev.Door.NameTag}";
        bool checkpoint = doorName.IndexOf("Checkpoint", System.StringComparison.OrdinalIgnoreCase) >= 0 || doorName.IndexOf("Chkp", System.StringComparison.OrdinalIgnoreCase) >= 0;
        if (!ev.IsAllowed && checkpoint && ev.Player.Team == Team.SCPs) ev.IsAllowed = true;
        if (doorName.IndexOf("106", System.StringComparison.OrdinalIgnoreCase) >= 0 && ev.Player.Role != RoleTypeId.Scp079 && Generator.List.Count(generator => generator.Engaged) < 2)
        {
            ev.IsAllowed = false;
            ev.Player.SendHint("<color=yellow>2 SCP-079 generators are required to open this door!</color>", 4);
        }
        if (!ev.IsAllowed && ev.Player.Role == RoleTypeId.Scp0492 && !checkpoint)
        {
            int zombies = Player.ReadyList.Count(player => player.Role == RoleTypeId.Scp0492 && Vector3.Distance(player.Position, ev.Door.Position) <= 4f);
            if (zombies >= 4 && ev.Door.Base is IDamageableDoor damageable)
            {
                damageable.ServerDamage(1000000f, DoorDamageType.ServerCommand);
                ev.IsAllowed = true;
            }
            else ev.Player.SendHint($"<color=red>You need at least {4 - zombies} more zombies to break this door.</color>", 4);
        }
        if (ev.IsAllowed) InformNearbyScp939(ev.Door.Rooms.FirstOrDefault(), ActivityMessage(doorName));
        GameLog(string.Format(messages.InteractedDoor, Describe(ev.Player), ev.Door));
    }

    private static string ActivityMessage(string doorName)
    {
        if (doorName.IndexOf("914", System.StringComparison.OrdinalIgnoreCase) >= 0) return "You sense activity at SCP-914";
        if (doorName.IndexOf("106", System.StringComparison.OrdinalIgnoreCase) >= 0) return "You sense activity at SCP-106's chamber";
        if (doorName.IndexOf("GateA", System.StringComparison.OrdinalIgnoreCase) >= 0) return "You sense activity at Gate A";
        if (doorName.IndexOf("GateB", System.StringComparison.OrdinalIgnoreCase) >= 0) return "You sense activity at Gate B";
        if (doorName.IndexOf("Checkpoint", System.StringComparison.OrdinalIgnoreCase) >= 0 || doorName.IndexOf("Chkp", System.StringComparison.OrdinalIgnoreCase) >= 0) return "You sense activity at a checkpoint";
        return string.Empty;
    }

    private static void InformNearbyScp939(Room? room, string message)
    {
        if (room is null || string.IsNullOrEmpty(message)) return;
        foreach (Player scp in Player.ReadyList.Where(player => player.Role == RoleTypeId.Scp939 && player.Room?.Zone == room.Zone))
            scp.SendHint($"<color=yellow>{message}</color>", 2);
    }

    private void OnInteractingElevator(PlayerInteractingElevatorEventArgs ev)
    {
        if (PlayhousePlugin.Instance?.Objectives?.CanUseElevator(ev.Player) == false || PlayhousePlugin.Instance?.SillySunday?.NerfWar == true) ev.IsAllowed = false;
        GameLog(string.Format(messages.CalledElevator, Describe(ev.Player), ev.Elevator));
    }

    private void OnInteractingLocker(PlayerInteractingLockerEventArgs ev) =>
        GameLog(string.Format(messages.UsedLocker, Describe(ev.Player)));

    private void OnWarheadStarting(WarheadStartingEventArgs ev)
    {
        if (IsSillySundayToday())
            PlayhousePlugin.Instance?.RainbowLights?.StartAllRooms();
        GameLog(ev.Player is null ? messages.WarheadStarted : string.Format(messages.PlayerWarheadStarted, Describe(ev.Player)));
    }

    private void OnWarheadStopping(WarheadStoppingEventArgs ev)
    {
        PlayhousePlugin.Instance?.RainbowLights?.StopWarheadRooms();
        GameLog(ev.Player is null ? messages.WarheadStopped : string.Format(messages.PlayerWarheadStopped, Describe(ev.Player)));
    }

    private void OnWarheadDetonated(WarheadDetonatedEventArgs ev)
    {
        GameLog(messages.WarheadDetonated);
        runtime.Schedule(1f, () =>
        {
            foreach (Pickup pickup in Pickup.List.ToArray())
                if (pickup.Position.y < 500f && !pickup.IsLocked) pickup.Destroy();
            foreach (Ragdoll ragdoll in Ragdoll.List.ToArray())
                if (ragdoll.Position.y < 500f) ragdoll.Destroy();
        });
    }

    private static bool IsSillySundayToday() =>
        PlayhousePlugin.Instance?.SillySunday?.IsEnabled == true &&
        System.DateTime.Now.DayOfWeek == System.DayOfWeek.Sunday;

    private void GameLog(string message) =>
        webhooks.Enqueue(WebhookDestination.GameLogs, $"[{System.DateTime.Now:HH:mm:ss}] {message}");
    private void StaffLog(string message) =>
        webhooks.Enqueue(WebhookDestination.StaffChat, $"[{System.DateTime.Now:HH:mm:ss}] {message}");

    private static string Describe(LabApi.Features.Wrappers.Player player) =>
        $"{player.Nickname} ({player.UserId}) [{player.Role}]";
}