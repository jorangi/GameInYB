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
    [SerializeField] protected float chkGroundRad = 0.3f;
    [SerializeField]protected int jumpCnt;
    [SerializeField]protected int jumpPower;
    RaycastHit2D hit, fronthit;
    private Vector2 slopeTop;
    protected float maxAngle = 60.0f;
    protected LAYER landingLayer = LAYER.FLOOR; //7 is Floor, 9 is Platform
    protected virtual void Update(){
        isGround = GroundCheck();

        hit = Physics2D.Raycast(foot.position, Vector2.down, rayDistance, LayerMask.GetMask("Floor", "Platform"));
        fronthit = Physics2D.Raycast(frontRay.position, sprite.flipX ? Vector2.right : Vector2.left, 0.5f, LayerMask.GetMask("Floor", "Platform"));

        Debug.DrawLine(foot.position, (Vector2)foot.position + Vector2.down * rayDistance, Color.blue);
        Debug.DrawLine(frontRay.position, (Vector2)frontRay.position + (sprite.flipX ? Vector2.right : Vector2.left) * 0.5f, Color.red);

        if(fronthit) {
                isSlope = SlopeCheck(fronthit);
                slopeTop = fronthit.point;
        }
        else{
            if(hit){
                isSlope = SlopeCheck(hit);
            }else{
                isSlope = false;
            }

            if(!isJump && Mathf.Abs(slopeTop.y - foot.position.y) < 0.1f && cAngle > 15 && cAngle < 60){
                rigid.gravityScale = gravityScale;
            }else{
                rigid.gravityScale = 1.5f;
            }
        }
        if(slopechk){
            Debug.Log($"cAngle : {cAngle}, point.normal{contactPoint2D.relativeVelocity}");
        }
    }

    protected virtual void FixedUpdate()
    {  
        Movement();
        if(Input.GetKey(KeyCode.LeftShift)){
            Debug.DrawLine(fronthit.point, fronthit.point+fronthit.normal, Color.yellow);
        }
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
    private bool SlopeCheck(RaycastHit2D hit, int i)
    {
        if (hit)
        {
            cAngle = Vector2.Angle(hit.normal, Vector2.up);
            Debug.Log($"{cAngle}, {rigid.linearVelocityY}");  
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
    private bool slopechk;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        slopechk = true;
    }
    private void OnCollisionExit2D(Collision2D collision)
    {
        slopechk = false;
    }
    private ContactPoint2D contactPoint2D;
    private void OnCollisionStay2D(Collision2D col) {
        //Debug.Log("스테이");
        if(!isGround) return;
        //Debug.Log("땅임");
        foreach (ContactPoint2D c in col.contacts)
        {
            //Debug.Log($"{Vector2.Distance(foot.position, c.point) > 0.05f} / {!slopechk} / {c.otherCollider.name}");

            if(Vector2.Distance(foot.position, c.point) > 0.05f){
                contactPoint2D = c;
                return;
            }
            Debug.Log("여기서 스탑");
            rigid.constraints = moveVec.x == 0.0f ?
            RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation :
            RigidbodyConstraints2D.FreezeRotation;

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
    private void SlopeJump(ContactPoint2D c){

    }
}
