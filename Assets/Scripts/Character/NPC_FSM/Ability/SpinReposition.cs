using UnityEngine;

public sealed class SpinReposition : IAbility {
    public string Id=>"SpinReposition"; public float Cooldown=>3.5f; public float NextReadyTime{get;set;}
    public float TooClose=0.6f;
    public Vector2 OptimalDistanceRange => throw new System.NotImplementedException();
    public bool CanExecute(AbilityContext c){ return c.TimeNow>=NextReadyTime && c.npc.blackboard.CanSeeTarget; }
    public float Score(AbilityContext c){
        bool behind = (c.target.position.x - c.self.position.x > 0)!=(c.npc.FacingSign>0);
        float s = 0f;
        if(behind) s += 0.7f;
        if(c.Dist<TooClose) s += 0.5f;
        return s;
    }
    public void Execute(AbilityContext c){ c.npc.SetRooted(true); c.npc.AnimTrigger("Spin"); NextReadyTime=c.TimeNow+Cooldown; }
}