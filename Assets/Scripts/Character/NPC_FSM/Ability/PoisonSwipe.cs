using UnityEngine;

public sealed class PoisonSwipe : IAbility
{
    public string Id => "PoisonSwipe";
    public float Cooldown => 2.0f;
    public float NextReadyTime { get; set; }

    public float Enter = 1.1f;   // 선호거리
    public float Exit  = 1.6f;   // 히스테리시스
    public System.Func<bool> IsTargetPoisoned; // 외부 주입(타깃 상태 확인)

    public bool CanExecute(AbilityContext c)
    {
        if (c.TimeNow < NextReadyTime) return false;
        if (!c.npc.blackboard.CanSeeTarget) return false;
        if (c.Dist > Exit) return false;
        // 독 부여 목적: 이미 독이면 굳이 안 쓰도록 게이트할 수도 있음(원하면 제거)
        return IsTargetPoisoned == null || !IsTargetPoisoned();
    }

    public float Score(AbilityContext c)
    {
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(c.Dist - Enter) / (Exit - Enter + 1e-4f));
        float sFace = c.IsFacingTarget ? 1f : 0.2f;
        // 타깃이 독이 아닐수록 가점
        float sNeed = (IsTargetPoisoned != null && IsTargetPoisoned()) ? 0.0f : 0.3f;
        return 0.7f * sDist + 0.2f * sFace + sNeed;
    }

    public void Execute(AbilityContext c)
    {
        c.npc.SetRooted(true);
        c.npc.AnimTrigger("PoisonSwipe"); // 애니 이벤트에서 독 상태 적용/히트 처리
        NextReadyTime = c.TimeNow + Cooldown;
    }
}
public sealed class PoisonConditionalCombo : IAbility
{
    public string Id => "PoisonConditional"; public float Cooldown => 2.3f; public float NextReadyTime { get; set; }
    public float Enter = 1.1f, Exit = 1.6f;
    public System.Func<bool> IsTargetPoisoned; // 외부 주입
    public bool CanExecute(AbilityContext c) { return c.TimeNow >= NextReadyTime && c.Dist <= Exit; }
    public float Score(AbilityContext c)
    { // 거리+상태 가중
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(c.Dist - Enter) / (Exit - Enter + 1e-4f));
        bool poisoned = IsTargetPoisoned != null && IsTargetPoisoned();
        return poisoned ? 0.5f * sDist : 0.9f * sDist + 0.2f; // 독X일 때 가점(독 부여 우선)
    }
    public void Execute(AbilityContext c)
    {
        bool poisoned = IsTargetPoisoned != null && IsTargetPoisoned();
        c.npc.SetRooted(true);
        c.npc.AnimTrigger(poisoned ? "FastCombo2" : "PoisonSwipe");
        NextReadyTime = c.TimeNow + Cooldown;
    }
}