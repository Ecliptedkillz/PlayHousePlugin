using System;
using System.Collections.Generic;
using System.Linq;
using CustomPlayerEffects;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayerRoles;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public sealed class EngineerClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;

    public EngineerClass(
        Player player,
        Runtime.PluginRuntime runtime,
        bool chaos)
        : base(player)
    {
        this.chaos = chaos;

        var entrance = new EngineerBuildingAbility(
            player,
            runtime,
            chaos,
            BuildingKind.TeleporterEntrance);

        var exit = new EngineerBuildingAbility(
            player,
            runtime,
            chaos,
            BuildingKind.TeleporterExit);

        // Link the entrance and exit together.
        entrance.SetPairedTeleporter(exit);
        exit.SetPairedTeleporter(entrance);

        abilities = new AbilityBase[]
        {
            new EngineerBuildingAbility(
                player,
                runtime,
                chaos,
                BuildingKind.Dispenser),

            new EngineerBuildingAbility(
                player,
                runtime,
                chaos,
                BuildingKind.Speedpad),

            entrance,
            exit
        };

        player.ClearInventory();

        player.AddItem(
            chaos
                ? ItemType.KeycardChaosInsurgency
                : ItemType.KeycardMTFOperative);

        player.AddItem(ItemType.GunShotgun);
        player.AddItem(ItemType.GunCOM18);

        if (chaos)
            player.AddItem(ItemType.Adrenaline);
        else
            player.AddItem(ItemType.Radio);

        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.ArmorCombat);

        player.SetAmmo(ItemType.Ammo12gauge, 42);
        player.SetAmmo(ItemType.Ammo9x19, 60);

        player.CustomInfo =
            $"{(chaos ? "Chaos " : string.Empty)}Engineer\n(Custom Class)";
    }

    public override string Name =>
        chaos ? "Chaos Engineer" : "NTF Engineer";

    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

public sealed class MachinistClass : CustomClassBase
{
    private readonly bool chaos;
    private readonly IReadOnlyList<AbilityBase> abilities;

    public MachinistClass(
        Player player,
        Runtime.PluginRuntime runtime,
        bool chaos)
        : base(player)
    {
        this.chaos = chaos;

        abilities = new AbilityBase[]
        {
            new EngineerBuildingAbility(
                player,
                runtime,
                chaos,
                BuildingKind.MiniDispenser),

            new EngineerBuildingAbility(
                player,
                runtime,
                chaos,
                BuildingKind.Relocator)
        };

        player.ClearInventory();

        player.AddItem(ItemType.GunShotgun);
        player.AddItem(ItemType.GunCOM15);
        player.AddItem(ItemType.ArmorCombat);
        player.AddItem(ItemType.Medkit);
        player.AddItem(ItemType.Painkillers);

        player.AddItem(
            chaos
                ? ItemType.KeycardChaosInsurgency
                : ItemType.KeycardMTFOperative);

        player.SetAmmo(ItemType.Ammo12gauge, 54);
        player.SetAmmo(ItemType.Ammo9x19, 60);

        player.CustomInfo =
            $"{(chaos ? "Chaos " : "NTF ")}Machinist\n(Custom Class)";
    }

    public override string Name =>
        chaos ? "Chaos Machinist" : "NTF Machinist";

    public override IReadOnlyList<AbilityBase> ActiveAbilities => abilities;
}

public enum BuildingKind
{
    Dispenser,
    Speedpad,
    TeleporterEntrance,
    TeleporterExit,
    MiniDispenser,
    Relocator
}

public sealed class EngineerBuildingAbility : CooldownAbilityBase, IDisposable
{
    private static readonly System.Random Random = new();

    private readonly Runtime.PluginRuntime runtime;
    private readonly bool chaos;
    private readonly BuildingKind kind;
    private readonly Dictionary<int, int> charge = new();

    private EngineerBuildingAbility? pairedTeleporter;
    private Runtime.ScheduledHandle? loop;
    private SchematicObject? schematic;

