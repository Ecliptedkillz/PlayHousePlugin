using System.Collections.Generic;

namespace PlayhousePlugin;

public sealed class RuntimeState
{
    public HashSet<int> InfiniteDropPlayerIds { get; } = new();
    public HashSet<int> InfiniteAmmoPlayerIds { get; } = new();
    public HashSet<int> InvisiblePlayerIds { get; } = new();
    public HashSet<int> InfectedPlayerIds { get; } = new();
    public HashSet<string> HiddenPlayerPairs { get; } = new();
    public HashSet<string> PendingStatisticsDeletion { get; } = new(System.StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, int> ContentGunRounds { get; } = new();
    public bool WipeRadiosOnSpawn { get; set; }
    public bool ScpSwapAllowed { get; set; }
    public bool BulletHolesDisabled { get; set; }
}
