using UnityEngine;

public class IdleState : StateBase
{
    public override string Name => "Idle";
    public IdleState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }
    public override void Enter()
    {
        npc.AnimSetMoving(false);
        npc.SetDesiredMove(0f);
        float idleDur = Random.Range(blackboard.IdleDurationRange.x, blackboard.IdleDurationRange.y);
        blackboard.IdleEndTime = blackboard.TimeNow + idleDur;
        npc.SetMinStateLock(blackboard.MinStateDuration);
    }
    public override void Exit()
    {
    }
    public override void Update()
    {
        if (blackboard.DistToTarget <= blackboard.AttackEnter && !InMinLock())
        {
            npc.RequestState<AttackState>();
            return;
        }
        // 타깃이 범위 내 + 볼 수 있음
        if (!(blackboard.IsPrecipiceAhead || blackboard.IsWallAhead) && blackboard.CanSeeTarget && blackboard.DistToTarget <= blackboard.DetectEnter && !InMinLock())
        {
            npc.RequestState<ChaseState>();
            return;
        }
        if (blackboard.TimeNow >= blackboard.IdleEndTime && !InMinLock())
        {
            bool goWander = Random.value < blackboard.WanderProbabilityAfterIdle;
            if (goWander)
            {
                npc.RequestState<WanderState>();
                return;
            }
            else
            {
                float idleDur = Random.Range(blackboard.IdleDurationRange.x, blackboard.IdleDurationRange.y);
                blackboard.IdleEndTime = blackboard.TimeNow + idleDur;
                npc.SetMinStateLock(blackboard.MinStateDuration);
            }
        }
        npc.SetDesiredMove(0);
        npc.SetRooted(true);
    }
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