    private Vector3 position;
    private Quaternion placementRotation;
    private Vector3 placementScale;

    /*
     * This only applies to the entrance.
     *
     * false = static entrance
     * true  = spinning entrance
     */
    private bool entranceIsSpinning;

    public EngineerBuildingAbility(
        Player player,
        Runtime.PluginRuntime runtime,
        bool chaos,
        BuildingKind kind)
        : base(player)
    {
        this.runtime = runtime;
        this.chaos = chaos;
        this.kind = kind;
    }

    public override string Name => kind switch
    {
        BuildingKind.TeleporterEntrance => "Teleporter Entrance",
        BuildingKind.TeleporterExit => "Teleporter Exit",
        BuildingKind.MiniDispenser => "Mini Dispenser",
        BuildingKind.Relocator => "Relocator",
        _ => kind.ToString()
    };

    public override double CooldownSeconds =>
        kind == BuildingKind.Relocator ? 20 : 30;

    public bool IsBuilt => schematic is not null;

    private bool IsTeleporter =>
        kind is BuildingKind.TeleporterEntrance
            or BuildingKind.TeleporterExit;

    public void SetPairedTeleporter(
        EngineerBuildingAbility pairedAbility)
    {
        pairedTeleporter = pairedAbility;
    }

    protected override bool UseCooldownAbility(out string response)
    {
        if (IsBuilt)
        {
            Destroy();

            response = $"{Name} destroyed.";
            return true;
        }

        if (Player.Role == RoleTypeId.Tutorial ||
            Player.Room is null ||
            Player.Room.Zone == FacilityZone.Other)
        {
            response = "You cannot build here.";
            return false;
        }

        float verticalOffset = kind switch
        {
            BuildingKind.TeleporterEntrance => -0.9f,
            BuildingKind.TeleporterExit => -0.9f,
            BuildingKind.Speedpad => -1.1f,
            BuildingKind.Dispenser => -1.0f,
            BuildingKind.MiniDispenser => -1.0f,
            BuildingKind.Relocator => -0.7f,
            _ => -1.0f
        };

        position =
            Player.Position +
            Vector3.up * verticalOffset;

        placementRotation = Quaternion.Euler(
            0f,
            Player.Camera.rotation.eulerAngles.y,
            0f);

        placementScale =
            kind == BuildingKind.Speedpad
                ? Vector3.one * 2f
                : Vector3.one;

        string schematicName = GetInitialSchematicName();

        schematic = PlayhousePlugin.Instance?.Schematics.Spawn(
            schematicName,
            position,
            placementRotation,
            placementScale);

        if (schematic is null)
        {
            response =
                $"Required schematic '{schematicName}' is unavailable.";

            return false;
        }

        /*
         * The entrance always starts static.
         * The exit has its own separate static schematic.
         */
        if (kind == BuildingKind.TeleporterEntrance)
            entranceIsSpinning = false;

        int ticks = 0;

        loop = runtime.Repeat(
            1f,
            () =>
            {
                ticks++;

                Tick();

                if (kind == BuildingKind.MiniDispenser &&
                    ticks >= 15)
                {
                    Destroy();
                }
            });

        /*
         * If both entrance and exit now exist, turn on the entrance.
         */
        if (IsTeleporter)
            RefreshTeleporterVisuals();

        response = $"{Name} built.";
        return true;
    }

    private string GetInitialSchematicName()
    {
        return kind switch
        {
            BuildingKind.Dispenser =>
                chaos
                    ? "DispenserGreen"
                    : "Dispenser",

            BuildingKind.Speedpad =>
                "SpeedPad",

            /*
             * Entrance starts off/static until the exit is placed.
             */
            BuildingKind.TeleporterEntrance =>
                GetStaticEntranceName(),

            /*
             * Exit always uses its own static schematic.
             */
            BuildingKind.TeleporterExit =>
                GetExitName(),

            BuildingKind.MiniDispenser =>
                chaos
                    ? "MiniDispenserChaos"
                    : "MiniDispenserMTF",

            BuildingKind.Relocator =>
                chaos
                    ? "RandomizerChaos"
                    : "RandomizerMTF",

            _ =>
                chaos
                    ? "RandomizerChaos"
                    : "RandomizerMTF"
        };
    }

