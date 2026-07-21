using HarmonyLib;
using Hints;
using System;
using System.Reflection;

namespace PlayhousePlugin.Patches;

[HarmonyPatch(typeof(HintDisplay), nameof(HintDisplay.Show))]
public static class HintDisplayPatch
{
    private static readonly FieldInfo? EffectsField = AccessTools.Field(typeof(Hint), "_effects");

    public static bool Prefix(Hint hint)
    {
        if (hint is TranslationHint)
            return false;

        return EffectsField?.GetValue(hint) is not Array effects || effects.Length == 0;
    }
}
