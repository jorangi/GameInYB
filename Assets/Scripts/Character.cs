using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[DisallowMultipleComponent]
public class Character : ParentObject
{
    protected enum LAYER{
        FLOOR = 7,
        PLATFORM = 9
    };
    [SerializeField] protected Transform frontRay;
    [SerializeField] protected Transform foot;
    [SerializeField] protected float movementSpeed = 10.0f;
    [SerializeField] protected float gravityScale = 3.5f; // 중력 적용 세기

    [SerializeField] protected Collider2D col;
    [SerializeField] protected Rigidbody2D rigid;
    [SerializeField] protected SpriteRenderer sprite;

    protected Vector2 moveVec = Vector2.zero;
    protected Vector2 perp;
    protected float cAngle;
    protected bool isSlope;
    protected bool isGround;
    protected bool isJump;
    protected float rayDistance = 1.2f;
    [SerializeField] protected float chkGroundRad = 0.1f;
    [SerializeField]protected int jumpCnt;
    [SerializeField]protected int jumpPower;
    RaycastHit2D hit, fronthit;
    private Vector2 slopeTop;
    protected float maxAngle = 60.0f;
    protected LAYER landingLayer = LAYER.FLOOR; //7 is Floor, 9 is Platform
    protected virtual void Update(){
        isGround = GroundCheck();

        Vector2 rayDir = (Vector2.down + new Vector2(moveVec.x, 0) * 0.25f).normalized;
        hit = Physics2D.Raycast(foot.position, rayDir, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(frontRay.position, sprite.flipX ? Vector2.right : Vector2.left, 0.2f, LayerMask.GetMask("Floor", "Platform"));

        Debug.DrawLine(foot.position, (Vector2)foot.position + rayDir * rayDistance, Color.red);
        Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + (sprite.flipX ? Vector2.right : Vector2.left) * 0.2f, Color.red);

        if(fronthit) {
                isSlope = SlopeCheck(fronthit);
                slopeTop = fronthit.point;
        }
        else{
            if(hit){
                isSlope = SlopeCheck(hit);
            }else{
                rigid.constraints = RigidbodyConstraints2D.FreezeRotation;
                isSlope = false;
            }

            if(!isJump && Mathf.Abs(slopeTop.y - foot.position.y) < 0.1f && cAngle > 15 && cAngle < 60){
                rigid.gravityScale = gravityScale;
            }else{
                rigid.gravityScale = 1.5f;
            }
        }
    }

    protected virtual void FixedUpdate()
    {  
        Movement();
    }
    protected virtual void Attack(){
        
    }
    protected void Movement()
    {
        if (isGround && isSlope && !isJump)
        {
            rigid.linearVelocity = Mathf.Abs(moveVec.x) * movementSpeed * perp;
        }
        else if (!isSlope)
        {
            rigid.linearVelocity = new Vector2(moveVec.x * movementSpeed, rigid.linearVelocityY);
        }
    }
    protected virtual void Landing(LAYER layer){
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
            if(cAngle > maxAngle) return false;
            perp = Vector2.Perpendicular(hit.normal).normalized;

            if (Vector2.Dot(perp, new Vector2(moveVec.x, 0)) < 0)
                perp = -perp;
        }
        bool slopeTemp = !Mathf.Approximately(cAngle, 0.0f);
        return slopeTemp;
    }
    private bool GroundCheck()
    {
        return Physics2D.OverlapCircle(foot.position, chkGroundRad, LayerMask.GetMask("Floor", "Platform"));
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        foreach (ContactPoint2D c in col.contacts)
        {
            if (c.collider.gameObject.layer.Equals((int)LAYER.FLOOR))
            {
                if(rigid.linearVelocityY <= 0.0f) Landing(LAYER.FLOOR);
            }
            else if(c.collider.gameObject.layer.Equals((int)LAYER.PLATFORM))
            {
                if(rigid.linearVelocityY <= 0.0f) Landing(LAYER.PLATFORM);
            }
        }
    }
}
