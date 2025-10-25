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
        bb.ComboStep = 1;
        bb.IsInCombo = bb.MaxCombo > 1;
        npc.AnimSetMoving(false);
        npc.AnimTriggerAttack();
    }

    public override void Exit()
    {
        bb.ComboStep = 0;
    }
    public override void Update()
    {
        if (bb.DistToTarget < bb.AttackExit && bb.DistToTarget > bb.AttackEnter) // 공격 범위 내
        {
            npc.sprite.flipX = npc.FacingSign > 0;
            if (bb.AttackCooldownEnd < bb.TimeNow) //공격 쿨타임 만료(공격 가능)
            {
                if (bb.ComboStep == 1)
                {
                    bb.AttackCooldownEnd = bb.TimeNow + tempCooldown; //임시로 쿨타임 2초
                    npc.AnimTriggerAttack();
                    npc.SetDesiredMove(0f);
                    npc.AnimSetMoving(false);
                    npc.SetRooted(true);
                }
                if (bb.IsInCombo && bb.ComboStep < bb.MaxCombo && bb.TimeNow < bb.ComboBuffer)
                {
                }
                bb.AttackCooldownEnd = bb.TimeNow + tempCooldown; //임시로 쿨타임 2초
                npc.AnimTriggerAttack();
                npc.SetDesiredMove(0f);
                npc.AnimSetMoving(false);
                npc.SetRooted(true);
            }
        }
        else if (!npc.AnimGetTriggerAttack())
        {
            if (bb.DistToTarget < bb.DetectExit)
            {
                npc.RequestState<ChaseState>();
                return;
            }
            else
            {
                float r = Random.value;
                if (r < bb.WanderProbabilityAfterIdle)
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
