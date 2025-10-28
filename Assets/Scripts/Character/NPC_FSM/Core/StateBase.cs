using System;
using System.Collections.Generic;
using UnityEngine;

public enum Personality
{
    AGGRESSIVE,
    DEFENSIVE
}
public enum Mobility
{
    GROUND,
    FLY
}
public interface IStateBase
{
    string Name { get; }
    public void Enter();
    public void Update();
    public void Exit();
}
[Serializable]
public class Blackboard
{
    //----------런타임------------
    public Transform self;
    public Transform target;
    public float TimeNow;
    public bool CanSeeTarget;
    public float DistToTarget;

    // 센싱
    public bool IsWallAhead;
    public bool IsPrecipiceAhead;
    public NonPlayableCharacter[] enemies;
    public bool TargetKnown;
    public bool HasOverheadPlatform;

    //Idle/Wander용 타이머
    public float IdleEndTime;
    public float WanderEndTime;

    //최소 상태 유지 시간 (움찔 방지)
    public float MinStateEndTime;
    //공격 관련
    public Personality personality; //성격
    public Mobility moveType; //이동 타입
    public float AttackCooldownEnd; //공격 쿨다운(콤보 마지막)
    public int ComboStep; // 현재 콤보
    public int MaxCombo; // 최대 콤보
    public float ComboBuffer = 0.5f; //콤보 사이 유예시간
    public bool IsInCombo;
    public float LastSeenTime;
    public Vector3 LastKnownPos;
    public float FlipCooldownEnd;

    //---------몬스터별 데이터----------
    [Header("인지")]
    public float DetectEnter = 6.0f;
    public float DetectExit = 7.5f;
    public float LostMemoryTime = 1.0f;

    [Header("Idle / Wander")]
    public Vector2 IdleDurationRange = new(0.8f, 1.6f);
    public Vector2 WanderDurationRange = new(1.0f, 2.0f);
    [Range(0f, 1f)] public float WanderProbabilityAfterIdle = 0.6f;
    [Range(0f, 1f)] public float StopAtObstacleChance = 0.3f;
    [Range(0f, 1f)] public float FlipAtObstacleChance = 0.7f;
    public float ObstacleDecisionCooldown = 0.2f;
    public float NextObstacleDecisionTime;

    [Header("최소 상태 유지")]
    public float MinStateDuration = 0.2f;

    [Header("공격 거리")]
    public float AttackEnter = 1.2f;
    public float AttackExit = 1.6f;

    [Header("피격 관련")]
    public float InvinsibleEndTime;
    public bool targetKnown;
    public float FlipCooldown = 1.0f;
}
public abstract class StateBase : IStateBase
{
    protected NonPlayableCharacter npc;
    protected readonly Blackboard bb;
    protected readonly AbilityContext _ctx;
    protected StateBase(NonPlayableCharacter npc, Blackboard bb)
    {
        this.npc = npc;
        this.bb = bb;
        this._ctx = new AbilityContext { npc = npc, bb = bb };
    }
    public abstract string Name { get; }
    /// <summary>
    /// 상태 진입 로직
    /// </summary>
    public abstract void Enter();
    /// <summary>
    /// 상태 종료 로직
    /// </summary>
    public abstract void Exit();
    /// <summary>
    /// 상태 업데이트 로직
    /// </summary>
    public abstract void Update();
    private float _nextThinkTime;
    private const float ThinkInterval = 0.12f;
    public bool TryExecuteAbilityOnce(out IAbility best)
    {
        best = null;
        //Debug.Log($"{_ctx.npc.IsAbilityRunning} and {_ctx.bb.TimeNow} < {_nextThinkTime} = {_ctx.bb.TimeNow < _nextThinkTime}");
        if (_ctx.npc.IsAbilityRunning) return true;           // 이미 실행 중이면 상태 로직 스킵
        if (_ctx.bb.TimeNow < _nextThinkTime) return false;   // 스로틀

        Debug.Log($"Ability selection thinking...");
        var pick = AbilitySelector.PickBest(_ctx, npc.Abilities, out IAbility bestOne); // 내부에서 CanExecute 포함
        Debug.Log($"picked: {(pick != null ? pick.Id : "null")} / bestOne: {(bestOne != null ? bestOne.Id : "null")}");
        best = bestOne;
        if (pick == null)
        {
            if (best != null)
            {
                _ctx.npc.blackboard.AttackEnter = best.OptimalDistanceRange.x;
                _ctx.npc.blackboard.AttackExit = best.OptimalDistanceRange.y;
            }
            return false;
        }
        else
        {
            _ctx.npc.blackboard.AttackEnter = pick.OptimalDistanceRange.x;
            _ctx.npc.blackboard.AttackExit = pick.OptimalDistanceRange.y;
        }

        // Debug.Log($"{pick.Id} selected to execute");
            pick.Execute(_ctx);            // ★ Ability가 루트/애니/쿨다운/종료 처리
        _nextThinkTime = _ctx.bb.TimeNow + ThinkInterval;
        return true;                  // 이 틱 상태 로직은 스킵
    }
    public void SetMinDuration()
    {
        bb.MinStateEndTime = bb.TimeNow + UnityEngine.Random.value * 0.3f + bb.MinStateDuration;
    }
}