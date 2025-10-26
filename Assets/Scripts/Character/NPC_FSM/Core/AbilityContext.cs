using UnityEngine;
using System.Linq;
using System.Collections.Generic;
public sealed class AbilityContext
{
    public NonPlayableCharacter npc;
    public Blackboard bb;
    public Transform self => bb.self;
    public Transform target => bb.target;

    public float Dist => bb.DistToTarget;
    public float TimeNow => bb.TimeNow;
    public bool IsFacingTarget => (npc.FacingSign > 0) == (target.position.x - self.position.x > 0);
}
public interface IAbility
{
    string Id { get; }
    bool CanExecute(AbilityContext ctx);
    float Score(AbilityContext ctx);
    void Execute(AbilityContext ctx);
    float Cooldown { get; }
    float NextReadyTime { get; set; }
}
public static class AbilitySelector
{
    public static IAbility PickBest(AbilityContext ctx, IEnumerable<IAbility> abilities)
    {
        IAbility best = null;
        float bestscore = float.NegativeInfinity;

        foreach (var a in abilities)
        {
            if (!a.CanExecute(ctx)) continue;
            float s = a.Score(ctx);
            if (s > bestscore)
            {
                best = a;
                bestscore = s;
            }
        }
        return best;
    }
}