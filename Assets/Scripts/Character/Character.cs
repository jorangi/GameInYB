using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public enum StatType
{
    HP,
    ATK,
    ATS,
    DEF,
    CRI,
    CRID,
    SPD,
    JCNT,
    JMP
}
public enum StatOp
{
    ADD, // 합연산 ex) hp + 10%
    MUL // 곱연산 ex)hp
}
public readonly struct StatModifier
{
    public readonly StatType Stat;
    public readonly float Value;
    public readonly StatOp Op;
    public readonly int Priority;
    public readonly object Source;
    public StatModifier(StatType stat, float value, StatOp op, object source, int priority = 0)
    {
        Stat = stat; Value = value; Op = op; Source = source; Priority = priority;
    }
}
public interface IStatModifierProvider
{
    IEnumerable<StatModifier> GetStatModifiers();
}
public class CharacterStats
{
    //기본 스탯
    private readonly Dictionary<StatType, float> baseStats = new();
    //추가 스탯(출처)
    private readonly Dictionary<IStatModifierProvider, int> providers = new();
    //캐시된 최종 스탯, 변경사항이 있을 때만 갱신(더티 플래그)
    private readonly Dictionary<StatType, float> finalCache = new();
    private bool dirty = true;
    //UI갱신 등 필요시 호출하는 이벤트
    public event Action OnRecalculated;
    public void SetBase(StatType stat, float value)
    {
        baseStats[stat] = value;
        dirty = true;
    }
    public float GetBase(StatType stat) => baseStats.TryGetValue(stat, out var value) ? value : 0f;
    public float GetFinal(StatType stat)
    {
        if (dirty) Recalculate();
        return finalCache.TryGetValue(stat, out var value) ? value : 0f;
    }
    public void AddProvider(IStatModifierProvider provider, int count = 1)
    {
        if (count <= 0) return;
        providers.TryGetValue(provider, out var cur);
        if (!providers.ContainsKey(provider)) providers.Add(provider, cur + count);
        else providers[provider] = cur + count;
        dirty = true;
    }
    public void RemoveProvider(IStatModifierProvider provider, int count = 1)
    {
        bool t = providers.TryGetValue(provider, out var cur);
        if (!t) return;
        int next = cur - count;
        if(next > 0)providers[provider] = next;
        else providers.Remove(provider);
        dirty = true;
    }
    public IReadOnlyDictionary<StatType, float> GetAllFinal()
    {
        if (dirty) Recalculate();
        return finalCache;
    }
    private void Recalculate()
    {
        finalCache.Clear();
        foreach (var kv in baseStats) finalCache[kv.Key] = kv.Value;

        var addBuckets = new Dictionary<StatType, float>();
        var mulBuckets = new Dictionary<(StatType stat, int priority), float>();

        foreach (var kv in providers)
        {
            var prov = kv.Key;
            int count = kv.Value;
            foreach (var m in prov.GetStatModifiers())
            {
                if (m.Op == StatOp.ADD) addBuckets[m.Stat] = addBuckets.TryGetValue(m.Stat, out var a) ? a + (m.Value * count) : (m.Value * count);
                else
                {
                    var key = (m.Stat, m.Priority);
                    mulBuckets[key] = mulBuckets.TryGetValue(key, out var a) ? a * a : a;
                }
            }
        }
        foreach (StatType stat in Enum.GetValues(typeof(StatType)))
        {
            float baseVal = finalCache.TryGetValue(stat, out var bv) ? bv : 0f;
            float sumAdd = addBuckets.TryGetValue(stat, out var a) ? a : 0f;

            float mulFactor = 1f;
            foreach (var kv in mulBuckets.Where(kv => kv.Key.stat == stat).OrderBy(kv => kv.Key.priority))
                mulFactor *= kv.Value;

            finalCache[stat] = (baseVal + sumAdd) * mulFactor;
        }

        dirty = false;
        OnRecalculated?.Invoke();
    }
}
public interface IStatProvider
{
    //스탯 반환
    CharacterStats GetStats();
}
public class CharacterData : IStatProvider
{
    private CharacterData() { }
    protected readonly CharacterStats stats = new();
    protected string unitName;
    public string UnitName
    {
        get => unitName;
        set => unitName = value;
    }
    public CharacterData(string name)
    {
        unitName = name;
        stats.SetBase(StatType.HP, 100.0f);
        stats.SetBase(StatType.ATK, 10.0f);
        stats.SetBase(StatType.ATS, 0.8f);
        stats.SetBase(StatType.DEF, 0.0f);
        stats.SetBase(StatType.SPD, 3.0f);
        SetInvicibleTime();
        SetHitStunTime();
    }
    public float Spd => stats.GetFinal(StatType.SPD); // movement speed
    public int MaxHP => stats.GetFinal(StatType.HP) is float f ? (int)f : 0;
    protected int _HP; // now Health
    public int HP
    {
        get => _HP;
        set
        {
            if (value < 0)
                _HP = 0;
            else if (value > stats.GetFinal(StatType.HP))
                _HP = (int)stats.GetFinal(StatType.HP);
            else
                _HP = value;
        }
    }
    public float Atk => stats.GetFinal(StatType.ATK); // attack power
    public float Ats => stats.GetFinal(StatType.ATS); // attack speed
    public float Def => stats.GetFinal(StatType.DEF); // defense power
    protected float invincibleTime; // invincibleTime
    public float InvincibleTime
    {
        get => invincibleTime;
        set
        {
            if (value < 0.0f)
                invincibleTime = 0.0f;
            else
                invincibleTime = value;
        }
    }
    public virtual CharacterData SetInvicibleTime(float invicibleTime = 0.0f)
    {
        this.InvincibleTime = invicibleTime;
        return this;
    }
    protected float hitStunTime; // hitStunTime
    public float HitStunTime
    {
        get => hitStunTime;
        set
        {
            if (value < 0.0f)
                hitStunTime = 0.0f;
            else
                hitStunTime = value;
        }
    }
    public virtual CharacterData SetHitStunTime(float hitStunTime = 0.5f)
    {
        this.HitStunTime = hitStunTime;
        return this;
    }
    public override string ToString() => $"Name: {unitName}, Speed: {Spd}, MaxHP: {MaxHP}, HP: {_HP}, Atk: {Atk}, Ats: {Ats}, Def: {Def}";
    public virtual CharacterStats GetStats() => stats;
}
[DisallowMultipleComponent]
public class Character : ParentObject
{
    #region fields
    protected CharacterData data;
    protected enum LAYER
    {
        FLOOR = 7,
        PLATFORM = 9
    };
    public Transform frontRay;
    public Transform foot;
    public float gravityScale = 3.5f; // 중력 적용 세기

