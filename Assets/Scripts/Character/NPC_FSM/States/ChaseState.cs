using UnityEngine;

public class ChaseState : StateBase
{
    public ChaseState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }

    public override string Name => "Chase";

    public override void Enter()
    {
        //이동, 비고정 등록
        npc.AnimSetMoving(true);
        npc.SetRooted(false);
        npc.FacingSign = Mathf.Sign(-blackboard.DistToTarget);
        npc.SetDesiredMove(npc.FacingSign);

        //장애물(낭떠러지, 벽) 충돌 판단 시간 초기화
        blackboard.NextObstacleDecisionTime = blackboard.TimeNow;
        //최소 유지시간
        npc.SetMinStateLock(blackboard.MinStateDuration);
    }

    public override void Exit()
    {
    }
    float MemoryTime = 0.0f;
    public override void Update()
    {
        npc.AnimSetMoving(true);
        npc.SetRooted(false);
        npc.FacingSign = Mathf.Sign(blackboard.target.position.x - blackboard.self.position.x);
        npc.SetDesiredMove(npc.FacingSign);

        //공격 거리 내에 들어온 플레이어
        if (blackboard.DistToTarget < blackboard.AttackExit && blackboard.DistToTarget > blackboard.AttackEnter)
        {
            npc.RequestState<AttackState>();
            return;
        }
        //공격 거리 보다 가까운 플레이어
        if (blackboard.DistToTarget < blackboard.AttackEnter)
        {
            npc.RequestState<DistancingState>();
            return;
        }
        // 장애물 대응
        if (blackboard.TimeNow >= blackboard.NextObstacleDecisionTime && (blackboard.IsWallAhead || blackboard.IsPrecipiceAhead))
        {
            if (Random.value < blackboard.WanderProbabilityAfterIdle)
            {
                npc.RequestState<WanderState>();
                return;
            }
            npc.RequestState<IdleState>();
            return;
        }
        //인지 거리 밖으로 나간 플레이어
        if (blackboard.DetectExit < blackboard.DistToTarget && !InMinLock())
        {
            //기억 시간 설정
            if (MemoryTime < blackboard.TimeNow)
            {
                MemoryTime = blackboard.LostMemoryTime + blackboard.TimeNow;
            }
            else
            {
                float r = Random.value;
                if (r < blackboard.WanderProbabilityAfterIdle)
                {
                    npc.RequestState<WanderState>();
                    return;
                }
                npc.RequestState<IdleState>();
                return;
            }
        }
    }
    
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
