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
        if (bb.TargetKnown && TryExecuteAbilityOnce(out var bestOne) && npc.Profile.isAggressive) npc.RequestState<ChaseState>();
        //공격 범위 내에 플레이어가 있을 경우
        if (bb.AttackExit > bb.DistToTarget && bb.AttackEnter < bb.DistToTarget)
        {
            npc.RequestState<AttackState>();
            return;
        }
        // 공격 범위 바깥에 플레이어가 있을 경우
        if (bb.AttackExit < bb.DistToTarget && npc.Profile.isAggressive)
        {
            npc.RequestState<ChaseState>();
            return;
        }
        // 공격 범위보다 플레이어가 가까울 경우
        if (bb.AttackEnter > bb.DistToTarget && !InMinLock())
        {
            npc.SetDesiredMove(-npc.FacingSign);
            SetMinDuration();
        }
    }
    private bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
