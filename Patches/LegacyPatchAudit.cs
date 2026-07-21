namespace PlayhousePlugin.Patches;

/// <summary>
/// Migration record for the EXILED-era Harmony patches. Keeping this beside the
/// live patches makes intentional retirements auditable during game updates.
/// </summary>
internal static class LegacyPatchAudit
{
    // Radio SCP speech: replaced by LabAPI SendingVoiceMessage handling.
    // SCP-096 UpdateVision: removed; the patched PlayableScps.Scp096 type no longer exists.
    // KeycardPickup.ProcessCollision / SCP-106 generator gate: the old femur-breaker
    // collision path no longer exists; Containment106ObjectiveController owns the flow.
    // SingleBulletHitreg and BuckshotHitreg Vanish filtering: replaced by
    // ValidatedVisibility and Hurting event cancellation for hidden player pairs.
    // PlaceBulletholeDecal: replaced by native PlacingBulletHole cancellation.
    // Dummy hit/audio crash transpilers: retired because the old StandardHitregBase and
    // FirearmExtensions targets are absent from the current server assembly.
    // CustomDamageType and profiler patches were commented out in the legacy source.
    // HintDisplay filtering remains active in HintDisplayPatch.
}
