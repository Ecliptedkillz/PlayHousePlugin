using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using LabApi.Features.Console;

namespace PlayhousePlugin.External;

public sealed class DonatorRepository
{
    private string sourcePath = string.Empty;
    private readonly Dictionary<string, Donator> byUserId =
        new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyCollection<Donator> All => byUserId.Values;

    public bool TryGet(string userId, out Donator donator) =>
        byUserId.TryGetValue(NormalizeUserId(userId), out donator!);

    public void Load(string path)
    {
        sourcePath = path ?? string.Empty;
        byUserId.Clear();
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
        {
            Logger.Warn($"Donator CSV does not exist: {path}");
            return;
        }

        int lineNumber = 0;
        foreach (string line in File.ReadLines(path))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                continue;

            string[] values = line.Split(',');
            if (values.Length < 3 ||
                !int.TryParse(values[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int tier))
            {
                Logger.Warn($"Ignoring invalid donator CSV row {lineNumber} in {path}.");
                continue;
            }

            var donator = new Donator
            {
                UserId = NormalizeUserId(values[0]),
                Tier = tier,
                IsBooster = values[2].Trim() is "1" or "true" or "True",
                Preference = values.Length > 3 ? values[3].Trim() : string.Empty,
            };
            byUserId[donator.UserId] = donator;
        }

        Logger.Info($"Loaded {byUserId.Count} donator records.");
    }

    public void UpdatePreference(string userId, string preference)
    {
        string normalized = NormalizeUserId(userId);
        if (!byUserId.TryGetValue(normalized, out Donator donator)) return;
        donator.Preference = preference;
        if (string.IsNullOrWhiteSpace(sourcePath) || !File.Exists(sourcePath)) return;
        try
        {
            string[] lines = File.ReadAllLines(sourcePath);
            for (int index = 0; index < lines.Length; index++)
            {
                if (string.IsNullOrWhiteSpace(lines[index]) || lines[index].TrimStart().StartsWith("#")) continue;
                string[] values = lines[index].Split(',');
                if (values.Length < 3 || !NormalizeUserId(values[0]).Equals(normalized, StringComparison.OrdinalIgnoreCase)) continue;
                lines[index] = $"{values[0].Trim()},{values[1].Trim()},{values[2].Trim()},{preference}";
                File.WriteAllLines(sourcePath, lines);
                return;
            }
        }
        catch (Exception exception)
        {
            Logger.Error($"Unable to persist donor preference in {sourcePath}: {exception}");
        }
    }

    private static string NormalizeUserId(string userId)
    {
        string value = (userId ?? string.Empty).Trim();
        int providerSeparator = value.IndexOf('@');
        return providerSeparator < 0 ? value : value.Substring(0, providerSeparator);
    }
}
