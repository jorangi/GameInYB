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
        if (bb.IsPrecipiceAhead || bb.IsWallAhead)
        {
            npc.FacingSign = -npc.FacingSign;
            bb.NextObstacleDecisionTime = bb.TimeNow + bb.ObstacleDecisionCooldown;
            bb.IsPrecipiceAhead = false;
            bb.IsWallAhead = false;
        }
        else
        {
            if (npc.FacingSign != -1 && npc.FacingSign != +1)
                npc.FacingSign = Random.value < 0.5f ? -1 : +1;
        }
        npc.SetDesiredMove(npc.FacingSign);

        //이동 시간 결정
        float dur = Random.Range(bb.WanderDurationRange.x, bb.WanderDurationRange.y);
        bb.WanderEndTime = bb.TimeNow + dur;
        //최소 유지시간
        npc.SetMinStateLock(bb.MinStateDuration);
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
        if (bb.TimeNow >= bb.NextObstacleDecisionTime && (bb.IsWallAhead || bb.IsPrecipiceAhead))
        {
            float r = Random.value;
            //정지
            if (r < bb.StopAtObstacleChance && !InMinLock())
            {
                npc.RequestState<IdleState>();
                return;
            }

            //방향 전환 후 계속 이동
            if (r < bb.StopAtObstacleChance + bb.FlipAtObstacleChance)
            {
                npc.FacingSign = -npc.FacingSign;
                //잦은 방향 전환 방지
                bb.NextObstacleDecisionTime = bb.TimeNow + bb.ObstacleDecisionCooldown;
            }
        }
        //타깃 감지
        if (!InMinLock() && bb.CanSeeTarget && bb.DistToTarget <= bb.DetectEnter && !bb.IsWallAhead && !bb.IsPrecipiceAhead)
        {
            npc.RequestState<ChaseState>();
            return;
        }
        
        npc.SetDesiredMove(npc.FacingSign);
    }
    private bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
