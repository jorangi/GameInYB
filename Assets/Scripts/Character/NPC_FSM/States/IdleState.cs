using UnityEngine;

public class IdleState : StateBase
{
    public override string Name => "Idle";
    public IdleState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }
    public override void Enter()
    {
        Debug.Log("대기 시작");
        npc.AnimSetMoving(false);
        npc.SetDesiredMove(0f);
        float idleDur = Random.Range(blackboard.IdleDurationRange.x, blackboard.IdleDurationRange.y);
        blackboard.IdleEndTime = blackboard.TimeNow + idleDur;
        npc.SetMinStateLock(blackboard.MinStateDuration);
    }
    public override void Exit()
    {
        Debug.Log("대기 종료");
    }
    public override void Update()
    {
        // 타깃이 범위 내 + 볼 수 있음
        if (blackboard.CanSeeTarget && blackboard.DistToTarget <= blackboard.DetectEnter && !InMinLock())
        {
            npc.RequestState<ChaseState>();
            return;
        }
        Debug.Log($"timenow: {blackboard.TimeNow} >= idletime: {blackboard.IdleEndTime} && !InMinLock: {!InMinLock()}");
        if (blackboard.TimeNow >= blackboard.IdleEndTime && !InMinLock())
        {
            bool goWander = Random.value < blackboard.WanderProbabilityAfterIdle;
            if (goWander)
            {
                npc.RequestState<WanderState>();
                return;
            }
            else
            {
                float idleDur = Random.Range(blackboard.IdleDurationRange.x, blackboard.IdleDurationRange.y);
                blackboard.IdleEndTime = blackboard.TimeNow + idleDur;
                npc.SetMinStateLock(blackboard.MinStateDuration);
            }
        }
        npc.SetDesiredMove(0);
        npc.SetRooted(true);
    }
    private bool InMinLock() => blackboard.TimeNow < blackboard.MinStateEndTime;
}
