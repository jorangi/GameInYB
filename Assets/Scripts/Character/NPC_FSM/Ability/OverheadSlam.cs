using UnityEngine;

public sealed class OverheadSlam : IAbility
{
    public string Id => "OverheadSlam";
    public float Cooldown => 3.0f;
    public float NextReadyTime { get; set; }
    public float EnterRange  = 0.8f;
    public float ExitRange   = 1.6f;
    public float WDist = 0.6f, WFace = 0.3f;
    public Vector2 OptimalDistanceRange => throw new System.NotImplementedException();

    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        if (ctx.Dist <= ExitRange) return false;
        return true;
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Clamp01(1f -Mathf.Abs((ctx.Dist - EnterRange) / (ExitRange - EnterRange + 0.0001f))); // 가우시안 예시
        float sFace = ctx.IsFacingTarget ? 1f : 0.1f;
        return WDist * sDist + WFace * sFace;
    }
    public void Execute(AbilityContext ctx)
    {
        ctx.npc.SetRooted(true);
        ctx.npc.AnimTrigger("OvewrheadSlam");
        NextReadyTime = ctx.TimeNow + Cooldown;
        // 애니 이벤트에서 실제 속도 가속/히트판정 시작 → 종료 후 Root 해제
    }
}
