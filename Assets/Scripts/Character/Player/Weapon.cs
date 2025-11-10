using UnityEngine;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;
[System.Serializable]
public class ItemData
{
    [SerializeField] private string id;
    [HideInInspector] public string Id => id;
    [SerializeField] private string name;
    [HideInInspector] public string Name => name;
    [SerializeField] private string description;
    [HideInInspector] public string Description => description;
    [SerializeField] private Sprite icon;
    [HideInInspector] public Sprite Icon => icon;
}
[System.Serializable]
public static class SpriteTopUtil
{
    public static Vector3 GetTopWorldPosition(SpriteRenderer sr)
    {
        if (!sr || !sr.sprite) return sr ? sr.transform.position : Vector3.zero;

        float topFromPivotLocalY;

        if (sr.drawMode == SpriteDrawMode.Simple)
        {
            var sp = sr.sprite;
            float ppu = sp.pixelsPerUnit;
            topFromPivotLocalY = (sp.rect.height - sp.pivot.y) / ppu; // distance from pivot to top in local +Y
        }
        else
        {
            var sp = sr.sprite;
            float normPivotY = sp.rect.height > 0f ? (sp.pivot.y / sp.rect.height) : 0.5f;
            topFromPivotLocalY = sr.size.y * (1f - normPivotY); // sliced/tiled uses size
        }

        if (sr.flipY) topFromPivotLocalY = -topFromPivotLocalY;

        return sr.transform.TransformPoint(new Vector3(0f, topFromPivotLocalY, 0f));
    }

    public static Vector2 GetTopWorldPosition2D(SpriteRenderer sr)
    {
        var p = GetTopWorldPosition(sr);
        return (Vector2)p;
    }

    // 화면(월드 AABB) 기준 상단이 필요할 때
    public static Vector3 GetAabbTopWorldPosition(SpriteRenderer sr)
    {
        if (!sr || !sr.sprite) return sr ? sr.transform.position : Vector3.zero;
        var b = sr.bounds;
        return new Vector3((b.min.x + b.max.x) * 0.5f, b.max.y, sr.transform.position.z);
    }
}

[RequireComponent(typeof(Animator))]
public class Weapon : MonoBehaviour
{
    public List<int> attackQueue;
    public SpriteRenderer spriteRenderer;
    public GameObject attackHitBox;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    private CharacterStats stats;
    private IStatProvider provider;
    private IAddressablesService svc;
    public int attackIdx;
    private async void Awake()
    {
        await PlayableCharacter.ReadyAsync(this.GetCancellationTokenOnDestroy());
        provider = PlayableCharacter.Inst.Data;
        stats ??= provider.GetStats();
        stats.OnRecalculated += OnStatChanged;
        if (provider is null) Debug.LogError("[WeaponScript] provider에 stats할당 실패");
        anim = GetComponent<Animator>();
        svc = ServiceHub.Get<IAddressablesService>();
        hitManager = ServiceHub.Get<IHitManager>();
    }
    public void OnStatChanged()
    {
        SetAts(provider.GetStats().GetFinal(StatType.ATS));
    }
    public void SetAts(float ats) => anim.SetFloat("attackSpd", ats);
    private void Update()
    {
        targets.Clear();
    }
    public void StartAttack()
    {
        if (anim.GetBool("IsAttack") || spriteRenderer.sprite == null || attackQueue.Count == 0) return;
        anim.Play($"{attackQueue[attackIdx]}", 0);
        anim.SetBool("IsAttack", true);
        anim.SetInteger("AttackId", attackQueue[attackIdx]);
        attackIdx++;
        if (attackIdx == attackQueue.Count) attackIdx = 0;
    }
    public void AttackEnd()
    {
        anim.SetBool("IsAttack", false);
    }
    public void StopAttack()
    {
        attackIdx = 0;
        AttackEnd();
    }
    public List<NonPlayableCharacter> targets = new();
    /// <summary>
    /// 공격 순간 히트박스 생성
    /// </summary>
    IHitManager hitManager = null;
    public void SpawnEffect()
    {
        Debug.Log(anim.GetCurrentAnimatorClipInfo(0)[0].clip.name);
    }
    public void CheckHit()
    {
        //GameObject hitbox = Instantiate(attackHitBox);
        if (hitManager == null) return;
        HitBox hitbox = hitManager.GetHitBox(provider);
        var start = (Vector2)PlayableCharacter.Inst.transform.position;
        var dir = (Vector2)PlayableCharacter.Inst.arm.up;
        var reach = Mathf.Max(spriteRenderer.sprite.bounds.size.x, spriteRenderer.sprite.bounds.size.y, 1.0f);
        var end = start + dir * reach;
        hitbox.transform.position = end;
        float maxSize = Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        //hitbox.transform.localScale = new(maxSize, maxSize);
        hitbox.SetSize(maxSize, maxSize);
        if (anim.GetCurrentAnimatorStateInfo(0).IsName("21012"))
        {
            if (svc.TryGetPrefab("StabEffect", out var effect))
            {
                //Debug.Log($"{transform.position} + {new Vector3(spriteRenderer.bounds.size.y + transform.localPosition.y, 0) * transform.localScale.x} = {transform.position + new Vector3(spriteRenderer.bounds.size.y, 0) * transform.localScale.x}");
                GameObject e = Instantiate(effect, hitbox.transform.position,
                                    name == "MainWeapon" ?
                                    transform.parent.localScale.x == -1 ? Quaternion.Euler(0, 0, transform.eulerAngles.z + 90) : Quaternion.Euler(0, 0, transform.eulerAngles.z - 90) :
                                    transform.localScale.x == -1 ? Quaternion.Euler(0, 0, transform.eulerAngles.z + 90) : Quaternion.Euler(0, 0, transform.eulerAngles.z - 90));
                //var e = Instantiate(effect, transform.position + spriteRenderer.bounds.size /*SpriteTopUtil.GetTopWorldPosition2D(spriteRenderer)*/, transform.localScale.x == -1 ? Quaternion.Euler(0, 0, transform.eulerAngles.z + 80) : Quaternion.Euler(0, 0, transform.eulerAngles.z - 80)/*);
                e.transform.localScale = new(-Mathf.Sign(name == "MainWeapon" ? transform.parent.localScale.x : transform.localScale.x) * e.transform.localScale.x, e.transform.localScale.y);
                e.GetComponent<SpriteRenderer>().flipY = !spriteRenderer.flipX;
                AudioManager.Inst.PlaySFX("swing");
            }
        }
        else
        {
            if (svc.TryGetPrefab("Effect", out var effect))
            {
                var e = Instantiate(effect, SpriteTopUtil.GetTopWorldPosition2D(spriteRenderer)/*transform.Find("WeaponTip").position*/, transform.parent.localScale.x == -1 ? Quaternion.Euler(0, 0, transform.eulerAngles.z + 80) : Quaternion.Euler(0, 0, transform.eulerAngles.z - 80));
                e.transform.localScale = new(-Mathf.Sign(transform.parent.localScale.x) * e.transform.localScale.x, e.transform.localScale.y);
                e.transform.GetChild(0).GetComponent<SpriteRenderer>().flipY = !spriteRenderer.flipX;
                AudioManager.Inst.PlaySFX("swing");
            }
        }
    }
    public Action weaponSkill;
    public void OnSkill() => weaponSkill?.Invoke();
}
