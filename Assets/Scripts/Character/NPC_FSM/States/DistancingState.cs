using UnityEngine;

/// <summary>
/// 최소 공격거리 유지 상태
/// </summary>
public class DistancingState : StateBase
{
    public DistancingState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }

    public override string Name => "Distancing";

    public override void Enter()
    {
        npc.AnimSetMoving(true);
        npc.SetRooted(false);
    }

    public override void Exit()
    {

    }

    public override void Update()
    {
        //공격 범위 내에 플레이어가 있을 경우
        if (blackboard.AttackExit > blackboard.DistToTarget && blackboard.AttackEnter < blackboard.DistToTarget)
        {
            npc.RequestState<AttackState>();
            return;
        }
        // 공격 범위 바깥에 플레이어가 있을 경우
        if (blackboard.AttackExit < blackboard.DistToTarget)
        {
            npc.RequestState<ChaseState>();
            return;
        }
        // 공격 범위보다 플레이어가 가까울 경우
        if (blackboard.AttackEnter > blackboard.DistToTarget && !InMinLock())
        {
            npc.SetDesiredMove(-npc.FacingSign);
            blackboard.MinStateEndTime = blackboard.TimeNow + blackboard.MinStateDuration;
        }
    }
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
