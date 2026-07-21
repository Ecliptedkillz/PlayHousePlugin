using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Usables.Scp330;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Commands;
using PlayhousePlugin.Runtime;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.Controllers;

public sealed class VendingMachineController : IDisposable
{
    private const string SchematicName = "Vending_Machine";
    private const int MachinesPerZone = 2;
    private const float CoinCollectionDistance = 0.8f;

    private static readonly System.Random Random = new();

    private static readonly Vector3 DefaultCoinIntake =
        new(-0.9709f, 1.5555f, 0.50f);

    // Product output position. The larger Z value pushes dispensed pickups
    // out in front of the machine instead of leaving them inside the model.
    private static readonly Vector3 DefaultItemOutput =
        new(0f, 0.42f, 1.05f);

    private static readonly Vector3 DefaultButtonStart =
        new(-0.7884f, 1.0326f, 0.50f);

    private static readonly Vector3 DefaultRefundButton =
        new(-0.8468f, 1.3297f, 0.50f);

    // Coin slot position on the vending-machine face.
    // More-negative X moves it visually to the RIGHT on this schematic.
    // Z is pushed forward so the machine panel does not block interaction.
    private static readonly Vector3 DefaultCoinSlot =
        new(-0.93f, 1.56f, 0.58f);

    // Large, easy-to-target interaction pickup.
    private static readonly Vector3 CoinSlotScale =
        Vector3.one * 0.18f;

    private readonly SchematicService schematics;
    private readonly PluginRuntime runtime;

    private readonly List<Machine> machines = new();
    private readonly Dictionary<ushort, Button> buttons = new();

    private ScheduledHandle? startupLoop;
    private ScheduledHandle? coinLoop;

    public VendingMachineController(
        SchematicService schematics,
        PluginRuntime runtime)
    {
        this.schematics = schematics;
        this.runtime = runtime;

        VendingPlacementEditor.Configure(schematics);
    }

    public void Spawn()
    {
        Destroy();

        startupLoop = runtime.Repeat(
            1f,
            TrySpawnMachines);
    }

    private void TrySpawnMachines()
    {
        if (machines.Count > 0)
            return;

        int entranceRoomCount = Room.List.Count(
            room => room.Zone == FacilityZone.Entrance);

        int lightRoomCount = Room.List.Count(
            room => room.Zone == FacilityZone.LightContainment);

        if (entranceRoomCount == 0 ||
            lightRoomCount == 0)
        {
            return;
        }

        SpawnZone(
            FacilityZone.Entrance,
            MachinesPerZone);

        SpawnZone(
            FacilityZone.LightContainment,
            MachinesPerZone);

        if (machines.Count == 0)
            return;

        startupLoop?.Dispose();
        startupLoop = null;

        coinLoop = runtime.Repeat(
            0.5f,
            CollectCoins);
    }

    private void SpawnZone(
        FacilityZone zone,
        int count)
    {
        var possibleRooms =
            new List<(Room Room, Placement[] Placements)>();

        foreach (Room room in Room.List)
        {
            if (room is null ||
                room.Zone != zone)
            {
                continue;
            }

            Placement[]? placements =
                GetPlacements(room);

            if (placements is not null &&
                placements.Length > 0)
            {
                possibleRooms.Add(
                    (room, placements));
            }
        }

        foreach ((Room room, Placement[] placements) in possibleRooms
                     .OrderBy(_ => Random.Next())
                     .Take(count))
        {
            Placement placement =
                placements[
                    Random.Next(placements.Length)];

            SpawnMachine(
                room,
                placement);
        }
    }

    private static Placement[]? GetPlacements(
        Room room)
    {
        if (RoomLocations.TryGetValue(
                room.Name,
                out Placement[]? roomPlacements))
        {
            return roomPlacements;
        }

        string originalName =
            room.GameObject.name;

        string checkpointName =
            NormalizeCheckpointName(originalName);

        if (PrefabLocations.TryGetValue(
                checkpointName,
                out Placement[]? checkpointPlacements))
        {
            return checkpointPlacements;
        }

        string normalizedName =
            NormalizePrefabName(originalName);

        return PrefabLocations.TryGetValue(
            normalizedName,
            out Placement[]? prefabPlacements)
                ? prefabPlacements
                : null;
    }

