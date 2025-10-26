using UnityEngine;

public sealed class PushKnockback : IAbility {
    public string Id=>"PushKnockback"; public float Cooldown=>6f; public float NextReadyTime{get;set;}
    public float Enter=0.9f, Exit=1.5f;
    public bool CanExecute(AbilityContext c){ return c.TimeNow>=NextReadyTime && c.Dist<=Exit; }
    public float Score(AbilityContext c){
        float sDist = Mathf.Clamp01(1f - Mathf.Abs(c.Dist-Enter)/(Exit-Enter+1e-4f));
        return 0.8f*sDist; // 주기 기술이면 Selector에서 우선권도 가능
    }
    public void Execute(AbilityContext c){ c.npc.SetRooted(true); c.npc.AnimTrigger("Push"); NextReadyTime=c.TimeNow+Cooldown; }
}