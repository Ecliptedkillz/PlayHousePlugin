using System.Reflection;
using HarmonyLib;
using LabApi.Features.Console;

namespace PlayhousePlugin.Patches;

public sealed class PatchManager
{
    private readonly Harmony harmony = new("com.kognity.playhouseplugin.labapi");

    public void Apply()
    {
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Logger.Info("Applied PlayhousePlugin Harmony patches.");
    }

    public void Remove()
    {
        harmony.UnpatchAll(harmony.Id);
        Logger.Info("Removed PlayhousePlugin Harmony patches.");
    }
}
