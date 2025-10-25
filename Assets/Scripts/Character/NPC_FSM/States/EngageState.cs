using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EngageState : StateBase
{
    private readonly List<IAbility> _abilities;
    private readonly AbilityContext _ctx;
    private float _nextThinkTime;
    private const float ThinkInterval = 0.12f; // 의사결정 주기

    public EngageState(NonPlayableCharacter npc, Blackboard bb, List<IAbility> abilities) : base(npc, bb)
    {
        _abilities = abilities;
        _ctx = new AbilityContext { npc = npc, bb = bb };
    }

    public override string Name => "Engage";

    public override void Enter()
    {
        npc.SetRooted(false);
        npc.AnimSetMoving(false);
        _nextThinkTime = 0f;
    }

    public override void Update()
    {
        if (bb.target == null || !bb.CanSeeTarget)
        {
            npc.RequestState<ChaseState>();
            return;
        }

        if (bb.TimeNow < _nextThinkTime) return;

        var pick = AbilitySelector.PickBest(_ctx, _abilities);
        if (pick == null)
        {
            npc.RequestState<ChaseState>();
            return;
        }

        // 실행 (쿨다운은 Ability 내부에서 끝날 때 설정)
        pick.Execute(_ctx);

        // 다음 선택까지 짧게 대기
        _nextThinkTime = bb.TimeNow + ThinkInterval;
        // 여기서 다른 상태로 전환하지 않음. (AttackState 불필요)
    }

    public override void Exit() { }
}

