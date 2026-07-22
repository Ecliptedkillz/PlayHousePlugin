using System;
using System.Globalization;
using System.Linq;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayhousePlugin.Integrations;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.Commands;

[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class VendingPlacementCommand : ICommand
{
    public string Command => "vendingplacement";

    public string[] Aliases =>
        new[]
        {
            "vendplace",
            "vmp"
        };

    public string Description =>
        "Creates and adjusts vending-machine placement entries.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission(
                "at.vendingplacement"))
        {
            response =
                "Missing permission: at.vendingplacement";

            return false;
        }

        Player? player =
            Player.Get(sender);

        if (player is null)
        {
            response =
                "This command must be run by an in-game player.";

            return false;
        }

        if (arguments.Count == 0)
        {
            response = Usage;
            return false;
        }

        string action =
            GetArgument(arguments, 0)
                .ToLowerInvariant();

        switch (action)
        {
            case "spawn":
            case "start":
            case "create":
                return VendingPlacementEditor.Begin(
                    player,
                    out response);

            case "move":
                if (arguments.Count < 3)
                {
                    response =
                        "Usage: vendingplacement move <forward|right|up> <amount>";

                    return false;
                }

                if (!TryFloat(
                        GetArgument(arguments, 2),
                        out float moveAmount))
                {
                    response =
                        "The movement amount must be a number.";

                    return false;
                }

                return VendingPlacementEditor.Move(
                    player,
                    GetArgument(arguments, 1),
                    moveAmount,
                    out response);

            case "forward":
            case "back":
            case "backward":
            case "right":
            case "left":
            case "up":
            case "down":
                if (arguments.Count < 2 ||
                    !TryFloat(
                        GetArgument(arguments, 1),
                        out float directMoveAmount))
                {
                    response =
                        $"Usage: vendingplacement {action} <amount>";

                    return false;
                }

                return VendingPlacementEditor.Move(
                    player,
                    action,
                    directMoveAmount,
                    out response);

            case "rotate":
            case "yaw":
                if (arguments.Count < 2 ||
                    !TryFloat(
                        GetArgument(arguments, 1),
                        out float degrees))
                {
                    response =
                        "Usage: vendingplacement rotate <degrees>";

                    return false;
                }

                return VendingPlacementEditor.Rotate(
                    player,
                    degrees,
                    out response);

            case "tp":
            case "teleport":
                return VendingPlacementEditor.TeleportToPlayer(
                    player,
                    out response);

            case "info":
            case "show":
                return VendingPlacementEditor.GetInfo(
                    player,
                    out response);

            case "save":
                return VendingPlacementEditor.Save(
                    player,
                    out response);

            case "cancel":
            case "delete":
            case "destroy":
            case "stop":
                return VendingPlacementEditor.Cancel(
                    player,
                    out response);

            default:
                response = Usage;
                return false;
        }
    }

    private static bool TryFloat(
        string value,
        out float result)
    {
        return float.TryParse(
                   value,
                   NumberStyles.Float,
                   CultureInfo.InvariantCulture,
                   out result) ||
               float.TryParse(
                   value,
                   out result);
    }

    private static string GetArgument(
        ArraySegment<string> arguments,
        int index)
    {
        return arguments.Array![
            arguments.Offset + index];
    }

    private const string Usage =
        "Vending placement commands:\n" +
        "vendingplacement spawn\n" +
        "vendingplacement move <forward|right|up> <amount>\n" +
        "vendingplacement forward/back/right/left/up/down <amount>\n" +
        "vendingplacement rotate <degrees>\n" +
        "vendingplacement tp\n" +
        "vendingplacement info\n" +
        "vendingplacement save\n" +
        "vendingplacement cancel";
}

public static class VendingPlacementEditor
{
    private const string SchematicName =
        "Vending_Machine";

    private static SchematicService? schematics;
    private static SchematicObject? preview;
    private static Room? selectedRoom;
    private static string? ownerUserId;

    public static void Configure(
        SchematicService service)
    {
        schematics = service;
    }

