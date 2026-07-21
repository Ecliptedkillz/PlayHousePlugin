using System;
using System.Collections.Generic;
using System.Linq;
using AdminToys;
using Logger = LabApi.Features.Console.Logger;
using LabApi.Features.Wrappers;
using PlayerRoles;
using PlayhousePlugin.Controllers;
using PlayhousePlugin.Integrations;
using ProjectMER.Features.Objects;
using UnityEngine;
using Object = UnityEngine.Object;
using LabLight = LabApi.Features.Wrappers.LightSourceToy;
using LabPrimitive = LabApi.Features.Wrappers.PrimitiveObjectToy;

namespace PlayhousePlugin.Components;

public sealed class ObjectivePointComponent : MonoBehaviour, IDisposable
{
    private const float Radius = 2.5f;
    private const float CaptureRequirement = 200f;
    private const float CaptureRate = 1.5f;
    private const float DecayRate = 0.5f;

    // Larger interaction target positioned in front of the terminal.
    private static readonly Vector3 ButtonLocalPosition =
        new(-0.116f, 0.237f, 0.34f);

    private static readonly Vector3 ButtonScale =
        new(0.75f, 0.75f, 0.75f);

    private ObjectivePointController? controller;
    private SchematicService? schematics;
    private SchematicObject? terminal;
    private Pickup? activateButton;
    private LabPrimitive? display;
    private LabLight? light;

    private Vector3 objectivePosition;
    private Quaternion terminalRotation;
    private Vector3 buttonWorldPosition;
    private Quaternion buttonWorldRotation;
    private bool lightOn;
    private bool disposed;

    public ObjectiveState State { get; private set; } =
        ObjectiveState.Disabled;

    public float CaptureAmount { get; private set; }

    public bool AllowAllToCapture { get; set; }

    public RoleTypeId RoleToNotify { get; set; } =
        RoleTypeId.None;

    public bool HasReportedCapture { get; set; }

    public bool IsDestroyed =>
        disposed ||
        this == null ||
        gameObject == null;

    public ushort ButtonSerial =>
        activateButton?.Serial ?? 0;

    public bool Initialize(
        ObjectivePointController owner,
        SchematicService schematicService,
        Vector3 position,
        Quaternion rotation)
    {
        controller = owner;
        schematics = schematicService;
        objectivePosition = position;
        terminalRotation = rotation;

        terminal = schematics.Spawn(
            "Terminal",
            position,
            rotation,
            Vector3.one);

        if (terminal is null)
        {
            Logger.Error(
                "[Objectives] Failed to spawn the Terminal schematic.");
            return false;
        }

        FindScreenPrimitive();
        CreateButton(rotation);
        CreateLight(rotation);

        if (activateButton is null)
        {
            Dispose();
            return false;
        }

        SetState(ObjectiveState.Disabled);
        return true;
    }

    public void TryActivate(Player player)
    {
        Logger.Info(
            $"[Objectives] TryActivate: Player={player.Nickname}, Role={player.Role}, " +
            $"Team={player.Team}, Alive={player.IsAlive}, State={State}, " +
            $"Generators={Generator.List.Count(generator => generator.Engaged)}.");

        if (disposed)
        {
            Logger.Warn("[Objectives] Activation rejected: objective has been disposed.");
            return;
        }

        if (!player.IsAlive)
        {
            player.SendHint("<color=red>You must be alive to activate this terminal.</color>", 3f);
            UnlockButton();
            return;
        }

        if (State == ObjectiveState.Enabled)
        {
            player.SendHint(
                $"<color=green>{controller?.ObjectivesCaptured ?? 0} out of 6 terminals enabled.</color>",
                3f);

            UnlockButton();
            return;
        }

        if (!Generator.List.Any(generator => generator.Engaged))
        {
            Logger.Info("[Objectives] Activation rejected: no SCP-079 generator is engaged.");
            player.SendHint(
                "<color=red>1 SCP-079 generator is required to power this objective point!</color>",
                4f);

            UnlockButton();
            return;
        }

        bool canActivate =
            AllowAllToCapture
                ? player.IsHuman
                : IsMtf(player);

        if (!canActivate)
        {
            Logger.Info(
                $"[Objectives] Activation rejected: role {player.Role} is not permitted.");

            player.SendHint(
                "<color=red>You cannot activate these objectives!</color>",
                3f);

            UnlockButton();
            return;
        }

        if (State == ObjectiveState.Disabled)
        {
            LockButton();
            SetState(ObjectiveState.Activating);

            Logger.Info(
                $"[Objectives] Terminal activation started by {player.Nickname} ({player.Role}).");

            player.SendHint(
                "<color=yellow>Terminal activated. Stay near it to capture.</color>",
                3f);
        }
    }

