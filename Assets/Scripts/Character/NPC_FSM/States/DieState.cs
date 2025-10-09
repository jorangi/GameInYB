using UnityEngine;

public class DieState : StateBase
{
    public DieState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard){}

    public override string Name => "Die";

    public override void Enter()
    {
        throw new System.NotImplementedException();
    }

    public override void Exit()
    {
        throw new System.NotImplementedException();
    }

    public override void Update()
    {
        throw new System.NotImplementedException();
    }
}
