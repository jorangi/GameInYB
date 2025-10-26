using UnityEngine;

[CreateAssetMenu(menuName="Ability/OverheadSlam")]
public sealed class OverheadSlamConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc)
    {
        return new OverheadSlam {
            NextReadyTime = 0,
            // RequiredRunway = runway,
        };
    }
}