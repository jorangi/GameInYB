using UnityEngine;

public class IdleState : StateBase
{
    public override string Name => "Idle";
    public IdleState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard){}
    public override void Enter()
    {
        npc.AnimSetMoving(false);
        npc.SetDesiredMove(0f);
        float idleDur = Random.Range(blackboard.IdleDurationRange.x, blackboard.IdleDurationRange.y);
        blackboard.IdleEndTime = blackboard.TimeNow + idleDur;
        //npc.SetMi

    }
    public override void Exit()
    {

    }
    public override void Update()
    {
        npc.SetDesiredMove(0);
        npc.SetRooted(true);
    }
}
