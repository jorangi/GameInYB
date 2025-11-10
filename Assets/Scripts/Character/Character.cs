using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static ApiManager_All;

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
public class Health
{
    private int _HP;
    public int HP => _HP;
    public Action OnHPChanged;
    public Action OnDied;
    public void ApplyHP(int value)
    {
        _HP = Mathf.Max(value, 0);
        OnHPChanged?.Invoke();
        if (_HP == 0)
            OnDied?.Invoke();
    }
}
public class CharacterStats
{
    //기본 스탯
    private readonly Dictionary<StatType, float> baseStats = new();
    //추가 스탯(출처)
    private readonly Dictionary<IStatModifierProvider, int> providers = new();
    //캐시된 최종 스탯, 변경사항이 있을 때만 갱신(더티 플래그)
    private Dictionary<StatType, float> finalCache = new();
    private bool dirty = true;
    //UI갱신 등 필요시 호출하는 이벤트
    public event Action OnRecalculated;
    public void SetBase(StatType stat, float value)
    {
        baseStats[stat] = value;
        dirty = true;
        Recalculate();
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
        if (next > 0) providers[provider] = next;
        else providers.Remove(provider);
        dirty = true;
    }
    public IReadOnlyDictionary<StatType, float> GetAllFinal()
    {
        if (dirty) Recalculate();
        return finalCache;
    }
    public void ForceRecalulate() => Recalculate();
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
                if (m.Op == StatOp.ADD)
                    addBuckets[m.Stat] = addBuckets.TryGetValue(m.Stat, out var a) ? a + (m.Value * count) : (m.Value * count);
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
    public Health health = new();
    protected CharacterStats stats = new();
    private string id;
    protected string unitName;
    public string UnitName
    {
        get => unitName;
        set => unitName = value;
    }
    public CharacterData(string id)
    {
        if (id != "Player")
        {
            this.id = id;
            Npc d = ServiceHub.Get<INPCRepository>().GetNPC(id);
            unitName = d.name[0];
            stats.SetBase(StatType.HP, d.hp);
            stats.SetBase(StatType.ATK, d.atk);
            stats.SetBase(StatType.DEF, d.def);
            stats.SetBase(StatType.SPD, d.spd);
        }
        else
        {
            unitName = "Player";
            stats.SetBase(StatType.HP, 100.0f);
            stats.SetBase(StatType.ATK, 10.0f);
            stats.SetBase(StatType.ATS, 0f);
            stats.SetBase(StatType.DEF, 0.0f);
            stats.SetBase(StatType.SPD, 3.0f);
        }
        SetInvicibleTime();
        SetHitStunTime();
    }
    public float Spd => stats.GetFinal(StatType.SPD); // movement speed
    public int MaxHP => stats.GetFinal(StatType.HP) is float f ? (int)f : 0;
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
    public override string ToString() => $"Name: {unitName}, Speed: {Spd}, MaxHP: {MaxHP}, HP: {health.HP}, Atk: {Atk}, Ats: {Ats}, Def: {Def}";
    public virtual CharacterStats GetStats() => stats;
}
[DisallowMultipleComponent]
public class Character : ParentObject
{
    #region fields
    [SerializeField] protected CharacterData data;
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
    [SerializeField]protected bool isGround;
    [SerializeField]protected bool isJump;
    protected float rayDistance = 0.2f;
    protected int jumpCnt;
    RaycastHit2D hit, fronthit;
    private Vector2 slopeTop;
    protected float maxAngle = 80.0f;
    protected LAYER landingLayer = LAYER.FLOOR; //7 is Floor, 9 is Platform
    private float invincibleTimer = 0.0f;
    protected float InvincibleTimer
    {
        get => invincibleTimer;
        set
        {
            if (this is NonPlayableCharacter) return;
            if (value < 0.0f)
            {
                invincibleTimer = 0.0f;
                hitBox.gameObject.SetActive(true);
            }
            else
            {
                invincibleTimer = value;
                hitBox.gameObject.SetActive(false);
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
    protected bool isKnockback;
    Vector2 rayDir;
    #endregion
    public async void Knockback(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        touchedGround = false;
        isKnockback = true;
        rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
        rigid.AddForce(force, mode);
        await UniTask.WaitUntil(() => touchedGround);
        isKnockback = false;
    }
    protected virtual void Update()
    {
        isGround = GroundCheck();
        if (this is PlayableCharacter)
        {
            isPrecipice = Physics2D.Raycast(frontRay.position, desiredMoveX > 0 ? new Vector2(1, -1).normalized : new Vector2(-1, -1).normalized,
            rayDistance, LayerMask.GetMask("Floor", "Platform"));
            Debug.DrawRay(frontRay.position, rayDir * rayDistance, Color.blue);
        }
        else
        {
            isPrecipice = Physics2D.Raycast(
                                        frontRay.position, FacingSign < 0 ? new Vector2(-1, -2.5f).normalized : new Vector2(1, -2.5f).normalized,
                                        1, LayerMask.GetMask("Floor", "Platform"));
        }

        if (isPrecipice && isGround) savedPos = transform.position;

        // rayDir = desiredMoveX > 0 ? new(1, -1) : desiredMoveX < 0 ? new(-1,-1) : rayDir;
        rayDir = Vector2.down;
        hit = Physics2D.Raycast(foot.position, rayDir, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(
            frontRay.position,
            desiredMoveX > 0 ? Vector2.right : Vector2.left, 0.2f,
            LayerMask.GetMask("Floor", "Platform")
        );
        Debug.DrawRay(frontRay.position, (desiredMoveX > 0 ? Vector2.right : Vector2.left)*0.2f, Color.green);
        if (!isKnockback)
        {
            if (!Mathf.Approximately(desiredMoveX, 0f))
                rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            else
                rigid.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
        }
        else
        {
            rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
            touchedGround = false;
        }
        if (fronthit.collider != null)
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

            if (!isJump && Mathf.Abs(slopeTop.y - foot.position.y) < 0.1f && cAngle > 15 && cAngle < 80)
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
        data ??= new("Default");
        data.health.OnHPChanged += OnHPChanged;
        data.health.OnDied += OnDied;
        OnDashEnd += EndDash;
    }
    protected virtual void OnDied(){}
    public bool isDash;
    private DashParams dashParams;
    
    public Action OnDash;
    public Action OnDashEnd;
    public Action OnAbility;
    public virtual void StartDash(in DashParams p)
    {
        dashParams = p;
        isDash = true;
        SetRooted(false);
        SetDesiredMove(dashParams.dir);
    }
    private void EndDash()
    {
        dashParams = default;
        SetDesiredMove(0f);
        SetRooted(true);
        isDash = false;
    }
    protected virtual void UpdateDash()
    {
        if (!isDash) return;
        if ((isPrecipice.collider == null || fronthit.collider == null) && dashParams.stopOnWall)
        {
            OnDashEnd?.Invoke();
            return;
        }
        frontRay.localPosition = new(
            Mathf.Abs(frontRay.localPosition.x) * (desiredMoveX > 0 ? 1 : -1),
            frontRay.localPosition.y
        );
        if (isGround && isSlope && !isJump)
        {
            rigid.linearVelocity = Mathf.Abs(desiredMoveX) * data.Spd * dashParams.speed * perp;
        }
        else if (!isSlope)
        {
            rigid.linearVelocity = new Vector2(desiredMoveX * data.Spd * dashParams.speed, rigid.linearVelocityY);
        }
        SetRooted(false);
    }
    protected virtual void OnHPChanged() { }
    protected virtual void FixedUpdate()
    {
        if (!isDash) Movement();
        else UpdateDash();
    }
    protected virtual void Attack()
    {
    }
    /// <summary>
    /// 유닛 이동 메소드
    /// </summary>
    public float specialSpd = 1.0f;
    protected virtual void Movement()
    {
        foot.transform.position = new Vector3(col.bounds.center.x, col.bounds.min.y, 0);
        frontRay.transform.position = new Vector3(frontRay.position.x, foot.transform.position.y, 0);
        
        if (isKnockback) return;
        if (isRooted || Mathf.Approximately(desiredMoveX, 0f))
        {
            rigid.linearVelocity = new(0, rigid.linearVelocityY);
            return;
        }
        frontRay.localPosition = new(
            Mathf.Abs(frontRay.localPosition.x) * (desiredMoveX > 0 ? 1 : -1),
            frontRay.localPosition.y
        );
        rigid.gravityScale = gravityScale;
        if (isGround && isSlope && !isJump)
        {
            rigid.gravityScale = 0;
            rigid.linearVelocity = Mathf.Abs(desiredMoveX) * data.Spd * specialSpd * perp;
        }
        else if (!isSlope)
        {
            rigid.linearVelocity = new Vector2(desiredMoveX * data.Spd * specialSpd, rigid.linearVelocityY);
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
        touchedGround = true;
        landingLayer = layer;
        // Physics2D.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Platform"), false);
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
            Debug.DrawRay(hit.point, perp, Color.white);
            if (Vector2.Dot(perp, new Vector2(desiredMoveX, 0)) < 0)
                perp = -perp;
        }
        bool slopeTemp = cAngle != 0.0f;
        return slopeTemp;
    }
    protected virtual bool GroundCheck()
    {
        Debug.DrawRay(foot.position, Vector2.down * rayDistance, Color.red);
        RaycastHit2D h = Physics2D.Raycast(foot.position, Vector2.down, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        if (h.collider != null)
        {
            Vector2 normal = h.normal;
            return normal.y < maxAngle;
        }
        touchedGround = false;
        return false;
    }
    public virtual void TakeDamage(float damage, bool isCritical = false, bool isFallingDmg = false)
    {
        hitCoroutine ??= StartCoroutine(Hit());
    }
    protected bool touchedGround = false;
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