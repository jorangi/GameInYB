using UnityEngine;

public class WanderState : StateBase
{
    public WanderState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard)
    {
    }

    public override string Name => "Wander";

    public override void Enter()
    {
        //이동, 비고정 등록
        npc.AnimSetMoving(true);
        npc.SetRooted(false);

        //이동 방향 결정
        if (blackboard.IsPrecipiceAhead || blackboard.IsWallAhead)
        {
            npc.FacingSign = -npc.FacingSign;
            blackboard.NextObstacleDecisionTime = blackboard.TimeNow + blackboard.ObstacleDecisionCooldown;
            blackboard.IsPrecipiceAhead = false;
            blackboard.IsWallAhead = false;
        }
        else
        {
            if (npc.FacingSign != -1 && npc.FacingSign != +1)
                npc.FacingSign = Random.value < 0.5f ? -1 : +1;
        }
        npc.SetDesiredMove(npc.FacingSign);

        //이동 시간 결정
        float dur = Random.Range(blackboard.WanderDurationRange.x, blackboard.WanderDurationRange.y);
        blackboard.WanderEndTime = blackboard.TimeNow + dur;
        //최소 유지시간
        npc.SetMinStateLock(blackboard.MinStateDuration);
    }

    public override void Exit()
    {
        npc.SetDesiredMove(0f);
        npc.AnimSetMoving(false);
        npc.SetRooted(true);
    }

    public override void Update()
    {
        //장애물 대응
        if (blackboard.TimeNow >= blackboard.NextObstacleDecisionTime && (blackboard.IsWallAhead || blackboard.IsPrecipiceAhead))
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
                //잦은 방향 전환 방지
                blackboard.NextObstacleDecisionTime = blackboard.TimeNow + blackboard.ObstacleDecisionCooldown;
            }
        }
        //타깃 감지
        if (!InMinLock() && blackboard.CanSeeTarget && blackboard.DistToTarget <= blackboard.DetectEnter && !blackboard.IsWallAhead && !blackboard.IsPrecipiceAhead)
        {
            npc.RequestState<ChaseState>();
            return;
        }
        
        npc.SetDesiredMove(npc.FacingSign);
    }
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
