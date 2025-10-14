using UnityEngine;

public class DieState : StateBase
{
    public DieState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard){}

    public override string Name => "Die";

    public override void Enter()
    {
        npc.SetDesiredMove(0f);
        npc.SetRooted(true);
        npc.AnimSetMoving(false);
        npc.AnimTriggerDeath();
    }
    public override void Exit()
    {
    }
    public override void Update()
    {
    }
}
