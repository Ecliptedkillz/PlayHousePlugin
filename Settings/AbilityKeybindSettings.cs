using System;
using System.Linq;
using LabApi.Features.Wrappers;
using PlayhousePlugin.CustomClasses;
using UnityEngine;
using UserSettings.ServerSpecific;

namespace PlayhousePlugin.Settings;

public sealed class AbilityKeybindSettings : IDisposable
{
    private const int ActivateId = 77101;
    private const int CycleId = 77102;
    private readonly CustomClassManager classes;
    private readonly ServerSpecificSettingBase[] definitions;

    public AbilityKeybindSettings(CustomClassManager classes)
    {
        this.classes = classes;
        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        definitions = new ServerSpecificSettingBase[]
        {
            new SSGroupHeader("Playhouse Custom Classes", false, "These keys can be changed here for this server."),
            new SSKeybindSetting(ActivateId, "Use selected class ability", KeyCode.F, true, false, "Activates your selected custom-class ability."),
            new SSKeybindSetting(CycleId, "Select next class ability", KeyCode.G, true, false, "Cycles to the next ability and displays it."),
        };
        ServerSpecificSettingsSync.DefinedSettings = existing
            .Where(setting => setting.SettingId != ActivateId && setting.SettingId != CycleId)
            .Concat(definitions)
            .ToArray();
        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSettingChanged;
        ServerSpecificSettingsSync.SendToAll();
    }

    private void OnSettingChanged(ReferenceHub hub, ServerSpecificSettingBase setting)
    {
        if (setting is not SSKeybindSetting keybind || !keybind.SyncIsPressed)
            return;
        Player? player = Player.Get(hub);
        if (player is null)
            return;

        bool succeeded;
        string response;
        if (setting.SettingId == ActivateId)
            succeeded = classes.TryActivate(player, out response);
        else if (setting.SettingId == CycleId)
            succeeded = classes.TryCycle(player, out response);
        else
            return;
        player.SendHint(succeeded ? response : $"<color=red>{response}</color>", succeeded ? 2f : 3f);
    }

    public void Dispose()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSettingChanged;
        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? Array.Empty<ServerSpecificSettingBase>();
        ServerSpecificSettingsSync.DefinedSettings = existing
            .Where(setting => setting.SettingId != ActivateId && setting.SettingId != CycleId && !definitions.Contains(setting))
            .ToArray();
        ServerSpecificSettingsSync.SendToAll();
    }
}
