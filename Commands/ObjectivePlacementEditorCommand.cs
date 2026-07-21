using System;
using System.Globalization;
using System.IO;
using CommandSystem;
using LabApi.Features.Permissions;
using LabApi.Features.Wrappers;
using MapGeneration;
using PlayhousePlugin.Integrations;
using ProjectMER.Features.Objects;
using UnityEngine;
using Logger = LabApi.Features.Console.Logger;

namespace PlayhousePlugin.Commands;

/// <summary>
/// In-game terminal placement editor.
///
/// Setup: call ObjectivePlacementEditor.Configure(schematics) once from the
/// ObjectivePointController constructor.
///
/// Commands:
///   objedit spawn
///   objedit set
///   objedit forward/back/left/right/up/down [amount]
///   objedit yaw/pitch/roll [degrees]
///   objedit save
///   objedit info
///   objedit delete
/// </summary>
[CommandHandler(typeof(RemoteAdminCommandHandler))]
public sealed class ObjectivePlacementEditorCommand : ICommand
{
    public string Command => "objedit";
    public string[] Aliases => new[] { "objectiveedit", "terminaledit" };
    public string Description => "Spawns and adjusts an objective terminal placement preview.";

    public bool Execute(
        ArraySegment<string> arguments,
        ICommandSender sender,
        out string response)
    {
        if (!sender.HasPermission("at.objectiveedit"))
        {
            response = "Missing permission: at.objectiveedit";
            return false;
        }

        Player? player = Player.Get(sender);

        if (player is null)
        {
            response = "This command must be run by an in-game player.";
            return false;
        }

        if (arguments.Count == 0)
        {
            response = Usage;
            return false;
        }

        string action = arguments.At(0).ToLowerInvariant();
        float amount = ParseAmount(arguments, 1, GetDefaultAmount(action));

        try
        {
            return action switch
            {
                "spawn" => ObjectivePlacementEditor.Spawn(player, out response),
                "set" => ObjectivePlacementEditor.SetToPlayer(player, out response),
                "forward" => ObjectivePlacementEditor.Move(player, Vector3.forward * amount, out response),
                "back" => ObjectivePlacementEditor.Move(player, Vector3.back * amount, out response),
                "left" => ObjectivePlacementEditor.Move(player, Vector3.left * amount, out response),
                "right" => ObjectivePlacementEditor.Move(player, Vector3.right * amount, out response),
                "up" => ObjectivePlacementEditor.Move(player, Vector3.up * amount, out response),
                "down" => ObjectivePlacementEditor.Move(player, Vector3.down * amount, out response),
                "yaw" => ObjectivePlacementEditor.Rotate(player, new Vector3(0f, amount, 0f), out response),
                "pitch" => ObjectivePlacementEditor.Rotate(player, new Vector3(amount, 0f, 0f), out response),
                "roll" => ObjectivePlacementEditor.Rotate(player, new Vector3(0f, 0f, amount), out response),
                "save" => ObjectivePlacementEditor.Save(player, out response),
                "info" => ObjectivePlacementEditor.Info(player, out response),
                "delete" or "remove" => ObjectivePlacementEditor.Delete(player, out response),
                _ => Fail(Usage, out response),
            };
        }
        catch (Exception exception)
        {
            Logger.Error($"[ObjectiveEditor] Command failed: {exception}");
            response = $"Objective editor error: {exception.Message}";
            return false;
        }
    }

    private const string Usage =
        "Usage: objedit <spawn|set|forward|back|left|right|up|down|yaw|pitch|roll|save|info|delete> [amount]";

    private static float GetDefaultAmount(string action)
    {
        return action is "yaw" or "pitch" or "roll" ? 5f : 0.1f;
    }

    private static float ParseAmount(
        ArraySegment<string> arguments,
        int index,
        float fallback)
    {
        if (arguments.Count <= index)
            return fallback;

        return float.TryParse(
            arguments.At(index),
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out float value)
                ? value
                : fallback;
    }

    private static bool Fail(string message, out string response)
    {
        response = message;
        return false;
    }
}

public static class ObjectivePlacementEditor
{
    private const string SchematicName = "Terminal";

    private static SchematicService? schematics;
    private static SchematicObject? preview;
    private static Room? previewRoom;

    /// <summary>
    /// Call this once from ObjectivePointController's constructor:
    /// ObjectivePlacementEditor.Configure(schematics);
    /// </summary>
    public static void Configure(SchematicService schematicService)
    {
        schematics = schematicService;
    }

    public static bool Spawn(Player player, out string response)
    {
        if (!Ready(out response))
            return false;

        Room? room = player.Room;

        if (room is null)
        {
            response = "You are not inside a recognized room.";
            return false;
        }

        DestroyPreview();

        Vector3 position = player.Position;
        Quaternion rotation = Quaternion.Euler(0f, player.Rotation.eulerAngles.y, 0f);

        preview = schematics!.Spawn(
            SchematicName,
            position,
            rotation,
            Vector3.one);

        if (preview is null || preview.gameObject is null)
        {
            preview = null;
            response = "Failed to spawn the Terminal schematic. Check that ProjectMER has a schematic named Terminal.";
            return false;
        }

        previewRoom = room;
        response = BuildStatus("Preview spawned");
        return true;
    }

