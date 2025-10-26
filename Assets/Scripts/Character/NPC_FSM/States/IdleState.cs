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
        if (bb.DistToTarget <= bb.AttackEnter)
        {
            npc.RequestState<AttackState>();
            return;
        }
        // 타깃이 범위 내 + 볼 수 있음
        if (!(bb.IsPrecipiceAhead || bb.IsWallAhead) && bb.CanSeeTarget && bb.DistToTarget <= bb.DetectEnter)
        {
            npc.RequestState<ChaseState>();
            return;
        }
        if (bb.TimeNow >= bb.IdleEndTime && !InMinLock())
        {
            bool goWander = Random.value < bb.WanderProbabilityAfterIdle;
            if (goWander)
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
