using UnityEngine;

public class DieState : StateBase
{
    private float _despawnAt;
    public DieState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard) { }
    public override string Name => "Die";

    public override void Enter()
    {
        npc.SetDesiredMove(0f);
        npc.SetRooted(true);
        npc.AnimSetMoving(false);
        npc.AnimTriggerDeath();
        npc.SetMinStateLock(bb.MinStateDuration);
    }
    public override void Exit()
    {
    }
    public override void Update()
    {
        if (npc.IsDieAnimFinished())
        {
            GameObject.Destroy(npc.gameObject);
            return;
        }
    }
}
