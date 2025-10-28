using System;
using UnityEngine;

public sealed class SpinAttack : IAbility {
    
    public string Id => "SpinAttack";
    public float Cooldown => _cfg.cooldown;
    public float NextReadyTime { get; set; }
    private readonly SpinAttackConfig _cfg;
    public SpinAttack(SpinAttackConfig cfg) { _cfg = cfg; }
    public Vector2 OptimalDistanceRange => new(_cfg.enter, _cfg.exit);
    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        return ctx.Dist <= _cfg.exit;
    }
    private Func<GameObject> meleeHitLogic;
    public void Execute(AbilityContext ctx)
    {
        var npc = ctx.npc;
        npc.blackboard.AttackEnter = _cfg.enter;
        npc.blackboard.AttackEnter = _cfg.exit;
        NextReadyTime = ctx.TimeNow + Cooldown;
        // 실행 시작 상태
        npc.IsAbilityRunning = true;
        npc.AnimSetMoving(false);
        npc.SetDesiredMove(0f);
        npc.SetRooted(_cfg.rootDuring);
        meleeHitLogic = () =>
        {
            if (_cfg.rootDuring)
            {
                float dir = Mathf.Sign(npc.blackboard.target.position.x - npc.blackboard.self.position.x);
                npc.SetDesiredMove(dir);
                if (npc.blackboard.IsPrecipiceAhead || npc.blackboard.IsWallAhead)
                    npc.SetDesiredMove(-dir);
            }
            return null;
        };
        // 히트 프레임 처리(순간 전진 등)
        npc.OnHitFrame += meleeHitLogic;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            if (meleeHitLogic != null)
            {
                npc.OnHitFrame -= meleeHitLogic;
            }
            npc.OnAbilityEnd = null;
        };
        // 애니메이션 딱 한 번 재생
        npc.AnimPlayAttack(_cfg.animIndex);
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(ctx.Dist - _cfg.enter) / (_cfg.exit - _cfg.enter + 0.0001f));
        float sFace = ctx.IsFacingTarget == !_cfg.backAttack ? 1f : 0.2f;
        return _cfg.WDist * sDist + _cfg.WFace * sFace;
    }
}