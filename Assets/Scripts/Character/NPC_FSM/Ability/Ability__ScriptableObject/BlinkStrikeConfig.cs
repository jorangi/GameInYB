using UnityEngine;

[CreateAssetMenu(menuName="Ability/BlinkStrike")]
public sealed class BlinkStrikeConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc) => new BlinkStrike(this);
}