using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public abstract class AbilityBase
{
    protected AbilityBase(Player player) => Player = player;
    public abstract string Name { get; }
    public Player Player { get; }
    public abstract bool Use(out string response);
    public virtual string GenerateHud() => $"Selected: {Name} (Ready)";
}
