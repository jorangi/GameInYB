using UnityEngine;

public sealed class HowlBuff : IAbility {
    public string Id=>"HowlBuff"; public float Cooldown=>8f; public float NextReadyTime{get;set;}
    public System.Func<int> NearbyPackCount; // 외부 주입
    public bool CanExecute(AbilityContext c){ return c.TimeNow>=NextReadyTime && NearbyPackCount!=null && NearbyPackCount()>0; }
    public float Score(AbilityContext c){
        int n = NearbyPackCount?.Invoke() ?? 0; // 스택 기반 가중
        return Mathf.Clamp01(n/3f) + 0.2f; // 동료가 많을수록 우선
    }
    public void Execute(AbilityContext c){ c.npc.SetRooted(true); c.npc.AnimTrigger("Howl"); NextReadyTime=c.TimeNow+Cooldown; }
}