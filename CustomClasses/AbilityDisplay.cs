using LabApi.Features.Wrappers;
using UnityEngine;

namespace PlayhousePlugin.CustomClasses;

public static class AbilityDisplay
{
    public static void Show(
        Player player,
        string message,
        float duration = 3f,
        Color? color = null)
    {
        if (player is null)
            return;

        Color displayColor = color ?? Color.yellow;
        string hexColor = ColorUtility.ToHtmlStringRGB(displayColor);

        player.SendHint(
            $"<size=26><b><color=#{hexColor}>{message}</color></b></size>",
            duration);
    }
}