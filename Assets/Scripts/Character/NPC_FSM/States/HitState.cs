using UnityEngine;

public class HitState : StateBase
{
    private float _hitStunEndTime;
    public HitState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }

    public override string Name => "Hit";

    public override void Enter()
    {
        npc.SetDesiredMove(0f);
        npc.SetRooted(true);
        npc.AnimSetMoving(false);
        npc.AnimTriggerHit();
        _hitStunEndTime = blackboard.TimeNow + npc.NPCData.HitStunTime;
        blackboard.InvinsibleEndTime = blackboard.TimeNow + npc.NPCData.InvincibleTime;
        npc.SetMinStateLock(blackboard.MinStateDuration);
        npc.hitBox.enabled = false;
    }

    public override void Exit()
    {
        npc.hitBox.enabled = true;
    }

    public override void Update()
    {
        if (blackboard.TimeNow < _hitStunEndTime)
        {
            return;
        }
        npc.SetRooted(false);
        if (!npc.InMinStateLock())
        {
            if (!InMinLock() && blackboard.CanSeeTarget && blackboard.DistToTarget <= blackboard.DetectEnter && !blackboard.IsWallAhead && !blackboard.IsPrecipiceAhead)
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
    public bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
