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
        return ctx.Dist <= OptimalDistanceRange.x && ctx.Dist >= OptimalDistanceRange.y;
    }
    private Action spinWithMove;
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
        spinWithMove = () =>
        {
            if (!_cfg.rootDuring)
            {
                float dir = Mathf.Sign(npc.blackboard.target.position.x - npc.blackboard.self.position.x);
                npc.SetDesiredMove(dir);
                if (npc.blackboard.IsPrecipiceAhead || npc.blackboard.IsWallAhead)
                    npc.SetDesiredMove(-dir);
            }
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
        npc.OnAbility += spinWithMove;
        npc.OnHitFrame += hitByBody;
        npc.OnAbilityEnd = () =>
        {
            // 실행 종료 상태
            npc.IsAbilityRunning = false;
            npc.RunningAbilityCfg = null;
            if (spinWithMove != null)
                npc.OnAbility -= spinWithMove;
            if (hitByBody != null)
            {
                GameObject.Destroy(obj);
                npc.OnHitFrame -= hitByBody;
            }
            npc.OnAbilityEnd = null;
            npc.specialSpd = 1.0f;
            npc.SetDesiredMove(0f);
            npc.SetRooted(true);
        };
        npc.AnimPlayAttack(_cfg.animIndex);
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(ctx.Dist - _cfg.enter) / (_cfg.exit - _cfg.enter + 0.0001f));
        float sFace = ctx.IsFacingTarget == !_cfg.backAttack ? 1f : 0.2f;
        return _cfg.WDist * sDist + _cfg.WFace * sFace;
    }
}