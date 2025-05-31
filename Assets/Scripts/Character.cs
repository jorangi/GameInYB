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
    [SerializeField] protected Transform frontRay;
    [SerializeField] protected Transform foot;
    [SerializeField] protected float gravityScale = 3.5f; // 중력 적용 세기

    [SerializeField] protected Collider2D col;
    [SerializeField] protected Rigidbody2D rigid;
    [SerializeField] protected SpriteRenderer sprite;

    protected Vector2 moveVec = Vector2.zero;
    protected Vector2 perp;
    protected float cAngle;
    public bool isSlope;
    protected bool isGround;
    protected bool isJump;
    protected float rayDistance = 0.5f;
    [SerializeField] protected float chkGroundRad = 0.1f;
    [SerializeField] protected int jumpCnt;
    RaycastHit2D hit, fronthit;
    private Vector2 slopeTop;
    protected float maxAngle = 60.0f;
    protected LAYER landingLayer = LAYER.FLOOR; //7 is Floor, 9 is Platform
    protected virtual void Update()
    {
        isGround = GroundCheck();

        Vector2 rayDir = (Vector2.down + new Vector2(moveVec.x, 0) * 0.25f).normalized;
        hit = Physics2D.Raycast(foot.position, rayDir, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(frontRay.position, sprite.flipX ? Vector2.right : Vector2.left, 0.2f, LayerMask.GetMask("Floor", "Platform"));
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
    protected void Movement()
    {
        if (moveVec.x == 0.0f) return;
        if (moveVec.x > 0)
        {
            sprite.flipX = true;
            frontRay.localPosition = new(Mathf.Abs(frontRay.localPosition.x), frontRay.localPosition.y);
        }
        else if (moveVec.x < 0)
        {
            sprite.flipX = false;
            frontRay.localPosition = new(Mathf.Abs(frontRay.localPosition.x) * -1, frontRay.localPosition.y);
        }
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
}
