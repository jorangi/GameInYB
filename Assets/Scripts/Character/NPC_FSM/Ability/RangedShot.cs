using UnityEngine;

public sealed class RangedShot : IAbility {
    public string Id=>"RangedShot"; public float Cooldown=>1.8f; public float NextReadyTime{get;set;}
    public float Enter=3f, Exit=7f, Prefer=5f;
    public bool CanExecute(AbilityContext c){ return c.TimeNow>=NextReadyTime && c.npc.blackboard.CanSeeTarget && c.Dist>=Enter && c.Dist<=Exit; }
    public float Score(AbilityContext c){
        float sDist = Mathf.Exp(-Mathf.Pow((c.Dist-Prefer)/1.2f,2f));
        float sFace = c.IsFacingTarget?1f:0.2f;
        return 0.7f*sDist + 0.3f*sFace;
    }
    public void Execute(AbilityContext c){ c.npc.SetRooted(false); c.npc.AnimTrigger("Shoot"); NextReadyTime=c.TimeNow+Cooldown; }
}