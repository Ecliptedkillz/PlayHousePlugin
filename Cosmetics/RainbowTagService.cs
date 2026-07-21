using System;
using System.Collections.Generic;
using System.Linq;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.Cosmetics;

public sealed class RainbowTagService : IDisposable
{
    private static readonly string[] DefaultColors =
    {
        "pink", "red", "brown", "silver", "light_green", "crimson", "cyan", "#00FFFF",
        "deep_pink", "tomato", "yellow", "magenta", "blue_green", "orange", "lime",
        "green", "emerald", "carmine", "nickel", "mint", "army_green", "pumpkin"
    };

    private readonly HashSet<int> attached = new();
    private readonly HashSet<string> activeGroups;
    private readonly string[] colors;
    private readonly float interval;

    public RainbowTagService(Config config)
    {
        activeGroups = new HashSet<string>(config.ActiveGroups, StringComparer.OrdinalIgnoreCase);
        colors = config.UseCustomSequence && config.CustomSequence.Count > 0
            ? config.CustomSequence.Where(x => !string.IsNullOrWhiteSpace(x)).ToArray()
            : DefaultColors;
        interval = Math.Max(0.05f, config.TagInterval);
    }

    public void TryAttach(Player player)
    {
        if (player is null || attached.Contains(player.PlayerId) || !IsEligible(player))
            return;

        RainbowTagBehaviour behaviour = player.GameObject!.AddComponent<RainbowTagBehaviour>();
        behaviour.Initialize(colors, interval);
        attached.Add(player.PlayerId);
    }

    public void Remove(Player player)
    {
        if (player is null)
            return;

        RainbowTagBehaviour behaviour = player.GameObject!.GetComponent<RainbowTagBehaviour>();
        if (behaviour is not null)
            UnityEngine.Object.Destroy(behaviour);
        attached.Remove(player.PlayerId);
    }

    public void Dispose()
    {
        foreach (Player player in Player.List.ToArray())
            Remove(player);
        attached.Clear();
    }

    private bool IsEligible(Player player)
    {
        UserGroup group = ServerStatic.PermissionsHandler.GetUserGroup(player.UserId);
        if (group is null)
            return false;

        string? groupName = ServerStatic.PermissionsHandler.GetAllGroups()
            .Where(pair => ReferenceEquals(pair.Value, group))
            .Select(pair => pair.Key)
            .FirstOrDefault();
        return groupName is not null && activeGroups.Contains(groupName);
    }
}

internal sealed class RainbowTagBehaviour : MonoBehaviour
{
    private ServerRoles? roles;
    private string originalColor = string.Empty;
    private string[] colors = Array.Empty<string>();
    private float interval;
    private float nextCycle;
    private int position;

    public void Initialize(string[] sequence, float cycleInterval)
    {
        colors = sequence;
        interval = cycleInterval;
    }

    private void Awake()
    {
        roles = GetComponent<ServerRoles>();
        if (roles is null)
        {
            LabApi.Features.Console.Logger.Warn("Could not attach a rainbow tag: ServerRoles was not found.");
            Destroy(this);
            return;
        }

        originalColor = roles.Network_myColor;
        nextCycle = Time.time;
    }

    private void Update()
    {
        if (roles is null || colors.Length == 0 || Time.time < nextCycle)
            return;

        nextCycle = Time.time + interval;
        roles.Network_myColor = colors[position];
        position = (position + 1) % colors.Length;
    }

    private void OnDestroy()
    {
        if (roles is not null)
            roles.Network_myColor = originalColor;
    }
}
