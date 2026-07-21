using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using PlayhousePlugin.External;
using PlayhousePlugin.Integrations;
using PlayhousePlugin.Runtime;
using ProjectMER.Features.Objects;
using UnityEngine;

using Logger = LabApi.Features.Console.Logger;

namespace PlayhousePlugin.Cosmetics;

public sealed class CosmeticService : IDisposable
{
    private readonly PluginRuntime runtime;
    private readonly SchematicService schematics;
    private readonly DonatorRepository donators;

    private readonly Dictionary<string, PetState> pets =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, HatState> hats =
        new(StringComparer.OrdinalIgnoreCase);

    public CosmeticService(
        PluginRuntime runtime,
        SchematicService schematics,
        DonatorRepository donators)
    {
        this.runtime = runtime;
        this.schematics = schematics;
        this.donators = donators;
    }

    public bool EquipPet(
        Player player,
        string code,
        out string response)
    {
        if (player is null)
        {
            response = "Player was unavailable.";
            return false;
        }

        code = code?.Trim() ?? string.Empty;

        if (!PetItems.TryGetValue(code, out ItemType type))
        {
            response = "Unknown pet code. Use .pets to list pets.";
            return false;
        }

        if (!donators.TryGet(player.UserId, out Donator donor))
        {
            response = "This is a donor-restricted command.";
            return false;
        }

        int requiredTier;

        if (code.StartsWith(
                "d",
                StringComparison.OrdinalIgnoreCase))
        {
            requiredTier = 0;
        }
        else if (
            code.Length < 1 ||
            !int.TryParse(
                code.Substring(0, 1),
                out requiredTier))
        {
            response = "Invalid pet code.";
            return false;
        }

        if (
            type == ItemType.MicroHID &&
            !donor.IsBooster)
        {
            response =
                "The Micro H.I.D. pet requires Discord booster status.";

            return false;
        }

        if (
            type != ItemType.MicroHID &&
            donor.Tier < requiredTier)
        {
            response =
                $"This pet requires donor tier {requiredTier}.";

            return false;
        }

        try
        {
            RemovePet(player.UserId);

            Vector3 spawnPosition =
                player.Position
                - player.Camera.forward * 1.2f
                + player.Camera.right * 0.7f
                + Vector3.up * 0.7f;

            Logger.Info(
                $"Creating pet. " +
                $"Player={player.UserId}, " +
                $"Code={code}, " +
                $"Item={type}, " +
                $"Position={spawnPosition}"
            );

            Pickup? pickup =
                Pickup.Create(type, spawnPosition);

            if (pickup is null)
            {
                response =
                    $"Pickup.Create returned null for {type}.";

                Logger.Error(response);
                return false;
            }

            pickup.Spawn();

            Logger.Info(
                $"Pickup spawned. " +
                $"Item={type}, " +
                $"Destroyed={pickup.IsDestroyed}, " +
                $"Position={pickup.Position}"
            );

            if (pickup.IsDestroyed)
            {
                response =
                    "The pickup was destroyed immediately after spawning.";

                Logger.Error(response);
                return false;
            }

            pickup.IsLocked = true;

            if (pickup.Rigidbody is not null)
            {
                pickup.Rigidbody.useGravity = false;
                pickup.Rigidbody.isKinematic = true;
                pickup.Rigidbody.linearVelocity = Vector3.zero;
                pickup.Rigidbody.angularVelocity = Vector3.zero;
            }
            else
            {
                Logger.Warn(
                    $"Pet pickup {type} spawned without a Rigidbody."
                );
            }

            if (type == ItemType.MicroHID)
            {
                pickup.GameObject.transform.localScale =
                    Vector3.one * 0.5f;
            }

            var state = new PetState(pickup);

            pets[player.UserId] = state;

            /*
             * Place the pickup in its correct follow position once
             * before starting the repeating task.
             */
            UpdatePetPosition(player, state);

            state.Handle = runtime.Repeat(
                0.05f,
                () => FollowPet(player, state)
            );

            donators.UpdatePreference(
                player.UserId,
                code
            );

            response = $"Pet equipped: {type}.";
            return true;
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Unable to equip pet. " +
                $"Player={player.UserId}, " +
                $"Code={code}, " +
                $"Exception={exception}"
            );

            response =
                $"Unable to spawn pet: " +
                $"{exception.GetType().Name}: " +
                $"{exception.Message}";

            return false;
        }
    }

    public bool EquipHat(
        Player player,
        string name,
        out string response)
    {
        if (player is null)
        {
            response = "Player was unavailable.";
            return false;
        }

        name = name?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name))
        {
            response = "Enter a hat schematic name.";
            return false;
        }

        try
        {
            RemoveHat(player.UserId);

            Vector3 spawnPosition =
                player.Camera.position
                + Vector3.up * 0.35f;

            Quaternion spawnRotation =
                Quaternion.Euler(
                    0f,
                    player.Camera.rotation.eulerAngles.y,
                    0f
                );

            SchematicObject? schematic =
                schematics.Spawn(
                    name,
                    spawnPosition,
                    spawnRotation,
                    Vector3.one
                );

            if (schematic is null)
            {
                response =
                    $"Hat schematic '{name}' is unavailable.";

                return false;
            }

            var state =
                new HatState(schematic);

            hats[player.UserId] = state;

            state.Handle = runtime.Repeat(
                0.05f,
                () => FollowHat(player, state)
            );

            response = "Hat equipped.";
            return true;
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Unable to equip hat. " +
                $"Player={player.UserId}, " +
                $"Hat={name}, " +
                $"Exception={exception}"
            );

            response =
                $"Unable to equip hat: " +
                $"{exception.GetType().Name}: " +
                $"{exception.Message}";

            return false;
        }
    }

    public void OnSpawned(Player player)
    {
        if (player is null)
            return;

        if (!donators.TryGet(
                player.UserId,
                out Donator donor))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(
                donor.Preference))
        {
            return;
        }

        if (donor.Preference == "0")
            return;

        bool success = EquipPet(
            player,
            donor.Preference,
            out string result
        );

        Logger.Info(
            $"Restored saved pet. " +
            $"Player={player.UserId}, " +
            $"Preference={donor.Preference}, " +
            $"Success={success}, " +
            $"Result={result}"
        );
    }

    private void FollowPet(
        Player player,
        PetState state)
    {
        if (player is null || !player.IsAlive)
        {
            if (player is not null)
                RemovePet(player.UserId);

            return;
        }

        if (
            state.Pickup is null ||
            state.Pickup.IsDestroyed)
        {
            pets.Remove(player.UserId);
            state.Handle?.Dispose();
            return;
        }

        try
        {
            UpdatePetPosition(player, state);
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Pet follow failed. " +
                $"Player={player.UserId}, " +
                $"Exception={exception}"
            );

            RemovePet(player.UserId);
        }
    }

    private static void UpdatePetPosition(
        Player player,
        PetState state)
    {
        state.Angle += 0.08f;

        /*
         * Base location:
         * - 1.2 metres behind the player
         * - 0.7 metres to the player's right
         * - 0.7 metres above the player's feet
         */
        Vector3 followPosition =
            player.Position
            - player.Camera.forward * 1.2f
            + player.Camera.right * 0.7f
            + Vector3.up * 0.7f;

        /*
         * Small floating-circle animation.
         */
        Vector3 floatingOffset =
            new(
                Mathf.Cos(state.Angle) * 0.15f,
                Mathf.Sin(state.Angle) * 0.10f,
                Mathf.Sin(state.Angle) * 0.15f
            );

        state.Pickup.Position =
            followPosition + floatingOffset;

        state.Pickup.Rotation =
            Quaternion.Euler(
                0f,
                player.Camera.rotation.eulerAngles.y,
                0f
            );

        /*
         * Ensure physics stays disabled in case the game resets it.
         */
        if (state.Pickup.Rigidbody is not null)
        {
            state.Pickup.Rigidbody.useGravity = false;
            state.Pickup.Rigidbody.isKinematic = true;
            state.Pickup.Rigidbody.linearVelocity = Vector3.zero;
            state.Pickup.Rigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void FollowHat(
        Player player,
        HatState state)
    {
        if (player is null || !player.IsAlive)
        {
            if (player is not null)
                RemoveHat(player.UserId);

            return;
        }

        try
        {
            state.Schematic.Position =
                player.Camera.position
                + Vector3.up * 0.35f;

            state.Schematic.Rotation =
                Quaternion.Euler(
                    0f,
                    player.Camera.rotation.eulerAngles.y,
                    0f
                );
        }
        catch (Exception exception)
        {
            Logger.Error(
                $"Hat follow failed. " +
                $"Player={player.UserId}, " +
                $"Exception={exception}"
            );

            RemoveHat(player.UserId);
        }
    }

    public void Remove(
        Player player,
        bool clearPreference = false)
    {
        if (player is null)
            return;

        RemovePet(player.UserId);
        RemoveHat(player.UserId);

        if (clearPreference)
        {
            donators.UpdatePreference(
                player.UserId,
                "0"
            );
        }
    }

    public void RemovePet(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!pets.TryGetValue(
                id,
                out PetState state))
        {
            return;
        }

        pets.Remove(id);

        state.Handle?.Dispose();

        if (
            state.Pickup is not null &&
            !state.Pickup.IsDestroyed)
        {
            state.Pickup.Destroy();
        }
    }

    public void RemoveHat(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        if (!hats.TryGetValue(
                id,
                out HatState state))
        {
            return;
        }

        hats.Remove(id);

        state.Handle?.Dispose();

        schematics.Destroy(
            state.Schematic
        );
    }

    public void Clear()
    {
        foreach (
            string id in
            new List<string>(pets.Keys))
        {
            RemovePet(id);
        }

        foreach (
            string id in
            new List<string>(hats.Keys))
        {
            RemoveHat(id);
        }
    }

    public void Dispose()
    {
        Clear();
    }

    public static string PetList =>
        "Pets: " +
        "d1 MicroHID (booster); " +
        "11 MTF card; " +
        "12 Coin; " +
        "13 Painkillers; " +
        "14 SCP-018; " +
        "21 Commander card; " +
        "22 Flash; " +
        "23 Medkit; " +
        "24 SCP-207; " +
        "31 Containment card; " +
        "32 Adrenaline; " +
        "33 Radio; " +
        "34 SCP-268; " +
        "41 Facility Manager card; " +
        "51 O5 card.";

    private sealed class PetState
    {
        public PetState(Pickup pickup)
        {
            Pickup = pickup;
        }

        public Pickup Pickup { get; }

        public ScheduledHandle? Handle { get; set; }

        public float Angle { get; set; }
    }

    private sealed class HatState
    {
        public HatState(
            SchematicObject schematic)
        {
            Schematic = schematic;
        }

        public SchematicObject Schematic { get; }

        public ScheduledHandle? Handle { get; set; }
    }

    private static readonly
        Dictionary<string, ItemType> PetItems =
            new(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "d1",
                    ItemType.MicroHID
                },
                {
                    "11",
                    ItemType.KeycardMTFOperative
                },
                {
                    "12",
                    ItemType.Coin
                },
                {
                    "13",
                    ItemType.Painkillers
                },
                {
                    "14",
                    ItemType.SCP018
                },
                {
                    "21",
                    ItemType.KeycardMTFCaptain
                },
                {
                    "22",
                    ItemType.GrenadeFlash
                },
                {
                    "23",
                    ItemType.Medkit
                },
                {
                    "24",
                    ItemType.SCP207
                },
                {
                    "31",
                    ItemType.KeycardContainmentEngineer
                },
                {
                    "32",
                    ItemType.Adrenaline
                },
                {
                    "33",
                    ItemType.Radio
                },
                {
                    "34",
                    ItemType.SCP268
                },
                {
                    "41",
                    ItemType.KeycardFacilityManager
                },
                {
                    "51",
                    ItemType.KeycardO5
                },
            };
}