using UnityEngine;

[CreateAssetMenu(menuName="Ability/OverheadSlam")]
public sealed class OverheadSlamConfig : AbilityConfig
{
    public bool chaseTarget = false;
    public override IAbility Build(NonPlayableCharacter npc) => new OverheadSlam(this);
}