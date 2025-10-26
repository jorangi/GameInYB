using System;
using UnityEngine;

public sealed class MeleeAttack : IAbility
{
    public string Id => "MeleeAttack";
    public bool rootDuring = true;
    public float movePulse = 0.0f;
    public float Cooldown => 2.0f;
    public float NextReadyTime { get; set; }
    private readonly MeleeAttackConfig _cfg;
    public MeleeAttack(MeleeAttackConfig cfg){ _cfg = cfg; }
    public float EnterRange = 1.1f;
    public float ExitRange = 1.6f;
    public float WDist = 0.6f, WFace = 0.3f;
    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        return ctx.Dist <= ExitRange;
    }
    public void Execute(AbilityContext ctx)
    {
        ctx.npc.SetDesiredMove(0);
        ctx.npc.SetRooted(true);
        ctx.npc.AnimPlayAttack(_cfg.animIndex);
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(ctx.Dist - EnterRange) / (ExitRange - EnterRange + 0.0001f));
        float sFace = ctx.IsFacingTarget ? 1f : 0.2f;

        return WDist * sDist + WFace * sFace;
    }
}