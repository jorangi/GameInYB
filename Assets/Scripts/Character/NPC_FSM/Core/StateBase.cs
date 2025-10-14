using System;
using Unity.VisualScripting;
using UnityEngine;

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

    //Idle/Wander용 타이머
    public float IdleEndTime;
    public float WanderEndTime;

    //최소 상태 유지 시간 (움찔 방지)
    public float MinStateEndTime;

    //공격 쿨타임
    public float AttackCooldownEnd;


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
}
public abstract class StateBase : IStateBase
{
    protected NonPlayableCharacter npc;
    protected readonly Blackboard blackboard;
    protected StateBase(NonPlayableCharacter npc, Blackboard blackboard)
    {
        this.npc = npc;
        this.blackboard = blackboard;
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
}