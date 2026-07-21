using System.Collections.Generic;
using System;
using ProjectMER.Features;
using ProjectMER.Features.Objects;
using UnityEngine;

namespace PlayhousePlugin.Integrations;

public sealed class SchematicService
{
    private readonly List<SchematicObject> spawned = new();

    public IReadOnlyList<SchematicObject> Spawned => spawned;

    public SchematicObject? Spawn(string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // The current ProjectMER asset pack renamed legacy Box1 to Box.
        string resolvedName = name == "Box1" ? "Box" : name;
        try
        {
            SchematicObject schematic = ObjectSpawner.SpawnSchematic(resolvedName, position, rotation, scale);
            if (schematic == null)
            {
                LabApi.Features.Console.Logger.Error($"Unable to spawn ProjectMER schematic '{name}': ProjectMER returned null.");
                return null;
            }
            spawned.Add(schematic);
            return schematic;
        }
        catch (Exception exception)
        {
            LabApi.Features.Console.Logger.Error($"Unable to spawn ProjectMER schematic '{name}': {exception}");
            return null;
        }
    }

    public void Destroy(SchematicObject? schematic)
    {
        // Unity objects compare equal to null after their native object has been
        // destroyed. Pattern matching (`is null`) does not perform that check.
        if (schematic == null)
        {
            spawned.Remove(schematic!);
            return;
        }
        spawned.Remove(schematic);
        if (schematic.gameObject != null)
            UnityEngine.Object.Destroy(schematic.gameObject);
    }

    public void DestroyAll()
    {
        for (int index = spawned.Count - 1; index >= 0; index--)
            if (spawned[index] != null && spawned[index].gameObject != null)
                UnityEngine.Object.Destroy(spawned[index].gameObject);
        spawned.Clear();
    }
}