    public static void Unconfigure(
        SchematicService service)
    {
        if (!ReferenceEquals(
                schematics,
                service))
        {
            return;
        }

        DestroyPreview();
        schematics = null;
    }

    public static bool Begin(
        Player player,
        out string response)
    {
        if (schematics is null)
        {
            response =
                "The schematic service is not available.";

            return false;
        }

        DestroyPreview();

        Room? room =
            FindNearestRoom(player.Position);

        if (room is null)
        {
            response =
                "Could not find a room near your position.";

            return false;
        }

        Vector3 worldPosition =
            player.Position +
            player.Rotation *
            Vector3.forward *
            1.5f;

        Quaternion worldRotation =
            Quaternion.Euler(
                0f,
                player.Rotation.eulerAngles.y + 180f,
                0f);

        SchematicObject? spawned =
            schematics.Spawn(
                SchematicName,
                worldPosition,
                worldRotation,
                Vector3.one);

        if (spawned is null ||
            spawned.gameObject is null)
        {
            response =
                $"Could not spawn schematic '{SchematicName}'.";

            return false;
        }

        preview = spawned;
        selectedRoom = room;
        ownerUserId = player.UserId;

        response =
            "Vending-machine preview spawned.\n" +
            BuildInfo();

        return true;
    }

    public static bool Move(
        Player player,
        string direction,
        float amount,
        out string response)
    {
        if (!CanEdit(
                player,
                out response))
        {
            return false;
        }

        Transform transform =
            preview!.gameObject.transform;

        string normalized =
            direction
                .Trim()
                .ToLowerInvariant();

        Vector3 delta;

        switch (normalized)
        {
            case "forward":
            case "f":
                delta =
                    transform.forward * amount;
                break;

            case "back":
            case "backward":
            case "b":
                delta =
                    -transform.forward * amount;
                break;

            case "right":
            case "r":
                delta =
                    transform.right * amount;
                break;

            case "left":
            case "l":
                delta =
                    -transform.right * amount;
                break;

            case "up":
            case "u":
                delta =
                    Vector3.up * amount;
                break;

            case "down":
            case "d":
                delta =
                    Vector3.down * amount;
                break;

            default:
                response =
                    "Direction must be forward, back, right, left, up, or down.";

                return false;
        }

        transform.position += delta;

        Room? nearest =
            FindNearestRoom(transform.position);

        if (nearest is not null)
            selectedRoom = nearest;

        response = BuildInfo();
        return true;
    }

    public static bool Rotate(
        Player player,
        float degrees,
        out string response)
    {
        if (!CanEdit(
                player,
                out response))
        {
            return false;
        }

        Transform transform =
            preview!.gameObject.transform;

        transform.rotation =
            Quaternion.AngleAxis(
                degrees,
                Vector3.up) *
            transform.rotation;

        response = BuildInfo();
        return true;
    }

    public static bool TeleportToPlayer(
        Player player,
        out string response)
    {
        if (!CanEdit(
                player,
                out response))
        {
            return false;
        }

        Transform transform =
            preview!.gameObject.transform;

        transform.position =
            player.Position +
            player.Rotation *
            Vector3.forward *
            1.5f;

        transform.rotation =
            Quaternion.Euler(
                0f,
                player.Rotation.eulerAngles.y + 180f,
                0f);

        Room? room =
            FindNearestRoom(transform.position);

        if (room is not null)
            selectedRoom = room;

        response = BuildInfo();
        return true;
    }

    public static bool GetInfo(
        Player player,
        out string response)
    {
        if (!CanEdit(
                player,
                out response))
        {
            return false;
        }

        response = BuildInfo();
        return true;
    }

    public static bool Save(
        Player player,
        out string response)
    {
        if (!CanEdit(
                player,
                out response))
        {
            return false;
        }

        Room room =
            selectedRoom!;

        Transform transform =
            preview!.gameObject.transform;

        Vector3 localPosition =
            room.Transform.InverseTransformPoint(
                transform.position);

        Quaternion localRotation =
            Quaternion.Inverse(
                room.Transform.rotation) *
            transform.rotation;

        Vector3 localEuler =
            NormalizeEuler(
                localRotation.eulerAngles);

        string placement =
            BuildPlacementCode(
                room,
                localPosition,
                localEuler);

        response =
            "Placement copied below. Paste it into the matching vending placement dictionary:\n\n" +
            placement;

        return true;
    }

