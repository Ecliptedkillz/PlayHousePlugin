using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public sealed class CustomClassManager : IDisposable
{
    private readonly Dictionary<int, PlayerClassState> players = new();

    private AbilityHudService? abilityHud;

    public IEnumerable<PlayerClassState> Active => players.Values;

    public PlayerClassState Get(Player player)
    {
        if (!players.TryGetValue(
                player.PlayerId,
                out PlayerClassState? state))
        {
            state = new PlayerClassState(player);
            players[player.PlayerId] = state;
        }

        return state;
    }

    public bool TryGet(
        Player player,
        out PlayerClassState state)
    {
        return players.TryGetValue(
            player.PlayerId,
            out state!);
    }

    public bool TryGetSelectedAbility(
        Player player,
        out AbilityBase? ability)
    {
        ability = null;

        if (!TryGet(player, out PlayerClassState state))
            return false;

        CustomClassBase? customClass = state.CustomClass;

        if (customClass is null ||
            customClass.ActiveAbilities.Count == 0)
        {
            return false;
        }

        if (state.AbilityIndex < 0 ||
            state.AbilityIndex >= customClass.ActiveAbilities.Count)
        {
            state.AbilityIndex = 0;
        }

        ability = customClass.ActiveAbilities[state.AbilityIndex];
        return ability is not null;
    }

    public bool TryActivate(
        Player player,
        out string response)
    {
        EnsureHudInitialized();

        if (!TryGetSelectedAbility(
                player,
                out AbilityBase? ability) ||
            ability is null)
        {
            response =
                "Your class does not have any active abilities.";

            return false;
        }

        try
        {
            bool result = ability.Use(out response);

            LabApi.Features.Console.Logger.Debug(
                $"Ability activation: Player={player.Nickname}, " +
                $"Ability={ability.Name}, Result={result}, " +
                $"Response={response}");

            return result;
        }
        catch (Exception exception)
        {
            string className = TryGet(
                    player,
                    out PlayerClassState state)
                ? state.CustomClass?.Name ?? "Unknown"
                : "Unknown";

            LabApi.Features.Console.Logger.Error(
                $"Custom ability failed for {player.Nickname}: " +
                $"class '{className}', " +
                $"ability '{ability.Name}': {exception}");

            response =
                $"{ability.Name} failed. " +
                "The error was logged for an administrator.";

            return false;
        }
    }

    public bool TryCycle(
        Player player,
        out string response)
    {
        EnsureHudInitialized();

        if (!TryGet(
                player,
                out PlayerClassState state) ||
            state.CustomClass is null ||
            state.CustomClass.ActiveAbilities.Count == 0)
        {
            response =
                "You are not a custom class or have no active abilities.";

            return false;
        }

        state.AbilityIndex =
            (state.AbilityIndex + 1) %
            state.CustomClass.ActiveAbilities.Count;

        AbilityBase selected =
            state.CustomClass.ActiveAbilities[state.AbilityIndex];

        response = selected.GenerateHud();

        LabApi.Features.Console.Logger.Debug(
            $"Ability changed: Player={player.Nickname}, " +
            $"Class={state.CustomClass.Name}, " +
            $"Ability={selected.Name}, " +
            $"Index={state.AbilityIndex}");

        return true;
    }

    public void Assign(
        Player player,
        CustomClassBase customClass)
    {
        EnsureHudInitialized();

        PlayerClassState state = Get(player);

        state.CustomClass?.Dispose();
        state.CustomClass = customClass;

        state.AbilityIndex =
            customClass.ActiveAbilities.Count > 0
                ? 0
                : -1;

        LabApi.Features.Console.Logger.Info(
            $"Assigned custom class '{customClass.Name}' " +
            $"to {player.Nickname} ({player.UserId}) with " +
            $"{customClass.ActiveAbilities.Count} active " +
            "ability/abilities.");

        if (customClass.ActiveAbilities.Count > 0)
        {
            AbilityBase selected =
                customClass.ActiveAbilities[state.AbilityIndex];

            player.SendHint(
                selected.GenerateHud(),
                2f);
        }
    }

    public void Remove(Player player)
    {
        if (!players.TryGetValue(
                player.PlayerId,
                out PlayerClassState? state))
        {
            return;
        }

        state.CustomClass?.Dispose();
        state.CustomClass = null;
        state.AbilityIndex = -1;

        players.Remove(player.PlayerId);
    }

    public void Clear()
    {
        foreach (PlayerClassState state in players.Values)
        {
            state.CustomClass?.Dispose();
            state.CustomClass = null;
            state.AbilityIndex = -1;
        }

        players.Clear();
    }

    public void Dispose()
    {
        Clear();

        abilityHud?.Dispose();
        abilityHud = null;
    }

    private void EnsureHudInitialized()
    {
        if (abilityHud is not null)
            return;

        Runtime.PluginRuntime? runtime =
            PlayhousePlugin.Instance?.Runtime;

        if (runtime is null)
        {
            LabApi.Features.Console.Logger.Warn(
                "Ability HUD could not start because " +
                "PluginRuntime is currently unavailable.");

            return;
        }

        abilityHud = new AbilityHudService(
            runtime,
            this);

        LabApi.Features.Console.Logger.Info(
            "Ability HUD service started.");
    }
}

public sealed class PlayerClassState
{
    public PlayerClassState(Player player)
    {
        Player = player;
    }

    public Player Player { get; }

    public CustomClassBase? CustomClass { get; internal set; }

    public int AbilityIndex { get; set; } = -1;
}