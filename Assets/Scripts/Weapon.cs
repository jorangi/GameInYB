using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine.Playables;
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
public class WeaponData : ItemData
{
    [SerializeField] private Sprite sprite;
    [HideInInspector] public Sprite Sprite => sprite;
    [SerializeField] private float atk;
    [HideInInspector] public float Atk => atk;
    [SerializeField] private float ats;
    [HideInInspector] public float Ats => ats;
    [SerializeField] private float cri;
    [HideInInspector] public float Cri => cri;
    [SerializeField] private float crid;
    [HideInInspector] public float Crid => crid;
}

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
            float heightUnits = sp.rect.height / ppu;
            float pivotUnitsY = sp.pivot.y / ppu;
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
        return new Vector2(p.x, p.y);
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
    public SpriteRenderer spriteRenderer;
    public GameObject attackHitBox;
    private const int MAX_SWING_COUNT = 2; // Maximum number of swings allowed
    public Animator anim;
    private CharacterStats stats;
    private IStatProvider provider;
    private IAddressablesService svc;
    private void Awake()
    {
        provider = PlayableCharacter.Inst.Data;
        stats ??= provider.GetStats();
        stats.OnRecalculated += OnStatChanged;
        if (provider is null) Debug.LogError("[WeaponScript] provider에 stats할당 실패");
        anim = GetComponent<Animator>();
        svc = ServiceHub.Get<IAddressablesService>();
    }
    public void OnStatChanged()
    {
        SetAts(provider.GetStats().GetFinal(StatType.ATS));
    }
    public void SetAts(float ats)
    {
        anim.SetFloat("attackSpd", ats);
    }
    private void Update()
    {
        targets.Clear();
    }
    public void StartSwing()
    {
        if (anim.GetBool("IsSwing") || spriteRenderer.sprite == null) return;
        anim.SetInteger("SwingCount", (anim.GetInteger("SwingCount") + 1) % MAX_SWING_COUNT);
        anim.SetBool("IsSwing", true);
    }
    public void SwingEnd()
    {
        anim.SetBool("IsSwing", false);
    }
    public void StopSwing()
    {
        anim.SetInteger("SwingCount", 0);
        SwingEnd();
    }
    public List<NonPlayableCharacter> targets = new();
    /// <summary>
    /// 공격 순간 히트박스 생성
    /// </summary>
    public void CheckHit()
    {
        GameObject hitbox = Instantiate(attackHitBox);
        hitbox.GetComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
        hitbox.GetComponent<AttackHitBox>().provider = provider;
        var start = (Vector2)PlayableCharacter.Inst.transform.position;
        var dir = (Vector2)PlayableCharacter.Inst.arm.up;
        var reach = 2.0f;
        var end = start + dir * reach;
        hitbox.transform.position = end;
        float maxSize = Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        hitbox.transform.localScale = new(maxSize, maxSize);

        if (svc.TryGetPrefab("Effect", out var effect))
        {
            var e = Instantiate(effect, SpriteTopUtil.GetTopWorldPosition2D(spriteRenderer)/*transform.Find("WeaponTip").position*/, transform.parent.localScale.x == -1 ? Quaternion.Euler(0, 0, transform.eulerAngles.z + 80) : Quaternion.Euler(0, 0, transform.eulerAngles.z - 80));
            e.transform.localScale = new(-Mathf.Sign(transform.parent.localScale.x) * e.transform.localScale.x, e.transform.localScale.y);
            e.transform.GetChild(0).GetComponent<SpriteRenderer>().flipY = !spriteRenderer.flipX;
        }
    }
}
