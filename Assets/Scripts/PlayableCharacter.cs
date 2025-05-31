using System.Collections;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayableCharacterData : CharacterData
{
    public PlayableCharacterData(string name) : base(name) { }
    protected int jCnt; // jump count
    public int JumpCnt
    {
        get => jCnt;
        set
        {
            if (value < 0)
                jCnt = 0;
            else
                jCnt = value;
        }
    }
    protected float jumpPower; // jump power
    public float JumpPower
    {
        get => jumpPower;
        set
        {
            if (value < 0)
                jumpPower = 0;
            else
                jumpPower = value;
        }
    }
    protected float cri;// critical chance
    public float Cri
    {
        get => cri;
        set
        {
            if (value < 0.0f)
                cri = 0.0f;
            else if (value > 1.0f)
                cri = 1.0f;
            else
                cri = value;
        }
    }
    protected float criDmg; // critical damage
    public float CriDmg
    {
        get => criDmg;
        set
        {
            if (value < 1.0f)
                criDmg = 1.0f;
            else
                criDmg = value;
        }
    }
    public override CharacterData SPD(float spd) => base.SPD(spd);
    public override CharacterData MAXHP(int maxhp) => base.MAXHP(maxhp);
    public override CharacterData H_P(int hp) => base.H_P(hp);
    public override CharacterData ATK(float atk) => base.ATK(atk);
    public override PlayableCharacterData ATS(float ats) => base.ATK(ats);
    public PlayableCharacterData JCNT(int jCnt)
    {
        this.JumpCnt = jCnt;
        return this;
    }
    public PlayableCharacterData JPOW(float jumpPower)
    {
        this.JumpPower = jumpPower;
        return this;
    }
    public PlayableCharacterData CRI(float cri)
    {
        this.Cri = cri;
        return this;
    }
    public PlayableCharacterData CRIDMG(float criDmg)
    {
        this.CriDmg = criDmg;
        return this;
    }
    public override string ToString()
    {
        return $"{base.ToString()}\nJump Count : {JumpCnt}\nJump Power : {JumpPower}\nCritical Chance : {Cri}\nCritical Damage : {CriDmg}";
    }
}
public class PlayableCharacter : Character
{
    protected new PlayableCharacterData data;
    public int d;
    public static PlayableCharacter Inst { get; set; }
    private Image messagePortrait, imageOnMessage;
    private TextMeshProUGUI unitName, message;
    public GameObject messageObj;
    private RectTransform messageBox;
    private InputSystem_Actions inputAction;
    private const float hpBarSpd = 5.0f;
    private Camera cam;
    [SerializeField]
    private SlicedFilledImage hpBar;
    [SerializeField]
    private SlicedFilledImage hpBarSec;

    [SerializeField]
    private TextMeshProUGUI hpVal;

