using LabApi.Features.Wrappers;

namespace PlayhousePlugin.CustomClasses;

public abstract class NonCooldownAbilityBase : AbilityBase
{
    protected NonCooldownAbilityBase(Player player) : base(player) { }
    public override bool Use(out string response) => UseAbility(out response);
    protected abstract bool UseAbility(out string response);
}
