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
        npc.FacingSign = Mathf.Sign(-bb.DistToTarget);
        npc.SetDesiredMove(npc.FacingSign);

        //장애물(낭떠러지, 벽) 충돌 판단 시간 초기화
        bb.NextObstacleDecisionTime = bb.TimeNow;
        //최소 유지시간
        npc.SetMinStateLock(bb.MinStateDuration);
    }

    public override void Exit()
    {
    }
    float MemoryTime = 0.0f;
    public override void Update()
    {
        npc.AnimSetMoving(true);
        npc.SetRooted(false);
        npc.FacingSign = Mathf.Sign(bb.target.position.x - bb.self.position.x);
        npc.SetDesiredMove(npc.FacingSign);

        //공격 거리 내에 들어온 플레이어
        if (bb.DistToTarget < bb.AttackExit && bb.DistToTarget > bb.AttackEnter)
        {
            npc.RequestState<AttackState>();
            return;
        }
        //공격 거리 보다 가까운 플레이어
        if (bb.DistToTarget < bb.AttackEnter)
        {
            npc.RequestState<DistancingState>();
            return;
        }
        // 장애물 대응
        if (bb.TimeNow >= bb.NextObstacleDecisionTime && (bb.IsWallAhead || bb.IsPrecipiceAhead))
        {
            if (Random.value < bb.WanderProbabilityAfterIdle)
            {
                npc.RequestState<WanderState>();
                return;
            }
            npc.RequestState<IdleState>();
            return;
        }
        //인지 거리 밖으로 나간 플레이어
        if (bb.DetectExit < bb.DistToTarget && !InMinLock())
        {
            //기억 시간 설정
            if (MemoryTime < bb.TimeNow)
            {
                MemoryTime = bb.LostMemoryTime + bb.TimeNow;
            }
            else
            {
                float r = Random.value;
                if (r < bb.WanderProbabilityAfterIdle)
                {
                    npc.RequestState<WanderState>();
                    return;
                }
                npc.RequestState<IdleState>();
                return;
            }
        }
    }
    
    private bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
