using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayableCharacter : Character
{
    private InputSystem_Actions inputAction;
    private const float hpBarSpd = 5.0f;

    [SerializeField]
    private SlicedFilledImage hpBar;
    [SerializeField]
    private SlicedFilledImage hpBarSec;

    [SerializeField]
    private TextMeshProUGUI hpVal;
    
    [SerializeField]
    protected Transform arm;
   [SerializeField]
    private float jumpPower = 8.0f;
    public int jumpCnt;
    private bool isHealing;
    private Coroutine hpSmooth;
    private float hp;
    public float Hp{
        get=>hp;
        set{
            isHealing = Mathf.FloorToInt(value) > Mathf.FloorToInt(hp);

            hp = value;
            hpVal.text = $"{Mathf.FloorToInt(value)} / {Mathf.FloorToInt(maxHp)}";
            
            //HpBar fills out smoothly
            if(hpSmooth != null) 
                StopCoroutine(hpSmooth);
            if(isHealing){
                hpBar.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(maxHp);
                hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
            }
            else{
                hpBarSec.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(maxHp);
                hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
            }
        }
    }
    private float maxHp;
    public float MaxHp{
        get => maxHp;
        set{
            maxHp = value;
        }
    }
    void Awake(){
        inputAction = new();
        jumpCnt = 2;
        MaxHp = 100.0f;
        Hp = 100.0f;
        chkGroundRad = 0.1f;
    }
    void OnEnable()
    {
        inputAction.Enable();
        inputAction.Player.Move.performed += OnMovement;
        inputAction.Player.Jump.performed += OnJump;
    }
    void Update()
    {
        Vector3 dir = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float armAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        arm.rotation = Quaternion.Euler(0, 0, armAngle + 90);

        if(Input.GetKeyDown(KeyCode.R)){
            Hp = Random.Range(0, MaxHp);
        }
    }
    IEnumerator HpBarFillsSmooth(SlicedFilledImage bar){
        yield return new WaitForSeconds(0.3f);
        while(Mathf.Abs(bar.fillAmount - (float)Mathf.FloorToInt(Hp) / Mathf.FloorToInt(maxHp)) > 0.01f){
            bar.fillAmount = Mathf.Lerp(bar.fillAmount, (float)Mathf.FloorToInt(Hp) / Mathf.FloorToInt(maxHp), Time.deltaTime * hpBarSpd);
            yield return null;
        }
        bar.fillAmount = (float)Mathf.FloorToInt(Hp) / Mathf.FloorToInt(maxHp);
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
        }
        Debug.Log(rigid.linearVelocityX);
    }
    void OnCollisionEnter2D(Collision2D col){
        foreach(ContactPoint2D c in col.contacts){
            if(c.collider.gameObject.layer.Equals(7)){
            //if(c.collider.gameObject.layer.Equals(7) && Mathf.RoundToInt(rigid.linearVelocityY) == 0){
                jumpCnt = 2;
            }
        }
    }
    public void OnMovement(InputAction.CallbackContext context){
        moveVec = context.ReadValue<Vector2>();
        if(moveVec.x > 0) sprite.flipX = true; 
        else if(moveVec.x < 0) sprite.flipX = false;
        rigid.constraints = moveVec.x == 0.0f ? RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation : RigidbodyConstraints2D.FreezeRotation;
        Movement();
    }
    public void OnJump(InputAction.CallbackContext context){
        if(context.performed && jumpCnt > 0){
            Debug.Log(rigid.linearVelocityY + jumpPower + " / " + jumpPower);
            rigid.linearVelocityY = Mathf.Min(rigid.linearVelocityY + jumpPower, jumpPower);
            //rigid.linearVelocityY += 100;
            jumpCnt--;
        }
    }
    private bool SlopeCheck(){
        return !Mathf.Approximately(cAngle, 0.0f);
    }
    private bool GroundCheck(){
        return Physics2D.OverlapCircle(foot.position, chkGroundRad, LayerMask.GetMask("Floor"));
    }
}
