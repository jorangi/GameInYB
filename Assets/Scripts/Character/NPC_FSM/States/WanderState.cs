using UnityEngine;

public class WanderState : StateBase
{
    public WanderState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard)
    {
    }

    public override string Name => "Wander";

    public override void Enter()
    {
        Debug.Log("이동 시작");

        //이동, 비고정 등록
        npc.AnimSetMoving(true);
        npc.SetRooted(false);

        //이동 방향 결정
        if (npc.FacingSign != -1 && npc.FacingSign != +1)
            npc.FacingSign = Random.value < 0.5f ? -1 : +1;
        npc.SetDesiredMove(npc.FacingSign);

        //이동 시간 결정
        float dur = Random.Range(blackboard.WanderDurationRange.x, blackboard.WanderDurationRange.y);
        blackboard.WanderEndTime = blackboard.TimeNow + dur;

        //장애물(낭떠러지, 벽) 충돌 판단 시간 초기화
        blackboard.NextObstacleDecisionTime = blackboard.TimeNow;
        //최소 유지시간
        npc.SetMinStateLock(blackboard.MinStateDuration);
    }

    public override void Exit()
    {
        
        Debug.Log("이동 종료");
        npc.SetDesiredMove(0f);
        npc.AnimSetMoving(false);
        npc.SetRooted(true);
    }

    public override void Update()
    {
        Debug.Log("이동중");
        //타깃 감지
        Debug.Log($"{!InMinLock()} && {blackboard.CanSeeTarget} && {blackboard.DistToTarget} <= {blackboard.DetectEnter}");
        if (!InMinLock() && blackboard.CanSeeTarget && blackboard.DistToTarget <= blackboard.DetectEnter)
        {
            Debug.Log("1번");
            npc.RequestState<ChaseState>();
            return;
        }
        //장애물 대응
        if (blackboard.TimeNow >= blackboard.NextObstacleDecisionTime && (blackboard.IsWallAhead || !blackboard.IsPrecipiceAhead))
        {
            float r = Random.value;
            //정지
            if (r < blackboard.StopAtObstacleChance && !InMinLock())
            {
                npc.RequestState<IdleState>();
                return;
            }

            //방향 전환 후 계속 이동
            if (r < blackboard.StopAtObstacleChance + blackboard.FlipAtObstacleChance)
            {
                
                npc.FacingSign = -npc.FacingSign;
                npc.SetDesiredMove(npc.FacingSign);

                //잦은 방향 전환 방지
                blackboard.NextObstacleDecisionTime = blackboard.TimeNow + blackboard.ObstacleDecisionCooldown;
            }
        }
        
        npc.SetDesiredMove(npc.FacingSign);
        Debug.Log($"{npc.FacingSign}");
    }
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
