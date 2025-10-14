using UnityEngine;

public class AttackState : StateBase
{
    public AttackState(NonPlayableCharacter npc, Blackboard blackboard) : base(npc, blackboard){}

    public override string Name => "Attack";
    private const float tempCooldown = 2.0f;
    public override void Enter()
    {
        npc.SetDesiredMove(0f);
        npc.SetRooted(true);

        blackboard.AttackCooldownEnd = blackboard.TimeNow + tempCooldown; //임시로 쿨타임 2초
        npc.AnimSetMoving(false);
        npc.AnimTriggerAttack();
    }

    public override void Exit()
    {
        
    }
    public override void Update()
    {
        if (blackboard.DistToTarget < blackboard.AttackExit && blackboard.DistToTarget > blackboard.AttackEnter)
        {
            npc.sprite.flipX = npc.FacingSign > 0;
            if (blackboard.AttackCooldownEnd < blackboard.TimeNow)
            {
                blackboard.AttackCooldownEnd = blackboard.TimeNow + tempCooldown; //임시로 쿨타임 2초
                npc.AnimTriggerAttack();
                npc.SetDesiredMove(0f);
                npc.AnimSetMoving(false);
                npc.SetRooted(true);
            }
        }
        else if (!npc.AnimGetTriggerAttack())
        {
            if (blackboard.DistToTarget < blackboard.DetectExit)
            {
                npc.RequestState<ChaseState>();
                return;
            }
            else
            {
                float r = Random.value;
                if (r < blackboard.WanderProbabilityAfterIdle)
                {
                    npc.RequestState<WanderState>();
                    return;
                }
                npc.RequestState<IdleState>();
                return;
            }
        }
    }
}
