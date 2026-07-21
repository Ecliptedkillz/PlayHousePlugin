using System.Collections.Generic;
using PlayhousePlugin.Integrations;
using ProjectMER.Features.Objects;
using PlayhousePlugin.Components;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.Controllers;

public sealed class SurfaceReworkController
{
    private readonly SchematicService schematics;
    private readonly List<SchematicObject> spawned = new();
    private readonly List<LightSourceToy> lights = new();
    public SurfaceReworkController(SchematicService schematics) => this.schematics = schematics;

    public void Spawn()
    {
        Destroy();
        Add("ElevatorAnimated", new Vector3(-13f, 987.2f, -65.1f), Quaternion.Euler(0, -90, 0), Vector3.one);
        Add("Stairs", new Vector3(-10.8f, 987.5f, -47.5f), Quaternion.identity, Vector3.one);
        Add("Stairs2", new Vector3(46.4f, 987.2f, -57.15f), Quaternion.identity, Vector3.one);
        Add("GateB", new Vector3(146.7f, 992.4f, -59.2f), Quaternion.identity, Vector3.one);
        SchematicObject? truck = Add("Truck_Crash", new Vector3(146.7f, 992.4f, -59.2f), Quaternion.identity, Vector3.one);
        if (truck != null)
        {
            foreach (GameObject block in truck.AttachedBlocks)
            {
                if (!block.name.Contains("Light")) continue;
                global::PlayhousePlugin.Components.LightBlink blink = block.AddComponent<global::PlayhousePlugin.Components.LightBlink>();
                blink.MaximumIntensityDecrease = 0.7f;
                blink.MaxFlickerTime = 0.3f;
                blink.Offset = 0.1f;
            }
        }
        Add("FootBridge", new Vector3(164.5f, 992.4f, -59.2f), Quaternion.identity, Vector3.one);
        AddLight(new Vector3(115f, 991.4f, -65.4f), 15f, new Color(254 / 255f, 1f, 232 / 255f));
        AddLight(new Vector3(100f, 991.4f, -65.4f), 15f, new Color(254 / 255f, 1f, 232 / 255f));
        AddLight(new Vector3(85f, 991.4f, -65.4f), 15f, new Color(254 / 255f, 1f, 232 / 255f));
        AddLight(new Vector3(87.4f, 996.4f, -47.8f), 10f, new Color(204 / 255f, 205 / 255f, 182 / 255f));
        AddLight(new Vector3(76.2f, 994.3f, -48.6f), 20f, new Color(254 / 255f, 1f, 232 / 255f));
        foreach (SurfaceBox box in Boxes) Add(box.Name, box.Position, box.Rotation, box.Scale);
    }

    public void Destroy()
    {
        foreach (SchematicObject schematic in spawned) schematics.Destroy(schematic);
        spawned.Clear();
        foreach (LightSourceToy light in lights) light.Destroy();
        lights.Clear();
    }

    private SchematicObject? Add(string name, Vector3 position, Quaternion rotation, Vector3 scale)
    {
        SchematicObject? schematic = schematics.Spawn(name, position, rotation, scale);
        if (schematic != null) spawned.Add(schematic);
        return schematic;
    }

    private void AddLight(Vector3 position, float range, Color color)
    {
        LightSourceToy light = LightSourceToy.Create(position, Quaternion.identity, Vector3.one, null, false);
        light.Range = range;
        light.Color = color;
        light.Spawn();
        lights.Add(light);
    }

    private static readonly SurfaceBox[] Boxes = {
        new("Box3", new Vector3(52.11f,1001.97f,-50.375f), Quaternion.identity, Vector3.one),
        new("Box1", new Vector3(53.735f,1001.97f,-49.735f), Quaternion.identity, Vector3.one),
        new("Box2", new Vector3(54.121f,1002.37f,-67.909f), Quaternion.identity, Vector3.one * 1.5f),
        new("Box2", new Vector3(-8.962f,987.7f,-66.150f), Quaternion.identity, Vector3.one),
        new("Box1", new Vector3(-6.267f,987.7f,-65.535f), Quaternion.identity, Vector3.one),
        new("Box1", new Vector3(-7.598f,987.7f,-63.764f), Quaternion.identity, Vector3.one),
        new("Box3", new Vector3(-18.959f,988.35f,-65.583f), Quaternion.Euler(0,90,0), Vector3.one * 2),
        new("Box1", new Vector3(-23.802f,988.35f,-65.306f), Quaternion.identity, Vector3.one * 2),
    };
    private readonly struct SurfaceBox { public SurfaceBox(string name, Vector3 position, Quaternion rotation, Vector3 scale) { Name=name; Position=position; Rotation=rotation; Scale=scale; } public string Name { get; } public Vector3 Position { get; } public Quaternion Rotation { get; } public Vector3 Scale { get; } }
}