    public static bool SetToPlayer(Player player, out string response)
    {
        if (!HasPreview(out response))
            return false;

        Room? room = player.Room;

        if (room is null)
        {
            response = "You are not inside a recognized room.";
            return false;
        }

        previewRoom = room;
        preview!.transform.SetPositionAndRotation(
            player.Position,
            Quaternion.Euler(0f, player.Rotation.eulerAngles.y, 0f));

        response = BuildStatus("Preview moved to you");
        return true;
    }

    public static bool Move(
        Player player,
        Vector3 localOffset,
        out string response)
    {
        if (!HasPreview(out response))
            return false;

        // Movement is relative to the terminal's current direction.
        preview!.transform.position += preview.transform.TransformDirection(localOffset);
        response = BuildStatus($"Moved by {FormatVector(localOffset)}");
        return true;
    }

    public static bool Rotate(
        Player player,
        Vector3 localEuler,
        out string response)
    {
        if (!HasPreview(out response))
            return false;

        preview!.transform.Rotate(localEuler, Space.Self);
        response = BuildStatus($"Rotated by {FormatVector(localEuler)} degrees");
        return true;
    }

    public static bool Info(Player player, out string response)
    {
        if (!HasPreview(out response))
            return false;

        response = BuildStatus("Current placement");
        return true;
    }

    public static bool Save(Player player, out string response)
    {
        if (!HasPreview(out response))
            return false;

        Room? room = previewRoom ?? player.Room;

        if (room is null)
        {
            response = "Could not determine the preview's room.";
            return false;
        }

        Vector3 localPosition = room.Transform.InverseTransformPoint(
            preview!.transform.position);

        Quaternion localRotation =
            Quaternion.Inverse(room.Rotation) * preview.transform.rotation;

        Vector3 localEuler = NormalizeEuler(localRotation.eulerAngles);
        string prefab = NormalizePrefabName(room.GameObject.name);

        string snippet =
            $"[\"{prefab}\"] = new[]\n" +
            "{\n" +
            "    new Placement(\n" +
            $"        new Vector3({F(localPosition.x)}f, {F(localPosition.y + 1f)}f, {F(localPosition.z)}f),\n" +
            $"        new Vector3({F(localEuler.x)}f, {F(localEuler.y)}f, {F(localEuler.z)}f))\n" +
            "},";

        // The controller subtracts Vector3.down while spawning, so the saved
        // dictionary Y value includes +1 to compensate for that existing logic.
        string directory = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "LabAPI",
            "configs",
            "PlayhousePlugin");

        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "ObjectivePlacements.txt");

        File.AppendAllText(
            path,
            $"\n// {DateTime.Now:yyyy-MM-dd HH:mm:ss} | RoomName={room.Name}\n{snippet}\n");

        Logger.Info($"[ObjectiveEditor] Saved placement:\n{snippet}");

        response =
            $"Saved placement for {prefab}.\n" +
            $"Local position: {FormatVector(localPosition)}\n" +
            $"Local rotation: {FormatVector(localEuler)}\n" +
            $"Written to: {path}";

        return true;
    }

    public static bool Delete(Player player, out string response)
    {
        if (preview is null)
        {
            response = "There is no placement preview to delete.";
            return false;
        }

        DestroyPreview();
        response = "Placement preview deleted.";
        return true;
    }

    private static bool Ready(out string response)
    {
        if (schematics is not null)
        {
            response = string.Empty;
            return true;
        }

        response =
            "ObjectivePlacementEditor has not been configured. Add " +
            "ObjectivePlacementEditor.Configure(schematics); to the " +
            "ObjectivePointController constructor.";
        return false;
    }

    private static bool HasPreview(out string response)
    {
        if (!Ready(out response))
            return false;

        if (preview is not null && preview.gameObject is not null)
        {
            response = string.Empty;
            return true;
        }

        preview = null;
        previewRoom = null;
        response = "No preview exists. Run: objedit spawn";
        return false;
    }

    private static void DestroyPreview()
    {
        if (preview is not null)
        {
            try
            {
                schematics?.Destroy(preview);
            }
            catch (Exception exception)
            {
                Logger.Warn($"[ObjectiveEditor] Failed to destroy preview: {exception.Message}");
            }
        }

        preview = null;
        previewRoom = null;
    }

    private static string BuildStatus(string heading)
    {
        if (preview is null || previewRoom is null)
            return heading;

        Vector3 localPosition = previewRoom.Transform.InverseTransformPoint(
            preview.transform.position);

        Quaternion localRotation =
            Quaternion.Inverse(previewRoom.Rotation) * preview.transform.rotation;

        return
            $"{heading}.\n" +
            $"Prefab: {NormalizePrefabName(previewRoom.GameObject.name)}\n" +
            $"World: {FormatVector(preview.transform.position)}\n" +
            $"Local: {FormatVector(localPosition)}\n" +
            $"Rotation: {FormatVector(NormalizeEuler(localRotation.eulerAngles))}";
    }

    private static string NormalizePrefabName(string objectName)
    {
        string name = objectName.Trim().ToUpperInvariant();

        while (name.EndsWith("(CLONE)", StringComparison.Ordinal))
            name = name.Substring(0, name.Length - "(CLONE)".Length).Trim();

        return name;
    }

    private static Vector3 NormalizeEuler(Vector3 euler)
    {
        return new Vector3(
            NormalizeAngle(euler.x),
            NormalizeAngle(euler.y),
            NormalizeAngle(euler.z));
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        return angle < 0f ? angle + 360f : angle;
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({F(value.x)}, {F(value.y)}, {F(value.z)})";
    }

    private static string F(float value)
    {
        if (Mathf.Abs(value) < 0.0005f)
            value = 0f;

        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }
}