    public Collider2D col;
    public Rigidbody2D rigid;


    protected Vector2 moveVec = Vector2.zero;
    protected Vector2 perp;
    protected float cAngle;
    public bool isSlope;
    protected bool isGround;
    protected bool isJump;
    protected float rayDistance = 0.5f;
    protected float chkGroundRad = 0.1f;
    protected int jumpCnt;
    RaycastHit2D hit, fronthit;
    private Vector2 slopeTop;
    protected float maxAngle = 60.0f;
    protected LAYER landingLayer = LAYER.FLOOR; //7 is Floor, 9 is Platform
    private float invincibleTimer = 0.0f;
    protected float InvincibleTimer
    {
        get => invincibleTimer;
        set
        {
            if (value < 0.0f)
            {
                invincibleTimer = 0.0f;
                hitBox.enabled = true;
            }
            else
            {
                invincibleTimer = value;
                hitBox.enabled = false;
            }
        }
    }
    protected float HitStunTimer;
    protected Coroutine hitCoroutine;
    public Collider2D hitBox;
    [SerializeField] protected Vector3 savedPos;
    protected RaycastHit2D isPrecipice;
    protected float desiredMoveX;
    protected bool isRooted;
    public float FacingSign;
    #endregion
    protected virtual void Update()
    {
        if (invincibleTimer > 0.0f)
            invincibleTimer -= Time.deltaTime;
        else
            hitBox.enabled = true;

        isGround = GroundCheck();
        if (this is PlayableCharacter)
        {
            isPrecipice = Physics2D.Raycast(frontRay.position, new Vector2(1, -1).normalized,
            1, LayerMask.GetMask("Floor", "Platform"));
        }
        else
        {
            isPrecipice = Physics2D.Raycast(frontRay.position, FacingSign < 0 ? new Vector2(-1, -2.5f).normalized : new Vector2(1, -2.5f).normalized, 1, LayerMask.GetMask("Floor", "Platform"));
        }



        if (isPrecipice && isGround) savedPos = transform.position;

        Vector2 rayDir = (Vector2.down + new Vector2(desiredMoveX, 0) * 0.25f).normalized;
        hit = Physics2D.Raycast(foot.position, rayDir, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(
            frontRay.position,
            desiredMoveX > 0 ? Vector2.right : Vector2.left, 0.2f,
            LayerMask.GetMask("Floor", "Platform")
        );
        if (!Mathf.Approximately(desiredMoveX, 0f))
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        else
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        if (fronthit)
        {

            isSlope = SlopeCheck(fronthit);
            slopeTop = fronthit.point;
        }
        else
        {
            if (hit)
            {
                isSlope = SlopeCheck(hit);
            }
            else
                isSlope = false;

            if (!isJump && Mathf.Abs(slopeTop.y - foot.position.y) < 0.1f && cAngle > 15 && cAngle < 60)
            {
                rigid.gravityScale = gravityScale;
            }
            else
            {
                rigid.gravityScale = 1.5f;
            }
        }
    }
    protected virtual void Awake()
    {
        data = new("Default");
    }
    protected virtual void FixedUpdate()
    {
        Movement();
    }
    protected virtual void Attack()
    {
    }
    protected virtual void Movement()
    {
        if (isRooted || Mathf.Approximately(desiredMoveX, 0f))
        {
            rigid.linearVelocity = new(0, rigid.linearVelocityY);
            return;
        }
        frontRay.localPosition = new(
            Mathf.Abs(frontRay.localPosition.x) * (desiredMoveX > 0 ? 1 : -1),
            frontRay.localPosition.y
        );
        if (isGround && isSlope && !isJump)
        {
            rigid.linearVelocity = Mathf.Abs(desiredMoveX) * data.Spd * perp;
        }
        else if (!isSlope)
        {
            rigid.linearVelocity = new Vector2(desiredMoveX * data.Spd, rigid.linearVelocityY);
        }
    }
    /// <summary>
    /// 유닛 착지 메소드, 점프 횟수, 점프 여부, Collision의 Trigger, 착지 레이어를 설정
    /// </summary>
    /// <param name="layer"></param> <summary>
    /// 
    /// </summary>
    /// <param name="layer"></param>
    protected virtual void Landing(LAYER layer)
    {
        col.isTrigger = false;
        landingLayer = layer;
    }
    /// <summary>
    /// [경사로 체크 메소드] 정면에 레이캐스트를 발사한 결과인 hit를 매개변수로 받아 노멀벡터와 Vector2.up을 비교하여 그 각이 경사로 인정값 내라면 그에 수직(경사면과 일치하는)인 벡터를 반환
    /// </summary>
    /// <param name="hit"></param>
    /// <returns></returns>
    private bool SlopeCheck(RaycastHit2D hit)
    {
        if (hit)
        {
            cAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (cAngle > maxAngle) return false;
            perp = Vector2.Perpendicular(hit.normal).normalized;

            if (Vector2.Dot(perp, new Vector2(desiredMoveX, 0)) < 0)
                perp = -perp;
        }
        bool slopeTemp = cAngle != 0.0f;
        return slopeTemp;
    }
    private bool GroundCheck()
    {
        return Physics2D.OverlapCircle(foot.position, chkGroundRad, LayerMask.GetMask("Floor", "Platform"));
    }
    public virtual void TakeDamage(float damage)
    {
        hitCoroutine ??= StartCoroutine(Hit());
    }
    void OnCollisionStay2D(Collision2D col)
    {
        if (!this.CompareTag("Player")) return;
        RaycastHit2D h = Physics2D.Raycast(foot.position, Vector2.down, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        if (!h) return;
        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.collider.gameObject.layer.Equals((int)LAYER.FLOOR))
            {
                if (rigid.linearVelocityY <= 0.0f) Landing(LAYER.FLOOR);
            }
            else if (c.collider.gameObject.layer.Equals((int)LAYER.PLATFORM))
            {
                if (rigid.linearVelocityY <= 0.0f) Landing(LAYER.PLATFORM);
            }
        }
    }
    public float timeMul = 10.0f;
    protected virtual IEnumerator Hit()
    {
        if (hitBox.transform.childCount > 0 && hitBox.GetComponentInChildren<SpriteRenderer>() is SpriteRenderer hBSprite)
        {
            hBSprite.color = Color.red;
            while (InvincibleTimer > 0)
            {
                yield return null;
            }
        }
    }
    public void SetDesiredMove(float x) => desiredMoveX = x;
    public void SetRooted(bool rooted) => isRooted = rooted;
    
}