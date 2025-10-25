using UnityEngine;

public class HitState : StateBase
{
    private float _hitStunEndTime;
    public HitState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }

    public override string Name => "Hit";

    public override void Enter()
    {
        npc.hitBox.gameObject.SetActive(false);
        npc.SetDesiredMove(0f);
        npc.SetRooted(true);
        npc.AnimSetMoving(false);
        npc.AnimTriggerHit();
        _hitStunEndTime = bb.TimeNow + npc.NPCData.HitStunTime;
        bb.InvinsibleEndTime = bb.TimeNow + npc.NPCData.InvincibleTime;
        npc.SetMinStateLock(bb.MinStateDuration);
    }

    public override void Exit()
    {
    }

    public override void Update()
    {
        if (bb.TimeNow < _hitStunEndTime)
            return;
        npc.hitBox.gameObject.SetActive(bb.TimeNow > bb.InvinsibleEndTime);
        npc.SetRooted(false);
        if (!npc.InMinStateLock())
        {
            if (!InMinLock() && bb.CanSeeTarget && bb.DistToTarget <= bb.DetectEnter && !bb.IsWallAhead && !bb.IsPrecipiceAhead)
            {
                npc.RequestState<ChaseState>();
                return;
            }
            else
            {
                npc.RequestState<IdleState>();
            }
        }
    }
    public bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
