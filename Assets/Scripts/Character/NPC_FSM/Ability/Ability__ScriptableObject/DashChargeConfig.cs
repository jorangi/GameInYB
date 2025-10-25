using UnityEngine;

[CreateAssetMenu(menuName="Ability/DashCharge")]
public sealed class DashChargeConfig : AbilityConfig
{
    public float prefer = 3.5f;
    public float runway = 3f;
    public override IAbility Build(NonPlayableCharacter npc)
    {
        return new DashChargeAbility {
            NextReadyTime = 0,
            // EnterRange = enter, ExitRange = exit, PreferRange = prefer,
            // RequiredRunway = runway,
        };
    }
}