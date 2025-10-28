using System;
using UnityEngine;



[CreateAssetMenu(menuName = "Ability/MeleeAttack")]
public sealed class MeleeAttackConfig : AbilityConfig
{
    public bool rootDuring = true;
    public float advanceDistanceOnHit = 0.0f;
    public override IAbility Build(NonPlayableCharacter npc) => new MeleeAttack(this);
}
