using UnityEngine;

public class IdleState : StateBase
{
    public override string Name => "Idle";
    public IdleState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }
    public override void Enter()
    {
        npc.AnimSetMoving(false);
        npc.SetDesiredMove(0f);
        float idleDur = Random.Range(bb.IdleDurationRange.x, bb.IdleDurationRange.y);
        bb.IdleEndTime = bb.TimeNow + idleDur;
        npc.SetMinStateLock(bb.MinStateDuration);
    }
    public override void Exit()
    {
    }
    public override void Update()
    {
        if (bb.TargetKnown && TryExecuteAbilityOnce(out var bestOne)) npc.RequestState<ChaseState>();
        
        if (!(bb.IsPrecipiceAhead || bb.IsWallAhead)
            && bb.CanSeeTarget
            && bb.DistToTarget <= bb.DetectEnter)
        {
            npc.RequestState<ChaseState>();
            return;
        }

        if (bb.TimeNow >= bb.IdleEndTime && !InMinLock())
        {
            if (Random.value < bb.WanderProbabilityAfterIdle)
            {
                npc.RequestState<WanderState>();
                return;
            }
            else
            {
                float idleDur = Random.Range(bb.IdleDurationRange.x, bb.IdleDurationRange.y);
                bb.IdleEndTime = bb.TimeNow + idleDur;
                npc.SetMinStateLock(bb.MinStateDuration);
            }
        }

        npc.SetDesiredMove(0);
        npc.SetRooted(true);
    }
    private bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
