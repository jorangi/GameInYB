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
        if (!InMinLock() && bb.TimeNow >= bb.WanderEndTime)
        {
            if (Random.value < 0.5f)
            {
                npc.RequestState<IdleState>();
                return;
            }
            else
            {
                npc.FacingSign = Random.value < 0.5f ? -1 : +1;
                float dur = Random.Range(bb.WanderDurationRange.x, bb.WanderDurationRange.y);
                bb.WanderEndTime = bb.TimeNow + dur;
                npc.SetMinStateLock(bb.MinStateDuration);
                npc.SetDesiredMove(npc.FacingSign);
                npc.SetRooted(false);
                npc.AnimSetMoving(true);
                return;
            }
        }
        if (bb.TargetKnown && TryExecuteAbilityOnce(out var bestOne) && npc.Profile.isAggressive) npc.RequestState<ChaseState>();
        if (bb.TimeNow >= bb.NextObstacleDecisionTime && (bb.IsWallAhead || bb.IsPrecipiceAhead))//장애물 충돌 판단
        {
            float r = Random.value;
            
            if (r < bb.StopAtObstacleChance && !InMinLock())
            {
                npc.RequestState<IdleState>();
                return;
            }
            if (r < bb.StopAtObstacleChance + bb.FlipAtObstacleChance)
            {
                npc.FacingSign = -npc.FacingSign;
                bb.NextObstacleDecisionTime = bb.TimeNow + bb.ObstacleDecisionCooldown;
            }
        }
        if (!InMinLock() && bb.CanSeeTarget && bb.DistToTarget <= bb.DetectEnter && !bb.IsWallAhead && !bb.IsPrecipiceAhead && npc.Profile.isAggressive)//감지 범위 내 진입
        {
            npc.RequestState<ChaseState>();
            return;
        }
        npc.SetDesiredMove(npc.FacingSign);
    }
    private bool InMinLock() => bb.TimeNow < bb.MinStateEndTime;
}
