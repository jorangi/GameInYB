using UnityEngine;

[CreateAssetMenu(menuName="Ability/SpinReposition")]
public sealed class SpinRepositionConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc)
    {
        return new SpinReposition {
            NextReadyTime = 0,
            // RequiredRunway = runway,
        };
    }
}