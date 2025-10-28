using UnityEngine;

[CreateAssetMenu(menuName="Ability/PushKnockback")]
public sealed class PushKnockbackConfig : AbilityConfig
{
    public bool rootDuring = true;
    public float advanceDistanceOnHit = 0.0f;
    public override IAbility Build(NonPlayableCharacter npc) => new PushKnockback(this);
}