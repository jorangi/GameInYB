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
    private const float UnreachableUpThreshold = 1.25f; // 자기보다 이 높이 이상 위면 '상단'으로 간주
    private bool HasUpwardAbilityReady()
    {
        // TODO: 나중에 IAbility에 Tags/Meta를 넣으면, a.Tags.Has(Upward) && a.CanExecute(_ctx)
        return false;
    }
    float _holdUntil;
    public override void Update()
    {
        npc.SenseOverheadPlatform();
        if (bb.target == null)
        {
            npc.RequestState<IdleState>();
            return;
        }
        // 1) 먼저 한 번 공격 시도(있으면 이 틱 종료)
        if (bb.targetKnown)
        {
            if (npc.IsAbilityRunning && npc.RunningAbilityCfg != null)
                {
                    if (!npc.RunningAbilityCfg.rootDuring)
                    {
                        npc.SetRooted(false);
                        npc.SetDesiredMove(-Mathf.Sign(bb.DistToTarget));
                        float spd = npc.RunningAbilityCfg.speed;
                        npc.specialSpd = spd;
                        return;
                    }
                }

            if (TryExecuteAbilityOnce(out var bestOne) || npc.AnimGetTriggerAttack()) return;
            else if (bestOne != null)
            {
                //현재 공격을 당장은 할 수 없을 경우 해당 공격에 적합한 위치로 이동
                float enter = bestOne.OptimalDistanceRange.x;
                float exit = bestOne.OptimalDistanceRange.y;
                float center = 0.5f * (enter + exit);

                const float dead = 0.10f;              // 10cm 정도
                const float hysteresis = 0.20f;        // 20cm 정도

                float delta = bb.DistToTarget - center;
                int dirToTarget = Mathf.Sign(bb.target.position.x - bb.self.position.x) >= 0 ? 1 : -1;

                if (bb.TimeNow < _holdUntil)
                {
                    npc.SetDesiredMove(0);
                    npc.AnimSetMoving(false);
                    npc.FacingSign = Mathf.Sign(dirToTarget);
                    npc.flipSprite(npc.FacingSign > 0);
                    return;
                }

                //유지 시간 처리
                if (Mathf.Abs(delta) <= hysteresis)
                {
                    _holdUntil = bb.TimeNow + Random.value * 0.3f + 0.2f;
                    npc.FacingSign = Mathf.Sign(dirToTarget);
                    npc.SetDesiredMove(0);
                    npc.AnimSetMoving(false);
                    npc.flipSprite(npc.FacingSign > 0);
                    return;
                }
                if (Mathf.Abs(delta) <= dead)
                {
                    _holdUntil = bb.TimeNow + Random.value * 0.3f + 0.2f;
                    npc.FacingSign = Mathf.Sign(dirToTarget);
                    npc.SetDesiredMove(0);
                    npc.AnimSetMoving(false);
                    npc.flipSprite(npc.FacingSign > 0);
                    return;
                }
                if (Mathf.Abs(delta) > 0.1f && !bb.IsPrecipiceAhead && bb.IsWallAhead)
                {
                    int approach = (delta > 0f) ? +1 : -1; // +1=접근, -1=후퇴
                    int moveSign = dirToTarget * approach;
                    npc.SetRooted(false);
                    npc.AnimSetMoving(true);
                    npc.FacingSign = dirToTarget;   // 정면 요구 능력이면 유지
                    npc.SetDesiredMove(moveSign);
                    return;
                }
            }
        }


        // 2) '머리 위 발판 + 상향 불가' 케이스 차단
        const float UnreachableUpThreshold = 1.25f; // 자기보다 이 높이 이상 위면 '상단'으로 간주
        float dy = bb.target.position.y - bb.self.position.y;
        bool targetFarAbove = dy > UnreachableUpThreshold;
        bool overheadBlocked = bb.HasOverheadPlatform; // ★ Blackboard에 추가 필요
        bool upwardReady = false; // 현재 상향 공격 없음(추후 상향 Ability가 생기면 실제 체크로 교체)

        if (bb.targetKnown && targetFarAbove && overheadBlocked && !upwardReady)
        {
            // 이동/추격 중단(빙빙 도는 현상 방지), 기억 타이머로 자연 복귀
            npc.AnimSetMoving(false);
            npc.SetRooted(true);

            // 기억 만료되면 Idle/Wander로 전환
            if (!InMinLock() && (bb.TimeNow - bb.LastSeenTime) > bb.LostMemoryTime)
            {
                if (Random.value < bb.WanderProbabilityAfterIdle)
                {
                    npc.RequestState<WanderState>();
                    return;
                }
                npc.RequestState<IdleState>();
                return;
            }

            // 아직 기억이 남아있으면 그 자리에서 대기(다음 틱 재판단)
            return;
        }

        // 3) 일반 추격 이동
        npc.SetRooted(false);
        npc.AnimSetMoving(true);

        Vector3 aimPos = bb.CanSeeTarget ? bb.target.position : bb.LastKnownPos;
        float dir = Mathf.Sign(aimPos.x - bb.self.position.x);
        if (!bb.CanSeeTarget)
        {
            if (bb.targetKnown && bb.TimeNow >= bb.FlipCooldownEnd)
            {
                bb.FlipCooldownEnd = bb.TimeNow + bb.FlipCooldown;
                npc.FacingSign = dir;
                SetMinDuration();
            }
            else
            {
                SetMinDuration();
            }
        }
        npc.SetDesiredMove(npc.FacingSign);

        // 4) 장애물 대응(기존 로직 유지)
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

        // 5) 인지 범위 이탈(기억 기반)
        if (!bb.targetKnown && !InMinLock())
        {
            if (MemoryTime < bb.TimeNow)
            {
                MemoryTime = bb.TimeNow + bb.LostMemoryTime;
            }
            else
            {
                if (Random.value < bb.WanderProbabilityAfterIdle)
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