    private string GetStaticEntranceName()
    {
        return chaos
            ? "TeleporterGreenStatic"
            : "TeleporterBlueStatic";
    }

    private string GetSpinningEntranceName()
    {
        return chaos
            ? "TeleporterGreenSpinning"
            : "TeleporterBlueSpinning";
    }

    private string GetExitName()
    {
        return chaos
            ? "TeleporterExitGreen"
            : "TeleporterExitBlue";
    }

    private void RefreshTeleporterVisuals()
    {
        EngineerBuildingAbility? entrance =
            kind == BuildingKind.TeleporterEntrance
                ? this
                : pairedTeleporter;

        EngineerBuildingAbility? exit =
            kind == BuildingKind.TeleporterExit
                ? this
                : pairedTeleporter;

        if (entrance is null ||
            entrance.kind != BuildingKind.TeleporterEntrance)
        {
            return;
        }

        /*
         * The entrance spins only when both objects are built.
         *
         * The exit is never changed and never spins.
         */
        bool shouldSpin =
            entrance.IsBuilt &&
            exit?.IsBuilt == true;

        entrance.SetEntranceVisual(shouldSpin);
    }

    private void SetEntranceVisual(bool spinning)
    {
        if (kind != BuildingKind.TeleporterEntrance ||
            schematic is null)
        {
            return;
        }

        /*
         * The entrance already has the correct schematic.
         */
        if (entranceIsSpinning == spinning)
        {
            if (spinning)
                StartTeleporterSpin();

            return;
        }

        string desiredSchematicName =
            spinning
                ? GetSpinningEntranceName()
                : GetStaticEntranceName();

        SchematicObject oldSchematic = schematic;

        /*
         * Temporarily clear the reference while replacing it.
         */
        schematic = null;

        PlayhousePlugin.Instance?.Schematics.Destroy(
            oldSchematic);

        schematic = PlayhousePlugin.Instance?.Schematics.Spawn(
            desiredSchematicName,
            position,
            placementRotation,
            placementScale);

        if (schematic is null)
        {
            entranceIsSpinning = false;
            return;
        }

        entranceIsSpinning = spinning;

        if (spinning)
            StartTeleporterSpin();
    }

