using System;
using UnityEngine;

public sealed class BlinkStrike : IAbility
{
    public string Id => "BlinkStrike";
    public float Cooldown => _cfg.cooldown;
    public float NextReadyTime { get; set; }
    private readonly BlinkStrikeConfig _cfg;
    public BlinkStrike(BlinkStrikeConfig cfg) { _cfg = cfg; }
    public Vector2 OptimalDistanceRange => new(_cfg.enter, _cfg.exit);
    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        return ctx.Dist <= _cfg.enter && ctx.Dist >= _cfg.exit;
    }
    private Action blinkAction;
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
        npc.SetRooted(false);
        blinkAction = () =>
        {
            Vector2 pos = ctx.target.position;
            float dir = Mathf.Sign(ctx.npc.blackboard.DistToTarget);
            pos.x += dir * _cfg.exit;
            ctx.self.position = pos;
        };
        // 히트 프레임 처리(순간 전진 등)
        npc.OnAbility += blinkAction;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            if (blinkAction != null)
                npc.OnAbility -= blinkAction;
            npc.OnAbilityEnd = null;
        };
        // 애니메이션 딱 한 번 재생
        Debug.Log(_cfg.animIndex);
        npc.AnimPlayAttack(_cfg.animIndex);
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(ctx.Dist - _cfg.enter) / (_cfg.exit - _cfg.enter + 0.0001f));
        float sFace = ctx.IsFacingTarget == !_cfg.backAttack ? 1f : 0.2f;

        return _cfg.WDist * sDist + _cfg.WFace * sFace;
    }
}