    private void SpawnMachine(
        Room room,
        Placement placement)
    {
        Vector3 worldPosition =
            room.Transform.TransformPoint(
                placement.LocalPosition);

        Quaternion worldRotation =
            room.Transform.rotation *
            Quaternion.Euler(
                placement.LocalRotation);

        SchematicObject? schematic =
            schematics.Spawn(
                SchematicName,
                worldPosition,
                worldRotation,
                Vector3.one);

        if (schematic is null ||
            schematic.gameObject is null)
        {
            return;
        }

        Machine machine = new(
            schematic,
            room.Zone);

        FindSchematicMarkers(machine);

        machines.Add(machine);
        SpawnButtons(machine);
        SpawnCoinSlot(machine);
    }

    private static string NormalizePrefabName(
        string objectName)
    {
        string name =
            RemoveCloneSuffix(objectName);

        int suffixStart =
            name.LastIndexOf(
                " (",
                StringComparison.Ordinal);

        if (suffixStart >= 0 &&
            name.EndsWith(
                ")",
                StringComparison.Ordinal))
        {
            string suffix =
                name.Substring(
                    suffixStart + 2,
                    name.Length - suffixStart - 3);

            if (int.TryParse(suffix, out _))
            {
                name =
                    name.Substring(
                        0,
                        suffixStart)
                        .Trim();
            }
        }

        return name;
    }

    private static string NormalizeCheckpointName(
        string objectName)
    {
        return RemoveCloneSuffix(objectName);
    }

    private static string RemoveCloneSuffix(
        string objectName)
    {
        string name =
            objectName
                .Trim()
                .ToUpperInvariant();

        while (name.EndsWith(
                   "(CLONE)",
                   StringComparison.Ordinal))
        {
            name =
                name.Substring(
                        0,
                        name.Length - "(CLONE)".Length)
                    .Trim();
        }

        return name;
    }

    private static void FindSchematicMarkers(
        Machine machine)
    {
        Transform schematicTransform =
            machine.Schematic.gameObject.transform;

        Transform[] children =
            schematicTransform.GetComponentsInChildren<Transform>(
                true);

        foreach (Transform child in children)
        {
            if (child is null)
                continue;

            string markerName =
                NormalizeMarkerName(child.name);

            if (IsNamed(
                    markerName,
                    "coinintake",
                    "coininput",
                    "coininsert",
                    "intake"))
            {
                machine.CoinIntakeMarker = child;
                continue;
            }

            if (IsNamed(
                    markerName,
                    "itemoutput",
                    "productoutput",
                    "dispenseroutput",
                    "output"))
            {
                machine.ItemOutputMarker = child;
                continue;
            }

            if (IsNamed(
                    markerName,
                    "refundbutton",
                    "buttonrefund",
                    "refund"))
            {
                machine.ButtonMarkers[9] = child;
                continue;
            }

            if (TryGetButtonIndex(
                    markerName,
                    out int buttonIndex))
            {
                machine.ButtonMarkers[buttonIndex] = child;
            }
        }
    }

    private static string NormalizeMarkerName(
        string markerName)
    {
        return markerName
            .Replace("_", string.Empty)
            .Replace("-", string.Empty)
            .Replace(" ", string.Empty)
            .ToLowerInvariant();
    }

