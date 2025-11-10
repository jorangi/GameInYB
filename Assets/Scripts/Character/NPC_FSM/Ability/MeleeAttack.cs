using System;
using UnityEngine;

public sealed class MeleeAttack : IAbility
{
    public string Id => "MeleeAttack";
    public float Cooldown => _cfg.cooldown;
    public float NextReadyTime { get; set; }
    private readonly MeleeAttackConfig _cfg;
    public MeleeAttack(MeleeAttackConfig cfg) { _cfg = cfg; }
    public Vector2 OptimalDistanceRange => new(_cfg.enter, _cfg.exit);
    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        return ctx.Dist <= OptimalDistanceRange.x && ctx.Dist >= OptimalDistanceRange.y;
    }
    private Func<GameObject> meleeHitLogic;
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
        npc.SetRooted(_cfg.rootDuring);
        hitByBody = () =>
        {
            if (!_cfg.Hit.HasFlag(HitPosition.Body)) return null;
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
        
        meleeHitLogic = () =>
        {
            //제자리 공격일 경우 움직이지 않고 공격
            if (!_cfg.rootDuring)
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
        npc.OnHitFrame += hitByBody;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            npc.RunningAbilityCfg = null;
            if (meleeHitLogic != null)
            {
                npc.OnHitFrame -= meleeHitLogic;
            }
            if (hitByBody != null)
            {
                GameObject.Destroy(obj);
                npc.OnHitFrame -= hitByBody;
            }
            npc.SetDesiredMove(0f);
            npc.SetRooted(true);
            npc.specialSpd = 1.0f;
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