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
        return ctx.Dist <= OptimalDistanceRange.x && ctx.Dist >= OptimalDistanceRange.y;
    }
    private Action blinkAction;
    private Func<GameObject> hitByBody;
    GameObject obj;
    int hitByBodyCount = 0;
    public void Execute(AbilityContext ctx)
    {
        var npc = ctx.npc;
        npc.RunningAbilityCfg = _cfg;
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
        hitByBody = () =>
        {
            hitByBodyCount++;
            if (hitByBodyCount > 1 && obj != null)
            {
                obj.GetComponent<NPC__AttackHitBox>().timer = 10.0f;
                return null;
            }
            obj = npc.CheckHit();
            obj.GetComponent<NPC__AttackHitBox>().timer = 10.0f;
            obj.transform.parent = npc.transform;
            obj.transform.localScale = new Vector3(_cfg.AttackSize, _cfg.AttackSize, 1.0f);
            obj.transform.localPosition = Vector3.zero;
            return obj;
        };
        // 히트 프레임 처리(순간 전진 등)
        npc.OnAbility += blinkAction;
        npc.OnHitFrame += hitByBody;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            npc.RunningAbilityCfg = null;
            if (blinkAction != null)
                npc.OnAbility -= blinkAction;
            if (hitByBody != null)
            {
                GameObject.Destroy(obj);
                npc.OnHitFrame -= hitByBody;
            }
            npc.OnAbilityEnd = null;
            
            npc.SetDesiredMove(0f);
            npc.SetRooted(true);
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