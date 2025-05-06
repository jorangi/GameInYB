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
    }
    void OnEnable()
    {
        inputAction.Enable();
        inputAction.Player.Move.performed += OnMovement;
        inputAction.Player.Move.canceled += OnMovement;
        inputAction.Player.Jump.performed += OnJump;
    }
    protected override void Update()
    {
        base.Update();
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
    protected override void FixedUpdate(){
        base.FixedUpdate();
    }
    public void OnMovement(InputAction.CallbackContext context)
    {
        moveVec = context.ReadValue<Vector2>();
        if (moveVec.x > 0){
          sprite.flipX = true;
          frontRay.localPosition = new(Mathf.Abs(frontRay.localPosition.x), frontRay.localPosition.y);
        } 
        else if (moveVec.x < 0){
            sprite.flipX = false;
            frontRay.localPosition = new(Mathf.Abs(frontRay.localPosition.x) * -1, frontRay.localPosition.y);
        } 

        rigid.constraints = moveVec.x == 0.0f ?
            RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation :
            RigidbodyConstraints2D.FreezeRotation;
    }

   public void OnJump(InputAction.CallbackContext context){
        if ((jumpCnt > 0 || isGround) && context.performed){
            rigid.gravityScale = 1.5f;
            isJump = true;
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, jumpPower);
            jumpCnt--;
        }
    }
}
