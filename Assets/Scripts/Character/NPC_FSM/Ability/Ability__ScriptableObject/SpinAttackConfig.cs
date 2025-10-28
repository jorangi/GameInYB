using UnityEngine;

[CreateAssetMenu(menuName="Ability/SpinAttack")]
public sealed class SpinAttackConfig : AbilityConfig
{
    public bool rootDuring = true;
    public bool chaseTarget = false;
    public override IAbility Build(NonPlayableCharacter npc) => new SpinAttack(this);
}