    private void StartTeleporterSpin()
    {
        if (kind != BuildingKind.TeleporterEntrance ||
            schematic is null)
        {
            return;
        }

        /*
         * First, try to find the exported spinning parent called
         * "GameObject".
         */
        GameObject? spinningPart =
            schematic.AttachedBlocks.FirstOrDefault(
                block =>
                    block is not null &&
                    string.Equals(
                        block.name,
                        "GameObject",
                        StringComparison.OrdinalIgnoreCase));

        /*
         * Fallback:
         * Find a top-level object that contains child objects and
         * is not named TeleporterBase.
         */
        spinningPart ??=
            schematic.AttachedBlocks
                .Where(block => block is not null)
                .FirstOrDefault(
                    block =>
                        block.transform.parent ==
                        schematic.transform &&
                        block.transform.childCount > 0 &&
                        !string.Equals(
                            block.name,
                            "TeleporterBase",
                            StringComparison.OrdinalIgnoreCase));

        if (spinningPart is null)
            return;

        /*
         * Stop Unity physics from fighting the scripted rotation.
         */
        Rigidbody? body =
            spinningPart.GetComponent<Rigidbody>();

        if (body is not null)
        {
            body.useGravity = false;
            body.isKinematic = true;
            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        SchematicSpinner? spinner =
            spinningPart.GetComponent<SchematicSpinner>();

        if (spinner is null)
        {
            spinner =
                spinningPart.AddComponent<SchematicSpinner>();
        }

        spinner.DegreesPerSecond = 90f;
    }

    private void Tick()
    {
        if (!Player.ReadyList.Contains(Player) ||
            !Player.IsAlive)
        {
            Destroy();
            return;
        }

        if (kind is BuildingKind.Dispenser
            or BuildingKind.MiniDispenser)
        {
            Dispense(
                kind == BuildingKind.MiniDispenser
                    ? 5
                    : 10);
        }
        else if (kind == BuildingKind.Speedpad)
        {
            Speedpad();
        }
        else if (kind == BuildingKind.TeleporterEntrance)
        {
            PairedTeleport();
        }
        else if (kind == BuildingKind.Relocator)
        {
            Relocate();
        }
    }

    private IEnumerable<Player> Nearby(float radius)
    {
        return Player.ReadyList.Where(
            target =>
                target.IsAlive &&
                Vector3.Distance(
                    target.Position,
                    position) <= radius &&
                IsAlly(target));
    }

    private bool IsAlly(Player target)
    {
        return target.IsDisarmed ||
               (chaos
                   ? target.Team is Team.ChaosInsurgency
                       or Team.ClassD
                   : target.Team is Team.FoundationForces
                       or Team.Scientists);
    }

    private void Dispense(int amount)
    {
        foreach (Player target in Nearby(4f))
        {
            target.Health = Math.Min(
                target.MaxHealth,
                target.Health + amount);

            if (!target.IsDisarmed)
            {
                ushort ammo = (ushort)amount;

                AddAmmo(
                    target,
                    ItemType.Ammo9x19,
                    ammo);

                AddAmmo(
                    target,
                    ItemType.Ammo556x45,
                    ammo);

                AddAmmo(
                    target,
                    ItemType.Ammo762x39,
                    ammo);

                AddAmmo(
                    target,
                    ItemType.Ammo12gauge,
                    ammo);

                AddAmmo(
                    target,
                    ItemType.Ammo44cal,
                    ammo);
            }

            target.SendHint(
                "<color=red>HP & ammo replenished</color>",
                1);
        }
    }

    private static void AddAmmo(
        Player player,
        ItemType type,
        ushort amount)
    {
        ushort currentAmmo = player.GetAmmo(type);
        ushort maximumAmmo = GetMaximumAmmo(player, type);
    
        // The player is already full for this ammo type.
        if (currentAmmo >= maximumAmmo)
            return;
    
        ushort ammoToGive = (ushort)Math.Min(
            amount,
            maximumAmmo - currentAmmo);
    
        player.SetAmmo(
            type,
            (ushort)(currentAmmo + ammoToGive));
    }
    
    private static ushort GetMaximumAmmo(
        Player player,
        ItemType ammoType)
    {
        bool hasHeavyArmor =
            player.Items.Any(item =>
                item.Type == ItemType.ArmorHeavy);
    
        bool hasCombatArmor =
            player.Items.Any(item =>
                item.Type == ItemType.ArmorCombat);
    
        bool hasLightArmor =
            player.Items.Any(item =>
                item.Type == ItemType.ArmorLight);
    
        return ammoType switch
        {
            ItemType.Ammo9x19 when hasHeavyArmor => 210,
            ItemType.Ammo9x19 when hasCombatArmor => 170,
            ItemType.Ammo9x19 when hasLightArmor => 70,
            ItemType.Ammo9x19 => 40,
    
            ItemType.Ammo556x45 when hasHeavyArmor => 200,
            ItemType.Ammo556x45 when hasCombatArmor => 120,
            ItemType.Ammo556x45 => 40,
    
            ItemType.Ammo762x39 when hasHeavyArmor => 200,
            ItemType.Ammo762x39 when hasCombatArmor => 120,
            ItemType.Ammo762x39 => 40,
    
            ItemType.Ammo12gauge when hasHeavyArmor => 74,
            ItemType.Ammo12gauge when hasCombatArmor => 54,
            ItemType.Ammo12gauge => 14,
    
            ItemType.Ammo44cal when hasHeavyArmor => 68,
            ItemType.Ammo44cal when hasCombatArmor => 48,
            ItemType.Ammo44cal => 18,
    
            _ => ushort.MaxValue
        };
    }

    private void Speedpad()
    {
        foreach (Player target in Nearby(3f))
        {
            target.EnableEffect<MovementBoost>(
                15,
                10);

            target.SendHint(
                "<color=red>Given 10s speed boost!</color>",
                1);
        }
    }

    private void PairedTeleport()
    {
        /*
         * Teleportation is disabled until the exit exists.
         */
        if (pairedTeleporter?.schematic is null)
        {
            charge.Clear();
            return;
        }

        /*
         * Confirm the paired ability is actually the exit.
         */
        if (pairedTeleporter.kind != BuildingKind.TeleporterExit)
        {
            charge.Clear();
            return;
        }

        HashSet<int> nearbyPlayerIds = new();

        foreach (Player target in Nearby(1.6f).ToArray())
        {
            nearbyPlayerIds.Add(target.PlayerId);

            int count =
                charge.TryGetValue(
                    target.PlayerId,
                    out int current)
                    ? current + 1
                    : 1;

            charge[target.PlayerId] = count;

            if (count >= 5)
            {
                target.Position =
                    pairedTeleporter.position +
                    Vector3.up * 1.5f;

                charge.Remove(target.PlayerId);
            }
            else
            {
                target.SendHint(
                    $"<color=green>Teleporter charging: {count}/5</color>",
                    1);
            }
        }

        /*
         * Reset the charge for players who walked away before the
         * teleporter finished charging.
         */
        foreach (int playerId in charge.Keys.ToArray())
        {
            if (!nearbyPlayerIds.Contains(playerId))
                charge.Remove(playerId);
        }
    }

    private void Relocate()
    {
        HashSet<int> nearbyPlayerIds = new();

        foreach (Player target in Nearby(1.6f).ToArray())
        {
            nearbyPlayerIds.Add(target.PlayerId);

            int count =
                charge.TryGetValue(
                    target.PlayerId,
                    out int current)
                    ? current + 1
                    : 1;

            charge[target.PlayerId] = count;

            if (count < 5)
            {
                target.SendHint(
                    $"<color=green>Relocator charging: {count}/5</color>",
                    1);

                continue;
            }

            Room[] rooms =
                Room.List
                    .Where(
                        room =>
                            room.Zone == target.Zone &&
                            room.Zone != FacilityZone.Other)
                    .ToArray();

            if (rooms.Length > 0)
            {
                target.Position =
                    rooms[Random.Next(rooms.Length)].Position +
                    Vector3.up * 1.5f;
            }

            charge.Remove(target.PlayerId);
        }

        foreach (int playerId in charge.Keys.ToArray())
        {
            if (!nearbyPlayerIds.Contains(playerId))
                charge.Remove(playerId);
        }
    }

    private void Destroy()
    {
        loop?.Dispose();
        loop = null;

        SchematicObject? oldSchematic = schematic;

        /*
         * Clear this first so the paired object sees that this
         * teleporter is no longer built.
         */
        schematic = null;

        if (oldSchematic is not null)
        {
            PlayhousePlugin.Instance?.Schematics.Destroy(
                oldSchematic);
        }

        entranceIsSpinning = false;
        charge.Clear();

        /*
         * When the exit is removed, this changes the remaining
         * entrance back to its static schematic.
         *
         * When the entrance is removed, the exit remains unchanged.
         */
        if (IsTeleporter)
            pairedTeleporter?.RefreshTeleporterVisuals();
    }

    public void Dispose()
    {
        Destroy();
    }
}

/*
 * This component is only added to the spinning entrance schematic.
 * The exit schematic never receives this component.
 */
public sealed class SchematicSpinner : MonoBehaviour
{
    public float DegreesPerSecond { get; set; } = 90f;

    private void Update()
    {
        transform.Rotate(
            0f,
            DegreesPerSecond * Time.deltaTime,
            0f,
            Space.Self);
    }
}