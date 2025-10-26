using UnityEngine;

[CreateAssetMenu(menuName="Ability/PushKnockback")]
public sealed class PushKnockbackConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc)
    {
        return new PushKnockback {
            NextReadyTime = 0,
            // RequiredRunway = runway,
        };
    }
}