    public void Tick(bool failedObjectives)
    {
        if (disposed)
            return;

        EnsureButton();
        BlinkLight();

        if (failedObjectives &&
            State == ObjectiveState.Activating &&
            !AllowAllToCapture)
        {
            CaptureAmount = 0f;
            LockButton();
            SetState(ObjectiveState.Disabled);
            return;
        }

        if (State == ObjectiveState.Enabled ||
            State == ObjectiveState.Disabled)
        {
            return;
        }

        var nearby = new List<Player>();
        int capturing = 0;
        int decaying = 0;

        foreach (Player player in Player.ReadyList)
        {
            if (!player.IsAlive ||
                Vector3.Distance(
                    player.Position,
                    objectivePosition) > Radius)
            {
                continue;
            }

            nearby.Add(player);

            if (AllowAllToCapture)
            {
                if (player.IsHuman)
                    capturing++;
                else
                    decaying++;

                continue;
            }

            if (IsMtf(player))
            {
                capturing++;
                continue;
            }

            if (IsNeutralForObjective(player))
                continue;

            decaying++;
        }

        if (capturing > 0 && decaying == 0)
        {
            SetState(ObjectiveState.Activating);

            CaptureAmount +=
                ((float)Math.Log(capturing * capturing) + 1f) *
                CaptureRate;

            SendProgressHints(nearby);

            if (RoleToNotify != RoleTypeId.None)
            {
                foreach (Player player in Player.ReadyList)
                {
                    if (player.Role == RoleToNotify)
                    {
                        player.SendHint(
                            "<color=red>! Your objective point is being captured !</color>",
                            0.4f);
                    }
                }
            }
        }
        else if (capturing > 0 && decaying > 0)
        {
            SetState(ObjectiveState.Contested);

            foreach (Player player in nearby)
            {
                player.SendHint(
                    "<color=grey>Contested Objective Point</color>",
                    0.4f);
            }
        }
        else
        {
            CaptureAmount = Mathf.Max(
                0f,
                CaptureAmount - DecayRate);

            SetState(ObjectiveState.Decaying);

            foreach (Player player in nearby)
            {
                player.SendHint(
                    $"Reversing capture {CaptureAmount / CaptureRequirement * 100f:0}%",
                    0.4f);
            }
        }

        if (CaptureAmount >= CaptureRequirement)
        {
            CaptureAmount = CaptureRequirement;
            SetState(ObjectiveState.Enabled);

            if (AllowAllToCapture)
                LockButton();
            else
                UnlockButton();

            controller?.ObjectiveCaptured(this);
            return;
        }

        if (CaptureAmount <= 0f)
        {
            CaptureAmount = 0f;
            UnlockButton();
            SetState(ObjectiveState.Disabled);
        }
    }

