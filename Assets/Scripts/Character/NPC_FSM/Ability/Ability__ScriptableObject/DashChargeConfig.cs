using UnityEngine;

[CreateAssetMenu(menuName="Ability/DashCharge")]
public sealed class DashChargeConfig : AbilityConfig
{
    public float duration = 1f;
    public float dir;
    public float speed = 2f;
    public bool stopOnWall = false;
    public override IAbility Build(NonPlayableCharacter npc) => new DashCharge(this);
}