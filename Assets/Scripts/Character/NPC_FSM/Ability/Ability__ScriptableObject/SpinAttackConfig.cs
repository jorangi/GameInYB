using UnityEngine;

[CreateAssetMenu(menuName="Ability/SpinAttack")]
public sealed class SpinAttackConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc) => new SpinAttack(this);
}