    public static bool Cancel(
        Player player,
        out string response)
    {
        if (preview is null)
        {
            response =
                "There is no vending-machine preview.";

            return false;
        }

        if (!string.Equals(
                ownerUserId,
                player.UserId,
                StringComparison.Ordinal))
        {
            response =
                "Another player owns the active placement preview.";

            return false;
        }

        DestroyPreview();

        response =
            "Vending-machine placement preview removed.";

        return true;
    }

    private static bool CanEdit(
        Player player,
        out string response)
    {
        if (preview is null ||
            preview.gameObject is null ||
            selectedRoom is null)
        {
            response =
                "No vending preview exists. Run vendingplacement spawn first.";

            return false;
        }

        if (!string.Equals(
                ownerUserId,
                player.UserId,
                StringComparison.Ordinal))
        {
            response =
                "Another player owns the active placement preview.";

            return false;
        }

        response = string.Empty;
        return true;
    }

    private static Room? FindNearestRoom(
        Vector3 position)
    {
        return Room.List
            .Where(room =>
                room is not null &&
                room.GameObject is not null)
            .OrderBy(room =>
                Vector3.SqrMagnitude(
                    room.Transform.position -
                    position))
            .FirstOrDefault();
    }

    private static string BuildInfo()
    {
        Room room =
            selectedRoom!;

        Transform transform =
            preview!.gameObject.transform;

        Vector3 localPosition =
            room.Transform.InverseTransformPoint(
                transform.position);

        Quaternion localRotation =
            Quaternion.Inverse(
                room.Transform.rotation) *
            transform.rotation;

        Vector3 localEuler =
            NormalizeEuler(
                localRotation.eulerAngles);

        return
            $"RoomName: {room.Name}\n" +
            $"Prefab: {NormalizeCheckpointName(room.GameObject.name)}\n" +
            $"Local position: {Format(localPosition)}\n" +
            $"Local rotation: {Format(localEuler)}";
    }

    private static string BuildPlacementCode(
        Room room,
        Vector3 position,
        Vector3 rotation)
    {
        string placement =
            "new Placement(\n" +
            $"    new Vector3({Float(position.x)}, {Float(position.y)}, {Float(position.z)}),\n" +
            $"    new Vector3({Float(rotation.x)}, {Float(rotation.y)}, {Float(rotation.z)}))";

        if (room.Name != RoomName.Unnamed)
        {
            return
                $"[RoomName.{room.Name}] = new[]\n" +
                "{\n" +
                $"    {placement.Replace("\n", "\n    ")}\n" +
                "},";
        }

        string prefab =
            NormalizeCheckpointName(
                room.GameObject.name);

        return
            $"[\"{prefab}\"] = new[]\n" +
            "{\n" +
            $"    {placement.Replace("\n", "\n    ")}\n" +
            "},";
    }

    private static Vector3 NormalizeEuler(
        Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(
        float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return Math.Abs(angle) < 0.0005f
            ? 0f
            : angle;
    }

    private static string Format(
        Vector3 value)
    {
        return
            $"({value.x:0.###}, " +
            $"{value.y:0.###}, " +
            $"{value.z:0.###})";
    }

    private static string Float(
        float value)
    {
        if (Math.Abs(value) < 0.0005f)
            value = 0f;

        return
            value.ToString(
                "0.###",
                CultureInfo.InvariantCulture) +
            "f";
    }

    private static string NormalizeCheckpointName(
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

    private static void DestroyPreview()
    {
        if (preview is not null &&
            schematics is not null)
        {
            schematics.Destroy(preview);
        }

        preview = null;
        selectedRoom = null;
        ownerUserId = null;
    }
}