    private static bool IsNamed(
        string actualName,
        params string[] expectedNames)
    {
        return expectedNames.Any(
            expected =>
                actualName.Equals(
                    expected,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryGetButtonIndex(
        string markerName,
        out int index)
    {
        index = -1;

        string numberText;

        if (markerName.StartsWith(
                "button",
                StringComparison.OrdinalIgnoreCase))
        {
            numberText =
                markerName.Substring("button".Length);
        }
        else if (markerName.StartsWith(
                     "vendbutton",
                     StringComparison.OrdinalIgnoreCase))
        {
            numberText =
                markerName.Substring("vendbutton".Length);
        }
        else
        {
            return false;
        }

        if (!int.TryParse(
                numberText,
                out index))
        {
            return false;
        }

        return index >= 0 &&
               index <= 8;
    }

    private void SpawnButtons(
        Machine machine)
    {
        for (int index = 0;
             index < Products.Length;
             index++)
        {
            GetButtonTransform(
                machine,
                index,
                out Vector3 worldPosition,
                out Quaternion worldRotation);

            SpawnButton(
                machine,
                index,
                worldPosition,
                worldRotation);
        }

        GetButtonTransform(
            machine,
            9,
            out Vector3 refundPosition,
            out Quaternion refundRotation);

        SpawnButton(
            machine,
            9,
            refundPosition,
            refundRotation);
    }

    private static void GetButtonTransform(
        Machine machine,
        int index,
        out Vector3 position,
        out Quaternion rotation)
    {
        if (machine.ButtonMarkers.TryGetValue(
                index,
                out Transform marker) &&
            marker is not null)
        {
            position = marker.position;
            rotation = marker.rotation;
            return;
        }

        Transform schematicTransform =
            machine.Schematic.gameObject.transform;

        Vector3 localPosition;

        if (index == 9)
        {
            localPosition =
                DefaultRefundButton;
        }
        else
        {
            int row = index / 3;
            int column = index % 3;

            localPosition =
                DefaultButtonStart +
                new Vector3(
                    -0.09125f * column,
                    0.09125f * row,
                    0f);
        }

        position =
            schematicTransform.TransformPoint(
                localPosition);

        rotation =
            schematicTransform.rotation *
            Quaternion.Euler(
                0f,
                -90f,
                0f);
    }

    private void SpawnButton(
        Machine machine,
        int index,
        Vector3 position,
        Quaternion rotation)
    {
        Pickup? pickup = Pickup.Create(
            ItemType.ArmorLight,
            position,
            rotation,
            Vector3.one * 0.09f);

        if (pickup is null)
            return;

        pickup.Spawn();
        pickup.IsLocked = false;

        if (pickup.Rigidbody is not null)
        {
            pickup.Rigidbody.linearVelocity =
                Vector3.zero;

            pickup.Rigidbody.angularVelocity =
                Vector3.zero;

            pickup.Rigidbody.useGravity = false;
            pickup.Rigidbody.isKinematic = true;
            pickup.Rigidbody.detectCollisions = true;
        }

        machine.ButtonPickups.Add(pickup);

        buttons[pickup.Serial] =
            new Button(
                machine,
                index);
    }

    private void SpawnCoinSlot(Machine machine)
    {
        Transform schematicTransform =
            machine.Schematic.gameObject.transform;

        Vector3 position =
            schematicTransform.TransformPoint(DefaultCoinSlot);

        Quaternion rotation =
            schematicTransform.rotation *
            Quaternion.Euler(0f, -90f, 0f);

        Pickup? slot = Pickup.Create(
            ItemType.ArmorLight,
            position,
            rotation,
            CoinSlotScale);

        if (slot is null)
            return;

        slot.Spawn();
        slot.IsLocked = false;

        if (slot.Rigidbody is not null)
        {
            slot.Rigidbody.linearVelocity = Vector3.zero;
            slot.Rigidbody.angularVelocity = Vector3.zero;
            slot.Rigidbody.useGravity = false;
            slot.Rigidbody.isKinematic = true;
            slot.Rigidbody.detectCollisions = true;
        }

        machine.CoinSlotPickup = slot;
        machine.CoinIntakeOverridePosition = position;

        // Register the slot through the exact same interaction dictionary as
        // the working product/refund buttons. Index 10 is reserved for coins.
        machine.ButtonPickups.Add(slot);
        buttons[slot.Serial] = new Button(machine, 10);
    }

    public void OnSearchingPickup(
        PlayerSearchingPickupEventArgs ev)
    {
        if (ev.Pickup is null)
            return;

        if (!buttons.TryGetValue(
                ev.Pickup.Serial,
                out Button button))
        {
            return;
        }

        ev.IsAllowed = false;
        ev.Pickup.IsInUse = false;

        HandleButton(
            button,
            ev.Player);
    }

    private static void HandleCoinSlot(
        Machine machine,
        Player player)
    {
        if (machine.Schematic is null ||
            machine.Schematic.gameObject is null)
        {
            return;
        }

        Item? heldItem = player.CurrentItem;

        if (heldItem is null || heldItem.Type != ItemType.Coin)
        {
            player.SendHint(
                "Hold a coin in your hand, then interact with the coin slot.",
                3f);
            return;
        }

        player.RemoveItem(heldItem);
        machine.Coins++;

        player.SendHint(
            $"Coin inserted!\nBalance: ${machine.Coins * 0.25f:0.00}",
            3f);
    }

    private void HandleButton(
        Button button,
        Player player)
    {
        Machine machine =
            button.Machine;

        if (!machines.Contains(machine))
            return;

        if (machine.Schematic is null ||
            machine.Schematic.gameObject is null)
        {
            return;
        }

        if (button.Index == 9)
        {
            HandleRefund(
                machine,
                player);

            return;
        }

        if (button.Index == 10)
        {
            HandleCoinSlot(
                machine,
                player);

            return;
        }

        if (button.Index < 0 ||
            button.Index >= Products.Length)
        {
            return;
        }

        Product product =
            Products[button.Index];

        if (machine.Coins < product.Price)
        {
            int missing =
                product.Price -
                machine.Coins;

            player.SendHint(
                $"You need {missing} more coin(s).",
                3f);

            return;
        }

        machine.Coins -=
            product.Price;

        bool dispensed = Dispense(
            machine,
            product,
            player);

        if (!dispensed)
        {
            // Return the balance when pickup creation fails.
            machine.Coins += product.Price;

            player.SendHint(
                "The vending machine could not dispense that item. Your coins were returned.",
                4f);

            return;
        }

        player.SendHint(
            $"Purchase complete!\n" +
            $"Balance: ${machine.Coins * 0.25f:0.00}",
            3f);
    }

    private static void HandleRefund(
        Machine machine,
        Player player)
    {
        if (machine.Coins <= 0)
        {
            player.SendHint(
                "There are no coins to refund.",
                2f);

            return;
        }

        int refundedCoins =
            machine.Coins;

        Refund(machine);

        player.SendHint(
            $"Refunded {refundedCoins} coin(s).",
            3f);
    }

    private void CollectCoins()
    {
        foreach (Machine machine in machines.ToArray())
        {
            if (machine.Schematic == null)
                continue;
    
            Vector3 intakePosition;
    
            try
            {
                intakePosition = GetCoinIntakePosition(machine);
            }
            catch (NullReferenceException)
            {
                continue;
            }
    
            foreach (Pickup pickup in Pickup.List.ToArray())
            {
                if (pickup == null || pickup.IsDestroyed)
                    continue;
    
                if (buttons.ContainsKey(pickup.Serial))
                    continue;
    
                if (pickup.Type != ItemType.Coin)
                    continue;
    
                if (Vector3.Distance(pickup.Position, intakePosition) > CoinCollectionDistance)
                    continue;
    
                pickup.Destroy();
                machine.Coins++;
            }
        }
    }

    private static Vector3 GetCoinIntakePosition(
        Machine machine)
    {
        if (machine.CoinIntakeOverridePosition.HasValue)
            return machine.CoinIntakeOverridePosition.Value;

        if (machine.CoinIntakeMarker is not null)
            return machine.CoinIntakeMarker.position;

        return machine.Schematic.gameObject.transform.TransformPoint(
            DefaultCoinIntake);
    }

    private static Vector3 GetItemOutputPosition(
        Machine machine)
    {
        if (machine.ItemOutputMarker is not null)
            return machine.ItemOutputMarker.position;

        return machine.Schematic.gameObject.transform.TransformPoint(
            DefaultItemOutput);
    }

    private static bool Dispense(
        Machine machine,
        Product product,
        Player buyer)
    {
        // Effect products do not create a pickup. Apply the healing directly so
        // the purchase works even when SCP-330 regeneration is unavailable.
        if (product.EffectOnly)
        {
            const float healAmount = 25f;

            float healthBefore = buyer.Health;
            float healthAfter = Mathf.Min(
                buyer.MaxHealth,
                healthBefore + healAmount);

            if (healthAfter <= healthBefore)
            {
                buyer.SendHint(
                    "You are already at full health.",
                    3f);

                return false;
            }

            buyer.Health = healthAfter;

            buyer.SendHint(
                $"Healing effect applied!\n" +
                $"Restored {healthAfter - healthBefore:0} HP.",
                3f);

            return true;
        }

        ItemType itemType =
            product.Mystery
                ? PickMystery(machine.Zone)
                : product.Item;

        if (itemType == ItemType.None)
            return false;

        Transform machineTransform =
            machine.Schematic.gameObject.transform;

        Vector3 outputPosition =
            GetItemOutputPosition(machine);

        // Even when a schematic marker exists, push the item outward and up
        // so it cannot be hidden or trapped inside the vending-machine mesh.
        outputPosition +=
            machineTransform.forward * 0.35f +
            machineTransform.up * 0.12f;

        Quaternion outputRotation =
            machine.ItemOutputMarker is not null
                ? machine.ItemOutputMarker.rotation
                : machineTransform.rotation;

        bool spawned = SpawnOutputPickup(
            itemType,
            outputPosition,
            outputRotation);

        if (!spawned)
            return false;

        if (itemType is
            ItemType.Ammo9x19 or
            ItemType.Ammo556x45)
        {
            Vector3 secondPosition =
                outputPosition +
                machineTransform.right * 0.18f;

            SpawnOutputPickup(
                itemType,
                secondPosition,
                outputRotation);
        }

        return true;
    }

    private static bool SpawnOutputPickup(
        ItemType itemType,
        Vector3 position,
        Quaternion rotation)
    {
        Pickup? pickup = Pickup.Create(
            itemType,
            position,
            rotation,
            Vector3.one);

        if (pickup is null)
            return false;

        pickup.Spawn();
        pickup.IsLocked = false;

        if (pickup.Rigidbody is not null)
        {
            // Re-apply the requested transform after spawning. Some pickup
            // prefabs adjust their transform during Spawn().
            pickup.Rigidbody.position = position;
            pickup.Rigidbody.rotation = rotation;
            pickup.Rigidbody.linearVelocity = Vector3.zero;
            pickup.Rigidbody.angularVelocity = Vector3.zero;
            pickup.Rigidbody.isKinematic = false;
            pickup.Rigidbody.useGravity = true;
            pickup.Rigidbody.detectCollisions = true;
        }

        return true;
    }

    private static ItemType PickMystery(
        FacilityZone zone)
    {
        ItemType[] commonItems =
        {
            ItemType.GrenadeFlash,
            ItemType.Adrenaline,
            ItemType.SCP2176,
            ItemType.SCP500
        };

        ItemType[] zoneItems =
            zone == FacilityZone.Entrance
                ? new[]
                {
                    ItemType.KeycardMTFOperative,
                    ItemType.ArmorHeavy
                }
                : new[]
                {
                    ItemType.GunRevolver,
                    ItemType.KeycardZoneManager,
                    ItemType.KeycardResearchCoordinator
                };

        ItemType[] pool =
            commonItems
                .Concat(zoneItems)
                .ToArray();

        return pool[
            Random.Next(pool.Length)];
    }

    private static void Refund(
        Machine machine)
    {
        int coinCount =
            machine.Coins;

        machine.Coins = 0;

        Vector3 outputPosition =
            GetItemOutputPosition(machine);

        Quaternion outputRotation =
            machine.ItemOutputMarker is not null
                ? machine.ItemOutputMarker.rotation
                : machine.Schematic.gameObject.transform.rotation;

        Transform machineTransform =
            machine.Schematic.gameObject.transform;

        for (int index = 0;
             index < coinCount;
             index++)
        {
            Vector3 offset =
                machineTransform.right *
                ((index % 3) * 0.08f);

            offset +=
                machineTransform.up *
                ((index / 3) * 0.04f);

            SpawnOutputPickup(
                ItemType.Coin,
                outputPosition + offset,
                outputRotation);
        }
    }

    public void Destroy()
    {
        startupLoop?.Dispose();
        startupLoop = null;

        coinLoop?.Dispose();
        coinLoop = null;

        foreach (Machine machine in
                 machines.ToArray())
        {
            foreach (Pickup pickup in
                     machine.ButtonPickups.ToArray())
            {
                buttons.Remove(
                    pickup.Serial);

                if (!pickup.IsDestroyed)
                    pickup.Destroy();
            }

            schematics.Destroy(
                machine.Schematic);
        }

        buttons.Clear();
        machines.Clear();
    }

    public void Dispose()
    {
        Destroy();
        VendingPlacementEditor.Unconfigure(schematics);
    }

    private sealed class Machine
    {
        public Machine(
            SchematicObject schematic,
            FacilityZone zone)
        {
            Schematic = schematic;
            Zone = zone;
        }

        public SchematicObject Schematic { get; }
        public FacilityZone Zone { get; }
        public int Coins { get; set; }
        public Transform? CoinIntakeMarker { get; set; }
        public Transform? ItemOutputMarker { get; set; }
        public Pickup? CoinSlotPickup { get; set; }
        public Vector3? CoinIntakeOverridePosition { get; set; }

        public Dictionary<int, Transform> ButtonMarkers
        {
            get;
        } = new();

        public List<Pickup> ButtonPickups
        {
            get;
        } = new();
    }

    private readonly struct Button
    {
        public Button(
            Machine machine,
            int index)
        {
            Machine = machine;
            Index = index;
        }

        public Machine Machine { get; }
        public int Index { get; }
    }

    private readonly struct Product
    {
        public Product(
            int price,
            ItemType item = ItemType.None,
            bool effectOnly = false,
            bool mystery = false)
        {
            Price = price;
            Item = item;
            EffectOnly = effectOnly;
            Mystery = mystery;
        }

        public int Price { get; }
        public ItemType Item { get; }
        public bool EffectOnly { get; }
        public bool Mystery { get; }
    }

    private readonly struct Placement
    {
        public Placement(
            Vector3 localPosition,
            Vector3 localRotation)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
        }

        public Vector3 LocalPosition { get; }
        public Vector3 LocalRotation { get; }
    }

    private static readonly Dictionary<RoomName, Placement[]> RoomLocations =
        new()
        {
            [RoomName.EzOfficeLarge] = new[]
            {
                new Placement(
                    new Vector3(3.898f, 0.05f, -6.285f), 
                    new Vector3(0f, -86.501f, 0f))
            },
            
            [RoomName. EzOfficeStoried] = new[]
            {
                new Placement(
                    new Vector3(7.037f, 2.906f, -5.93f), 
                    new Vector3(0f, -90f, 0f))
            },
            
            [RoomName.EzOfficeSmall] = new[]
            {
                new Placement(
                    new Vector3(3.7f, 0f, 9.6f),
                    new Vector3(0f, 180f, 0f))
            },

            [RoomName.LczComputerRoom] = new[]
            {
                new Placement(
                    new Vector3(-5.922f, 0.049f, -4.613f), 
                    new Vector3(0f, 0.5f, 0f))
            },

            
            [RoomName.LczToilets] = new[]
            {
                new Placement(
                    new Vector3(4.478f, 0.049f, -0.771f), 
                    new Vector3(0f, 0.196f, 0f))
            },

            [RoomName.Lcz330] = new[]
            {
                new Placement(
                    new Vector3(-7.8f, 0f, 4.3f),
                    new Vector3(0f, 180f, 0f))
            },

            [RoomName. EzGateA] = new[]
            {
                new Placement(
                    new Vector3(1.826f, 0f, 6.542f), 
                    new Vector3(0f, -134.697f, 0f))
            },
            
            [RoomName. EzGateB] = new[]
            {
                new Placement (
                    new Vector3(-6.055f, 0.06f, 6.9f), 
                    new Vector3(0f, -177.898f, 0f))
            },

            [RoomName.LczGreenhouse] = new[]
            {
                new Placement(
                    new Vector3(-4.828f, 0.227f, 7.026f), 
                    new Vector3(0f, 175.1f, 0f))
            },
        };

    // Add unnamed hallway/prefab placements here using the output from:
    // vendingplacement save
    private static readonly Dictionary<string, Placement[]> PrefabLocations =
        new(StringComparer.OrdinalIgnoreCase)
        {
        };

    private static readonly Product[] Products =
    {
        new(
            1,
            ItemType.Painkillers),

        new(
            1,
            effectOnly: true),

        new(
            1,
            effectOnly: true),

        new(
            1,
            effectOnly: true),

        new(
            2,
            ItemType.Medkit),

        new(
            1,
            effectOnly: true),

        new(
            2,
            ItemType.Ammo9x19),

        new(
            4,
            mystery: true),

        new(
            2,
            ItemType.Ammo556x45)
    };
}