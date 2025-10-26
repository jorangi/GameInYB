using UnityEngine;

public sealed class Enrage : IAbility
{
    public string Id => "Enrage";
    public float Cooldown => 4.0f;
    public float NextReadyTime { get; set; }
    public float PreferRange = 3.5f; // 중거리 선호
    public float EnterRange  = 3.0f;
    public float ExitRange   = 5.0f;
    public float RequiredRunway = 3.0f; // 돌진할 직선 여유
    public float WDist = 0.5f, WFace = 0.2f;

    public bool CanExecute(AbilityContext ctx)
    {
        if (ctx.TimeNow < NextReadyTime) return false;
        if (!ctx.npc.blackboard.CanSeeTarget) return false;
        if (ctx.Dist < EnterRange || ctx.Dist > ExitRange) return false;
        if (ctx.npc.blackboard.IsWallAhead || ctx.npc.blackboard.IsPrecipiceAhead) return false; // 앞에 벽/낭떠러지 등
        return true;
    }
    public float Score(AbilityContext ctx)
    {
        float sDist = Mathf.Exp(-Mathf.Pow((ctx.Dist - PreferRange) / 0.8f, 2f)); // 가우시안 예시
        float sFace = ctx.IsFacingTarget ? 1f : 0.1f;
        return WDist * sDist + WFace * sFace;
    }
    public void Execute(AbilityContext ctx)
    {
        ctx.npc.SetRooted(true);
        ctx.npc.AnimTrigger("DashWindup");
        // 애니 이벤트에서 실제 속도 가속/히트판정 시작 → 종료 후 Root 해제
    }
}
