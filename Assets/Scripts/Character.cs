using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class Character : MonoBehaviour
{
    protected Rigidbody2D test;
    [SerializeField]
    protected Transform frontRay;
    [SerializeField]
    protected Transform foot;
    [SerializeField]
    protected float movementSpeed = 10.0f;

    [SerializeField]
    protected Collider2D col;

    [SerializeField]
    protected Rigidbody2D rigid;
    protected Vector2 moveVec = Vector2.zero;
    [SerializeField]
    protected SpriteRenderer sprite;
    protected Vector2 perp;
    protected float cAngle;
    protected bool isSlope;
    protected bool isGround;
    protected float rayDistance;
    protected float chkGroundRad;

    void Awake(){
    }
    void Update()
    {
    }
    void FixedUpdate(){
        isSlope = false;
        isGround = GroundCheck();
        RaycastHit2D hit = Physics2D.Raycast(foot.position, Vector2.down, rayDistance, LayerMask.GetMask("Floor"));
        Debug.DrawRay(foot.position, Vector2.down * rayDistance, Color.yellow);

        if(hit){
            Debug.DrawLine(hit.point, hit.point + hit.normal, Color.blue);
            cAngle = Vector2.Angle(hit.normal, Vector2.up);
            isSlope = SlopeCheck();
            perp = Vector2.Perpendicular(hit.normal).normalized;
            Debug.DrawLine(hit.point - perp * 0.5f, (hit.point - perp * 0.5f) + perp, Color.red);
            Debug.Log(perp);
        }
    }
    void OnCollisionEnter2D(Collision2D col){
        Debug.Log(col.gameObject.name);
        foreach(ContactPoint2D c in col.contacts){
            if(c.collider.gameObject.layer.Equals(7)){
            }
        }
    }
    public void OnMovement(InputAction.CallbackContext context){
        moveVec = context.ReadValue<Vector2>();
        if(moveVec.x > 0) sprite.flipX = true; 
        else if(moveVec.x < 0) sprite.flipX = false;
        rigid.constraints = moveVec.x == 0.0f ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.FreezeRotation;
    }
    void Movement(){
        Debug.Log($"Slope : {isSlope} / Ground : {isGround}");
        if(isSlope && isGround){
            rigid.linearVelocity = 1.2f * movementSpeed * new Vector2(perp.x * moveVec.x, perp.y * moveVec.x);
        }else if(!isSlope && isGround){
            rigid.linearVelocity = new(movementSpeed * moveVec.x, 0);
        }else if(!isSlope && !isGround){
            rigid.linearVelocity = new(movementSpeed * moveVec.x, rigid.linearVelocityY);
        }
        rigid.linearVelocity = new Vector2(movementSpeed * moveVec.x, rigid.linearVelocityY);
    }
    private bool SlopeCheck(){
        return !Mathf.Approximately(cAngle, 0.0f);
    }
    private bool GroundCheck(){
        return Physics2D.OverlapCircle(foot.position, chkGroundRad, LayerMask.GetMask("Floor"));
    }
}