    [SerializeField]
    protected Transform arm;
    private SpriteRenderer weaponSprite;
    private Weapon weaponScript;
    private bool isDropdown;
    private bool isHealing;
    private Coroutine hpSmooth;
    protected override void Awake()
    {
        if (PlayableCharacter.Inst != null && PlayableCharacter.Inst != this)
        {
            Destroy(PlayableCharacter.Inst);
            return;
        }
        PlayableCharacter.Inst = this;
        DontDestroyOnLoad(gameObject);

        data = new PlayableCharacterData("Player")
            .JCNT(2)
            .JPOW(10.0f)
            .CRI(0.1f)
            .CRIDMG(1.5f)
            .SPD(10.0f);

        inputAction = new();
        jumpCnt = 2;
        weaponSprite = arm.GetChild(0).GetComponent<SpriteRenderer>();
        weaponScript = weaponSprite.GetComponent<Weapon>();
        SetupMessageBox();
        cam = Camera.main;
    }
    void OnEnable()
    {
        inputAction.Enable();
        inputAction.Player.Move.performed += OnMovement;
        inputAction.Player.Move.canceled += OnMovement;
        inputAction.Player.Jump.performed += OnJump;
        inputAction.Player.Attack.performed += OnAttack;
        inputAction.Player.Dropdown.performed += OnDropdown;
    }
    private void OnDropdown(InputAction.CallbackContext context)
    {
        if (landingLayer == LAYER.PLATFORM)
        {
            col.isTrigger = true;
            isDropdown = true;
        }
    }
    private void OnAttack(InputAction.CallbackContext context)
    {
        Attack();
    }
    protected override void Attack()
    {
        base.Attack();
        weaponScript.StartSwing();
    }
    protected override void Update()
    {
        base.Update();
        Vector3 dir = transform.position - Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float armAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (!weaponScript.isSwing)
        {
            if (armAngle + 90 <= 180 && armAngle + 90 >= 0)
                arm.rotation = Quaternion.Euler(0, 180, -(armAngle + 90));
            else
                arm.rotation = Quaternion.Euler(0, 0, armAngle + 90);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            data.HP = Random.Range(0, data.MaxHP);
        }
    }
    IEnumerator HpBarFillsSmooth(SlicedFilledImage bar)
    {
        yield return new WaitForSeconds(0.3f);
        while (Mathf.Abs(bar.fillAmount - (float)Mathf.FloorToInt(data.HP) / Mathf.FloorToInt(data.MaxHP)) > 0.01f)
        {
            bar.fillAmount = Mathf.Lerp(bar.fillAmount, (float)Mathf.FloorToInt(data.HP) / Mathf.FloorToInt(data.MaxHP), Time.deltaTime * hpBarSpd);
            yield return null;
        }
        bar.fillAmount = (float)Mathf.FloorToInt(data.HP) / Mathf.FloorToInt(data.MaxHP);
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        Vector3 pos = transform.position;
        cam.transform.position = Vector3.Lerp(cam.transform.position, new(Mathf.Clamp(pos.x, 0, 31), Mathf.Clamp(pos.y, 0, 18), -10), Time.fixedDeltaTime);
    }
    public void OnMovement(InputAction.CallbackContext context)
    {
        moveVec = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context)
    {
        if ((jumpCnt > 0 || isGround) && context.performed)
        {
            rigid.gravityScale = 1.5f;
            isJump = true;
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, data.JumpPower);
            jumpCnt--;
        }
    }
    private void SetupMessageBox()
    {
        messagePortrait = messageObj.transform.Find("portraitBox").Find("portrait").GetComponent<Image>();
        unitName = messageObj.transform.Find("portraitBox").Find("unitNameBox").Find("unitName").GetComponent<TextMeshProUGUI>();
        message = messageObj.transform.Find("messageBox").Find("message").GetComponent<TextMeshProUGUI>();
        messageBox = message.transform.parent.GetComponent<RectTransform>();
        imageOnMessage = messageObj.transform.Find("imageBox").Find("image").GetComponent<Image>();
    }
    public void ShowMessage(string[] info)
    {
        messageObj.SetActive(true);
        if (string.IsNullOrEmpty(info[0]))
        {
            messagePortrait.transform.parent.gameObject.SetActive(false);
            messageBox.offsetMin = new(50.0f, messageBox.offsetMin.y);
        }
        else
        {
            messageBox.offsetMin = new(318f, messageBox.offsetMin.y);
            messagePortrait.transform.parent.gameObject.SetActive(true);
            messagePortrait.sprite = null;
            unitName.text = info[1];
            message.text = info[2];
        }
        if (string.IsNullOrEmpty(info[3]))
        {
            imageOnMessage.transform.parent.gameObject.SetActive(false);
        }
        else
        {
            imageOnMessage.transform.parent.gameObject.SetActive(true);
            imageOnMessage.sprite = null;
        }
    }
    public void ShowMessage()
    {
        messageObj.SetActive(false);
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isDropdown && rigid.linearVelocityY <= 0.0f)
        {
            Landing((LAYER)collision.gameObject.layer);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.layer == (int)LAYER.PLATFORM && isDropdown)
        {
            isDropdown = false;
        }
    }
    public void SetHP(int value)
    {
        isHealing = Mathf.FloorToInt(value) > Mathf.FloorToInt(data.HP);

        data.HP = value;
        hpVal.text = $"{Mathf.FloorToInt(value)} / {Mathf.FloorToInt(data.MaxHP)}";
        //HpBar fills out smoothly
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        if (isHealing)
        {
            hpBar.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
        }
        else
        {
            hpBarSec.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
        }
    }
}