    private void FindScreenPrimitive()
    {
        if (terminal is null)
            return;

        foreach (GameObject block in terminal.AttachedBlocks)
        {
            if (block is null ||
                block.name.IndexOf(
                    "Screen",
                    StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            if (block.TryGetComponent(
                    out AdminToys.PrimitiveObjectToy primitiveBase))
            {
                display = LabPrimitive.Get(primitiveBase);
                break;
            }
        }

        if (display is null)
        {
            Logger.Warn(
                "[Objectives] Terminal schematic has no primitive block containing 'Screen'.");
        }
    }

    private void CreateButton(Quaternion rotation)
    {
        if (terminal is null || disposed)
            return;

        ushort oldSerial = ButtonSerial;

        buttonWorldPosition =
            terminal.transform.TransformPoint(ButtonLocalPosition);

        buttonWorldRotation =
            rotation * Quaternion.Euler(0f, -90f, 90f);

        activateButton = Pickup.Create(
            ItemType.Coin,
            buttonWorldPosition,
            buttonWorldRotation,
            ButtonScale);

        if (activateButton is null)
        {
            Logger.Error("[Objectives] Failed to create terminal interaction pickup.");
            return;
        }

        activateButton.Spawn();
        ConfigureButtonPhysics();

        controller?.RegisterButton(this, oldSerial, activateButton.Serial);

        Logger.Info(
            $"[Objectives] Interaction pickup spawned. Type={activateButton.Type}, " +
            $"Serial={activateButton.Serial}, Position={activateButton.Position}, " +
            $"Scale={ButtonScale}.");
    }

    private void EnsureButton()
    {
        if (disposed || terminal is null)
            return;

        if (activateButton is null || activateButton.IsDestroyed)
        {
            Logger.Warn(
                "[Objectives] Terminal interaction pickup was removed; respawning it.");

            activateButton = null;
            CreateButton(terminalRotation);
            return;
        }

        if (Vector3.Distance(activateButton.Position, buttonWorldPosition) > 0.05f)
            activateButton.Position = buttonWorldPosition;

        activateButton.Rotation = buttonWorldRotation;
        activateButton.GameObject.transform.localScale = ButtonScale;
        activateButton.IsInUse = false;

        ConfigureButtonPhysics();
    }

    private void ConfigureButtonPhysics()
    {
        if (activateButton is null || activateButton.IsDestroyed)
            return;

        activateButton.Position = buttonWorldPosition;
        activateButton.Rotation = buttonWorldRotation;
        activateButton.GameObject.transform.localScale = ButtonScale;
        activateButton.IsInUse = false;

        Rigidbody? rigidbody = activateButton.Rigidbody;

        if (rigidbody is not null)
        {
            rigidbody.linearVelocity = Vector3.zero;
            rigidbody.angularVelocity = Vector3.zero;
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = true;
            rigidbody.isKinematic = true;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }

        Collider[] colliders =
            activateButton.GameObject.GetComponentsInChildren<Collider>(true);

        foreach (Collider collider in colliders)
        {
            collider.enabled = true;

            if (collider is BoxCollider box)
                box.size = Vector3.Max(box.size, new Vector3(0.8f, 0.8f, 0.8f));
            else if (collider is SphereCollider sphere)
                sphere.radius = Mathf.Max(sphere.radius, 0.45f);
            else if (collider is CapsuleCollider capsule)
                capsule.radius = Mathf.Max(capsule.radius, 0.45f);
        }
    }

    private void CreateLight(Quaternion terminalRotation)
    {
        Vector3 lightPosition =
            objectivePosition +
            terminalRotation * Vector3.forward * 0.45f +
            Vector3.up * 0.33f;

        light = LabLight.Create(
            lightPosition,
            terminalRotation,
            Vector3.one);

        light.Range = 3f;
        light.Intensity = 1f;
        light.Color = Color.red;
    }

    private void BlinkLight()
    {
        if (light is null)
            return;

        lightOn = !lightOn;
        light.Color = lightOn
            ? GetLightColor(State)
            : Color.black;
    }

    private void SetState(ObjectiveState state)
    {
        if (State == state)
            return;

        State = state;

        if (display is not null)
            display.Color = GetDisplayColor(state);
    }

    private void SendProgressHints(IEnumerable<Player> players)
    {
        float percent =
            CaptureAmount / CaptureRequirement * 100f;

        foreach (Player player in players)
        {
            player.SendHint(
                $"Captured {percent:0}%",
                0.4f);
        }
    }

    private void LockButton()
    {
        if (activateButton is null || activateButton.IsDestroyed)
            return;

        activateButton.IsLocked = true;
    }

    private void UnlockButton()
    {
        if (activateButton is null || activateButton.IsDestroyed)
            return;

        activateButton.IsLocked = false;
    }

    private static bool IsMtf(Player player)
    {
        return player.Role is
            RoleTypeId.NtfCaptain or
            RoleTypeId.NtfSergeant or
            RoleTypeId.NtfSpecialist or
            RoleTypeId.NtfPrivate or
            RoleTypeId.FacilityGuard;
    }

    private static bool IsNeutralForObjective(Player player)
    {
        return player.Team == Team.Scientists ||
               player.Team == Team.Dead ||
               player.Role == RoleTypeId.Tutorial ||
               player.Team == Team.ClassD;
    }

    private static Color GetDisplayColor(ObjectiveState state)
    {
        return state switch
        {
            ObjectiveState.Disabled => Color.red,
            ObjectiveState.Activating => Color.yellow,
            ObjectiveState.Contested => Color.gray,
            ObjectiveState.Decaying =>
                new Color(191f / 255f, 181f / 255f, 203f / 255f),
            ObjectiveState.Enabled => Color.green,
            _ => Color.black
        };
    }

    private static Color GetLightColor(ObjectiveState state)
    {
        return state switch
        {
            ObjectiveState.Disabled => Color.red,
            ObjectiveState.Activating => Color.yellow,
            ObjectiveState.Contested => Color.black,
            ObjectiveState.Decaying => Color.red,
            ObjectiveState.Enabled => Color.green,
            _ => Color.black
        };
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        try
        {
            if (activateButton is not null &&
                !activateButton.IsDestroyed)
            {
                activateButton.Destroy();
            }
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"[Objectives] Failed to destroy terminal button: {exception.Message}");
        }

        try
        {
            light?.Destroy();
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"[Objectives] Failed to destroy terminal light: {exception.Message}");
        }

        try
        {
            if (terminal is not null)
                schematics?.Destroy(terminal);
        }
        catch (Exception exception)
        {
            Logger.Warn(
                $"[Objectives] Failed to destroy terminal schematic: {exception.Message}");
        }

        activateButton = null;
        light = null;
        display = null;
        terminal = null;
        controller = null;
        schematics = null;

        if (gameObject is not null)
            Object.Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (!disposed)
            Dispose();
    }
}

public enum ObjectiveState
{
    Disabled,
    Activating,
    Contested,
    Decaying,
    Enabled
}