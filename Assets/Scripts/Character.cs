using System.Collections;
using Sirenix.OdinInspector;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterData
{
    private CharacterData() { }
    protected string unitName;
    public string UnitName
    {
        get => unitName;
        set => unitName = value;
    }
    public CharacterData(string name)
    {
        unitName = name;
        SetSpd();
        SetMaxHP();
        SetHP();
        SetAtk();
        SetAts();
        SetDef();
        SetInvicibleTime();
        SetHitStunTime();
    }
    protected float spd; // movementSpeed
    public float Spd
    {
        get => spd;
        set
        {
            if (value < 0.0f)
                spd = 0.0f;
            else
                spd = value;
        }
    }
    public virtual CharacterData SetSpd(float spd = 5.0f)
    {
        this.Spd = spd;
        return this;
    }
    protected int maxHP; // max Health
    public int MaxHP
    {
        get => maxHP;
        set
        {
            if (value < 0)
                maxHP = 0;
            else
                maxHP = value;
        }
    }
    public virtual CharacterData SetMaxHP(int maxHP = 100)
    {
        this.MaxHP = maxHP;
        return this;
    }
    protected int _HP; // now Health
    public int HP
    {
        get => _HP;
        set
        {
            if (value < 0)
                _HP = 0;
            else if (value > maxHP)
                _HP = maxHP;
            else
                _HP = value;
        }
    }
    public virtual CharacterData SetHP(int hp = 100)
    {
        this.HP = hp;
        return this;
    }
    protected float atk; // attack power
    public float Atk
    {
        get => atk;
        set
        {
            if (value < 0.0f)
                atk = 0.0f;
            else
                atk = value;
        }
    }
    public virtual CharacterData SetAtk(float atk = 10)
    {
        this.Atk = atk;
        return this;
    }
    protected float ats; // attack speed
    public float Ats
    {
        get => ats;
        set
        {
            if (value < 0.0f)
                ats = 0.0f;
            else
                ats = value;
        }
    }
    public virtual CharacterData SetAts(float ats = 0.8f)
    {
        this.Ats = ats;
        return this;
    }
    protected float def; // defence
    public float Def
    {
        get => def;
        set
        {
            if (value < 0.0f)
                def = 0.0f;
            else
                def = value;
        }
    }
    public virtual CharacterData SetDef(float def = 0.0f)
    {
        this.Def = def;
        return this;
    }
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
    public override string ToString()
    {
        return $"Name: {unitName}, Speed: {spd}, MaxHP: {maxHP}, HP: {_HP}, Atk: {atk}, Ats: {ats}, Def: {def}";
    }
}
[DisallowMultipleComponent]
public class Character : ParentObject
{
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
    [ShowInInspector] protected Vector3 savedPos;
    protected bool moveDir; //f : left, t : right
    protected RaycastHit2D isPrecipice;
    protected virtual void Update()
    {
        if (invincibleTimer > 0.0f)
            invincibleTimer -= Time.deltaTime;
        else
            hitBox.enabled = true;

        isGround = GroundCheck();
        if (this is PlayableCharacter)
        {
            isPrecipice = Physics2D.Raycast(frontRay.position,
            frontRay.localPosition.x > 0 ?
                (moveDir ?
                    new Vector2(-1, -1).normalized :
                    new Vector2(1, -1).normalized) :
                (moveDir ?
                    new Vector2(-1, -1).normalized :
                    new Vector2(1, -1).normalized),
            1, LayerMask.GetMask("Floor", "Platform"));
            // Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + (
            //     frontRay.localPosition.x > 0 ?
            //         (moveDir ?
            //             new Vector2(-1, -1).normalized :
            //             new Vector2(1, -1).normalized) :
            //         (moveDir ?
            //             new Vector2(-1, -1).normalized :
            //             new Vector2(1, -1).normalized)),
            // Color.red);
        }
        else
        {
            isPrecipice = Physics2D.Raycast(frontRay.position, moveDir ? new Vector2(1, -1).normalized : new Vector2(-1, -1).normalized, 1, LayerMask.GetMask("Floor", "Platform"));
            // Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + (moveDir ? new Vector2(1, -1).normalized : new Vector2(-1, -1).normalized), Color.red);
        }



        if (isPrecipice && isGround) savedPos = transform.position;

        Vector2 rayDir = (Vector2.down + new Vector2(moveVec.x, 0) * 0.25f).normalized;
        hit = Physics2D.Raycast(foot.position, rayDir, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(frontRay.position, moveVec.x > 0 ? Vector2.right : Vector2.left, 0.2f, LayerMask.GetMask("Floor", "Platform"));
        if (moveVec.x != 0.0f)
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
        data = new CharacterData("Default");
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
        if (moveVec.x == 0.0f) return;
        frontRay.localPosition = new(Mathf.Abs(frontRay.localPosition.x) * (moveVec.x > 0 ? 1 : -1), frontRay.localPosition.y);
        if (isGround && isSlope && !isJump)
        {
            rigid.linearVelocity = Mathf.Abs(moveVec.x) * data.Spd * perp;
        }
        else if (!isSlope)
        {
            rigid.linearVelocity = new Vector2(moveVec.x * data.Spd, rigid.linearVelocityY);
        }
    }
    protected virtual void Landing(LAYER layer)
    {
        jumpCnt = 2;
        isJump = false;
        col.isTrigger = false;
        landingLayer = layer;
    }
    private bool SlopeCheck(RaycastHit2D hit)
    {
        if (hit)
        {
            cAngle = Vector2.Angle(hit.normal, Vector2.up);
            if (cAngle > maxAngle) return false;
            perp = Vector2.Perpendicular(hit.normal).normalized;

            if (Vector2.Dot(perp, new Vector2(moveVec.x, 0)) < 0)
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
}
