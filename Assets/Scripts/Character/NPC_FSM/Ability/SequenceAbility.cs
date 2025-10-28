using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public sealed class SequenceAbility : IAbility
{
    public string Id => "Sequence";
    public float Cooldown => _steps.Count > 0 ? _steps[^1].Cooldown : 0f;
    public float NextReadyTime { get; set; }

    private readonly List<IAbility> _steps;
    private readonly float _linkMaxWait = 0.25f;
    public Vector2 OptimalDistanceRange => _steps.Count > 0 ? _steps[0].OptimalDistanceRange : new Vector2(0f, 0f);
    public SequenceAbility(List<IAbility> steps, float linkMaxWait = 0.25f)
    {
        _steps = steps ?? new List<IAbility>();
        _linkMaxWait = Mathf.Max(0f, linkMaxWait);
    }
    public bool CanExecute(AbilityContext ctx)
    {
        if (_steps == null || _steps.Count == 0) return false;
        if (ctx.TimeNow < NextReadyTime) return false;
        return _steps[0].CanExecute(ctx);
    }
    public float Score(AbilityContext ctx)
    {
        if (_steps == null || _steps.Count == 0) return 0f;
        return _steps[0].Score(ctx); // use first step's utility
    }
    public void Execute(AbilityContext ctx)
    {
        // 파괴 시 취소되도록
        var ct = ctx.npc.GetCancellationTokenOnDestroy();
        RunSequenceAsync(ctx, ct).Forget();
    }
    private async UniTask RunSequenceAsync(AbilityContext ctx, CancellationToken ct)
    {
        NextReadyTime = float.PositiveInfinity;

        for (int i = 0; i < _steps.Count; i++)
        {
            var step = _steps[i];

            if (i == 0)
            {
                if (!step.CanExecute(ctx)) break;
                step.Execute(ctx);
            }
            else
            {
                float deadline = Time.time + _linkMaxWait;
                while (!step.CanExecute(ctx) && Time.time < deadline && !ct.IsCancellationRequested)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, ct);
                }
                if (!step.CanExecute(ctx) || ct.IsCancellationRequested) break;
                step.Execute(ctx);
            }
            await UniTask.WaitUntil(() => !ctx.npc.IsAbilityRunning, PlayerLoopTiming.Update, ct);
            // 한 프레임 양보
            await UniTask.Yield(PlayerLoopTiming.Update, ct);
        }

        var npc = ctx.npc;
        // 종료 처리(한 번만 쿨다운 적용)
        npc.OnAbilityEnd = null;
        npc.SetRooted(false);
        npc.SetDesiredMove(0f);
        npc.IsAbilityRunning = false;
        npc.RunningAbilityCfg = null;
        NextReadyTime = ctx.bb.TimeNow + Cooldown; // ✅ 여기서만 쿨다운
    }
}