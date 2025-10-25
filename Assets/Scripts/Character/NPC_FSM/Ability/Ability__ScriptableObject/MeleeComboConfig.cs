using System;
using UnityEngine;



[CreateAssetMenu(menuName = "Ability/MeleeAttack")]
public sealed class MeleeAttackConfig : AbilityConfig
{
    public override IAbility Build(NonPlayableCharacter npc) => new MeleeAttack(this);
}
