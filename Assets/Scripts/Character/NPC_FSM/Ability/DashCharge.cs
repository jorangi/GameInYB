using System;
using UnityEngine;

public struct DashParams
{
    public float dir;
    public float speed;
    public bool stopOnWall;
}
public sealed class DashCharge : IAbility
{
    public string Id => "DashCharge";
    public float Cooldown => _cfg.cooldown;
    public float NextReadyTime { get; set; }
    private readonly DashChargeConfig _cfg;
    public DashCharge(DashChargeConfig cfg) { _cfg = cfg; }
    public Vector2 OptimalDistanceRange => new(_cfg.enter, _cfg.exit);
    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        return ctx.Dist <= OptimalDistanceRange.x && ctx.Dist >= OptimalDistanceRange.y;
    }
    private Action dashLogic;
    private Func<GameObject> hitByBody;
    GameObject obj;
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
        hitByBody = () =>
        {
            if (_cfg.Hit.HasFlag(HitPosition.Body))
            {
                obj = npc.CheckHit();
                obj.GetComponent<NPC__AttackHitBox>().timer = 10.0f;
                obj.transform.parent = npc.transform;
                obj.transform.localScale = new Vector3(_cfg.AttackSize, _cfg.AttackSize, 1.0f);
                obj.transform.localPosition = Vector3.zero;
            }
            return obj;
        };
        dashLogic = () =>
        {
            npc.StartDash(new DashParams
            {
                dir = Mathf.Sign(ctx.target.position.x - ctx.self.position.x),
                speed = _cfg.speed,
                stopOnWall = false
            });
        };
        // 히트 프레임 처리(순간 전진 등)
        npc.OnHitFrame += hitByBody;
        npc.OnAbility += dashLogic;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            npc.RunningAbilityCfg = null;
            if (dashLogic != null)
                npc.OnAbility -= dashLogic;
            if (hitByBody != null)
            {
                GameObject.Destroy(obj);
                npc.OnHitFrame -= hitByBody;
            }
            npc.SetDesiredMove(0f);
            npc.SetRooted(true);
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