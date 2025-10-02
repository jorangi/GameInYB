using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;

public struct ItemSlot
{
    public Item item;
    public int ea;

    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
    public static ItemSlot operator +(ItemSlot a, int ea)
    {
        a.ea += ea;
        return a;
    }
    public static ItemSlot operator -(ItemSlot a, int ea)
    {
        a.ea -= ea;
        return a;
    }
    public static bool operator ==(ItemSlot a, ItemSlot b)
    {
        return a.item.id == b.item.id && a.ea == b.ea;
    }
    public static bool operator !=(ItemSlot a, ItemSlot b)
    {
        return a.item.id == b.item.id && a.ea == b.ea;
    }
}
public enum EquipmentType {
    ARMOR,
    PANTS,
    HELMET,
    MAINWEAPON,
    SUBWEAPON
}
public class PlayableCharacterData : CharacterData
{
    public PlayableCharacterData(CharacterData data) : base(data.UnitName) { }
    public int JumpCnt => (int)stats.GetFinal(StatType.JCNT); // 최대 점프 횟수
    public float JumpPower => stats.GetFinal(StatType.JMP); // 점프력
    public float Cri => stats.GetFinal(StatType.CRI); // 크리티컬 확률
    public float CriDmg => stats.GetFinal(StatType.CRID); // 크리티컬 데미지 배율
    public PlayableCharacterData SetJCnt(int jCnt = 2) // 점프 횟수 설정 빌더
    {
        this.stats.SetBase(StatType.JCNT, jCnt);
        return this;
    }
    public PlayableCharacterData SetJPow(float jumpPower = 10.0f) // 점프력 설정 빌더
    {
        this.stats.SetBase(StatType.JMP, jumpPower);
        return this;
    }
    public PlayableCharacterData SetCri(float cri = 0.0f) // 크리티컬 확률 설정 빌더
    {
        this.stats.SetBase(StatType.CRI, cri);
        return this;
    }
    public PlayableCharacterData SetCriDmg(float criDmg = 1.5f) // 크리티컬 데미지 설정 빌더
    {
        this.stats.SetBase(StatType.CRID, criDmg);
        return this;
    }
    public void SetInfoObj(GameObject obj)
    {
        this.CharacterInformationObj = obj;
    }
    public override string ToString() // 플레이어 캐릭터 데이터 출력
    {
        return $"{base.ToString()}\nJump Count : {JumpCnt}\nJump Power : {JumpPower}\nCritical Chance : {Cri}\nCritical Damage : {CriDmg}";
    }
    public GameObject CharacterInformationObj;
    public void RefreshUIData()
    {
        Transform status = CharacterInformationObj.transform.Find("CharacterDataUI").Find("Status");
        Transform statusData = status.Find("Data");

        statusData.Find("HP").Find("val").GetComponent<TextMeshProUGUI>().text = $"{MaxHP}";
        statusData.Find("ATK").Find("val").GetComponent<TextMeshProUGUI>().text = $"{Atk}";
        statusData.Find("ATS").Find("val").GetComponent<TextMeshProUGUI>().text = $"{Ats}";
        statusData.Find("DEF").Find("val").GetComponent<TextMeshProUGUI>().text = $"{Def}";
        statusData.Find("CRI").Find("val").GetComponent<TextMeshProUGUI>().text = $"{Cri}";
        statusData.Find("CRID").Find("val").GetComponent<TextMeshProUGUI>().text = $"{CriDmg}";
        statusData.Find("SPD").Find("val").GetComponent<TextMeshProUGUI>().text = $"{Spd}";
        statusData.Find("JMP").Find("val").GetComponent<TextMeshProUGUI>().text = $"{JumpPower}";
    }
    public override CharacterStats GetStats() => stats;
}
public class PlayableCharacter : Character
{
    public class PlayerEquipments
    {
        private Item mainWeapon;
        private Item subWeapon;
        private Item helmet;
        private Item armor;
        private Item pants;

        public Item MainWeapon
        {
            get => mainWeapon;
            set => mainWeapon = value;
        }
        public Item SubWeapon
        {
            get => subWeapon;
            set => subWeapon = value;
        }
        public Item Helmet
        {
            get => helmet;
            set => helmet = value;
        }
        public Item Armor
        {
            get => armor;
            set => armor = value;
        }
        public Item Pants
        {
            get => pants;
            set => pants = value;
        }
    }
    public class Inventory
    {
        public PlayerEquipments equipments = new();
        public ItemSlot[] backpack = new ItemSlot[15]; //아이템, 개수를 담은 리스트
    }
    public Inventory inventory = new();
    public GameObject CharacterInformationObj;
    public Animator anim; // 플레이어 캐릭터 애니메이터
    public Material material;
    public PlayableCharacterData Data
    {
        get
        {
            if (data is not null) return (PlayableCharacterData)data;
            data = new PlayableCharacterData(new CharacterData("Player").SetInvicibleTime(0.2f))
                .SetJCnt(4)
                .SetJPow(12.0f)
                .SetCri(0.0f)
                .SetCriDmg(1.5f);
            return (PlayableCharacterData)data;
        }
    }
    private static PlayableCharacter inst; // 싱글턴 인스턴스
    public static PlayableCharacter Inst
    {
        get
        {
            if (inst != null) return inst;
            return Component.FindFirstObjectByType<PlayableCharacter>();
        }
        private set
        {
            inst = value;
        }
    }
    private Image messagePortrait, imageOnMessage; // 메시지 박스에 표시되는 이미지
    private TextMeshProUGUI unitName, message; // 메시지 박스에 표시되는 유닛 이름과 메시지
    public GameObject messageObj; // 메시지 박스 오브젝트
    private RectTransform messageBox; // 메시지 박스의 RectTransform
    private InputSystem_Actions inputAction; // 인풋 액션
    private const float hpBarSpd = 5.0f; // HP 바 스피드
    private Camera cam; // 메인 카메라
    public SlicedFilledImage hpBar; // HP 바 이미지
    public SlicedFilledImage hpBarSec; // HP 바 서브 이미지
    public TextMeshProUGUI hpVal; // HP 값 텍스트
    public Transform arm; // 플레이어의 팔 트랜스폼
    private SpriteRenderer weaponSprite; // 플레이어의 무기 스프라이트 렌더러
    private SpriteRenderer subWeaponSprite; // 플레이어의 무기 스프라이트 렌더러
    private Weapon weaponScript; // 플레이어의 무기 스크립트
    private bool isDropdown; // 드롭다운 여부
    private bool isHealing; // 힐링 여부
    private Coroutine hpSmooth; // HP 바 부드럽게 채우기 코루틴
    public GameObject statusObj;
    private SpriteAtlas weaponAtlas; // 무기 스프라이트 아틀라스
    public event Action<EquipmentType, Item> OnEquipmentChanged; //옵저버 패턴을 이용해 장비변경시 알림
    public event Action<int, ItemSlot> OnInventoryChanged;//옵저버 패턴을 이용해 인벤토리 변경시 알림
    protected override void Awake()
    {
        //싱글턴 인스턴스 설정
        if (PlayableCharacter.Inst != null && PlayableCharacter.Inst != this)
        {
            Destroy(PlayableCharacter.Inst);
            return;
        }
        PlayableCharacter.Inst = this;
        DontDestroyOnLoad(gameObject);
        InitAtlas();
        // 데이터 초기화(우선적으로 getter에서 처리하지만, 명시적으로 초기화도 해둠)
        data ??= new PlayableCharacterData(new CharacterData("Player").SetInvicibleTime(0.2f))
            .SetJCnt(4)
            .SetJPow(12.0f)
            .SetCri(0.1f)
            .SetCriDmg(1.5f);
        data.GetStats().SetBase(StatType.ATS, 0.0f);
        data.HP = data.MaxHP;

        ((PlayableCharacterData)data).SetInfoObj(CharacterInformationObj);
        //((PlayableCharacterData)data).RefreshUIData();

        // 인풋 액션 초기화
        inputAction = new();
        Application.targetFrameRate = 120;
        //실질 점프 카운트 초기화
        jumpCnt = Data.JumpCnt;

        // 무기 스프라이트, 스크립트, 메시지 박스, 카메라 초기화
        weaponScript = arm.GetComponentInChildren<Weapon>();
        weaponSprite = weaponScript.GetComponent<SpriteRenderer>();
        subWeaponSprite = arm.GetChild(1).GetComponent<SpriteRenderer>();
        SetupMessageBox();
        cam = Camera.main;
        GameObject obj = Instantiate(new GameObject(), null);
        obj.transform.position = weaponSprite.transform.position;
        obj.name = "HitBox";
        BoxCollider2D bC = obj.AddComponent<BoxCollider2D>();
        bC.size = new Vector2(0.5f, 0.5f);
    }
    /// <summary>
    /// 무기 아틀라스 로드
    /// </summary>
    /// <returns></returns>
    private async void InitAtlas()
    {
        AsyncOperationHandle<SpriteAtlas> loadSprite = Addressables.LoadAssetAsync<SpriteAtlas>($"Characters/Weapons");
        var ct = this.GetCancellationTokenOnDestroy();
        weaponAtlas = await loadSprite.ToUniTask(cancellationToken: ct);


        SetMainWeapon(ItemDataManager.GetItem("01001"));
        SetSubWeapon(ItemDataManager.GetItem("02001"));
        SetHelmet(ItemDataManager.GetItem("03001"));
        SetArmor(ItemDataManager.GetItem("04001"));
        SetPants(ItemDataManager.GetItem("05001"));
    }
    void OnEnable()
    {
        // 인풋 액션 활성화 및 이벤트 핸들러 등록 (= 키설정)
        inputAction.Enable();
        inputAction.Player.Move.performed += OnMovement;
        inputAction.Player.Move.canceled += OnMovement;
        inputAction.Player.Jump.performed += OnJump;
        inputAction.Player.Attack.performed += OnAttack;
        inputAction.Player.Dropdown.performed += OnDropdown;
    }
    /// <summary>
    ///하강(드롭다웃) 액션 등록
    /// </summary>
    /// <param name="context"></param>
    private void OnDropdown(InputAction.CallbackContext context)
    {
        if (isGround && landingLayer == LAYER.PLATFORM)
        {
            col.isTrigger = true;
            isDropdown = true;
        }
    }
    /// <summary>
    /// 공격 액션 등록
    /// </summary>
    /// <param name="context"></param>
    private void OnAttack(InputAction.CallbackContext context)
    {
        Attack();
    }
    /// <summary>
    /// 공격 메소드 오버라이드
    /// </summary>
    protected override void Attack()
    {
        base.Attack();
        weaponScript.StartSwing();
    }
    protected override void Update()
    {
        // Character(부모 클래스)의 Update 메소드 호출
        base.Update();
    }
    /// <summary>
    /// 부드러운 체력바 채우기 코루틴
    /// </summary>
    /// <param name="bar"></param>
    /// <returns></returns>
    IEnumerator HpBarFillsSmooth(SlicedFilledImage bar)
    {
        yield return new WaitForSeconds(0.3f);
        while (Mathf.Abs(bar.fillAmount - (float)Mathf.FloorToInt(Data.HP) / Mathf.FloorToInt(Data.MaxHP)) > 0.01f)
        {
            bar.fillAmount = Mathf.Lerp(bar.fillAmount, (float)Mathf.FloorToInt(Data.HP) / Mathf.FloorToInt(Data.MaxHP), Time.deltaTime * hpBarSpd);
            yield return null;
        }
        bar.fillAmount = (float)Mathf.FloorToInt(Data.HP) / Mathf.FloorToInt(Data.MaxHP);
    }
    protected override void Landing(LAYER layer)
    {
        jumpCnt = Data.JumpCnt;
        isJump = false;
        base.Landing(layer);
    }
    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        //공중 판정 체크
        anim.SetBool("JUMP", !isGround);
        if (weaponScript.anim.GetBool("IsSwing")) return;

        const float offsetDeg = -90f; // 무기 이미지가 세로로 되어 있어 보정 각도 필요

        Vector3 mouseW = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseW.z = arm.position.z;

        Vector2 delta = (Vector2)(mouseW - arm.position); // 마우스 위치와 팔 위치의 차이 벡터
        float angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; // 각도 계산
        float finalZ = angleDeg + offsetDeg; // 오프셋 보정

        arm.rotation = Quaternion.Euler(0f, 0f, finalZ);
        {
            Transform body = transform.GetChild(0);
            Vector3 s = body.localScale;
            s.x = (delta.x >= 0f) ? -2f : 2f;
            s.y = 2f;
            s.z = 1f;
            body.localScale = s;
        }
        {
            Transform mainWeapon = weaponScript.transform;
            if (mainWeapon != null)
            {
                float d = Vector3.Dot(arm.up, Vector3.right);
                Vector3 ls = mainWeapon.transform.parent.localScale;
                float absX = Mathf.Abs(ls.x);
                ls.x = (d >= 0f) ? -absX : absX;
                mainWeapon.transform.parent.localScale = ls;
            }
        }
    }
    public void OnMovement(InputAction.CallbackContext context) // 이동 액션 등록
    {
        moveVec = context.ReadValue<Vector2>();
    }
    public void OnJump(InputAction.CallbackContext context) // 점프 액션 등록
    {
        if (jumpCnt > 0 && context.performed) //점프 조건 분기
        {
            rigid.gravityScale = 1.5f; // 중력 초기화
            isJump = true; // 점프 상태 등록
            rigid.linearVelocity = new Vector2(rigid.linearVelocity.x, Data.JumpPower); // 점프 적용 계산
            col.isTrigger = true;
            jumpCnt--; // 점프 횟수 차감
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
        //info 배열은 [0] = 이미지 경로, [1] = 유닛 이름, [2] = 메시지 내용, [3] = 이미지 경로
        messageObj.SetActive(true);
        message.text = info[2];
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
        if (collision.gameObject.CompareTag("FallingZone"))
        {
            transform.position = savedPos + Vector3.up;
            TakeDamage(data.MaxHP * 0.2f);
        }
        RaycastHit2D h = Physics2D.Raycast(foot.position, Vector2.down, rayDistance, LayerMask.GetMask("Floor", "Platform"));         // 바닥 충돌 처리 전용 레이캐스트 히트

        if (h && !isDropdown && rigid.linearVelocityY <= 0.0f) // 바닥 착지 조건 분기
        {
            Landing((LAYER)collision.gameObject.layer); //착지 메소드 호출
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        // 하강 기능용 로직
        if (collision.gameObject.layer == (int)LAYER.PLATFORM && isDropdown) // 하강은 플랫폼 레이어에서만 가능
        {
            isDropdown = false;
        }
    }
    public void SetHP(int value) // 체력 설정 메소드
    {
        //회복일 경우 HPBar와 HPBarSec의 순서를 변경해야 함므로 사용
        isHealing = Mathf.FloorToInt(value) > Mathf.FloorToInt(Data.HP);

        //회복 로직
        Data.HP = value;
        hpVal.text = $"{Mathf.FloorToInt(value)} / {Mathf.FloorToInt(Data.MaxHP)}";
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        if (isHealing)
        {
            hpBar.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
        }
        else
        {
            hpBarSec.fillAmount = (float)Mathf.FloorToInt(value) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
        }
    }
    public override void TakeDamage(float damage) // 피해 적용 로직
    {
        base.TakeDamage(damage);
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - data.Def);
        if (dmg < 0) dmg = 0;
        SetHP(data.HP - dmg);
        if (data.HP <= 0)
        {
            Debug.Log($"{data.UnitName} has died.");
        }
    }
    protected override void Movement()
    {
        base.Movement();
        if (isGround) anim.SetBool("1_Move", moveVec.x != 0.0f);
    }
    protected override IEnumerator Hit()
    {
        InvincibleTimer = data.InvincibleTime;
        hitBox.enabled = false;

        float colorVal = 0f;
        float elapsedTime = 0f;
        material.color = Color.red;
        while (InvincibleTimer > 0.0f)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / data.InvincibleTime;
            colorVal = Mathf.Lerp(colorVal, 1f, t);
            material.color = new Color(1, colorVal, colorVal, 1);
            InvincibleTimer -= Time.deltaTime;

            yield return null;
        }
        material.color = new Color(1, 1, 1, 1);
        hitCoroutine = null;
    }
    /// <summary>
    /// 주무기 설정
    /// </summary>
    /// <param name="item"></param> <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void SetMainWeapon(Item item)
    {
        if (item.two_hander)
            SetSubWeapon(item, true);
        else if (inventory.equipments.MainWeapon.two_hander)
            SetSubWeapon(ItemDataManager.GetItem("00000"), false);

        weaponSprite.sprite = weaponAtlas.GetSprite(item.id);

        if(inventory.equipments.MainWeapon is Item i)
            Debug.Log($"끼고있던 템 : {i.GetProvider().GetHashCode()}");
        Data.GetStats().RemoveProvider(inventory.equipments.MainWeapon.GetProvider());
        inventory.equipments.MainWeapon = item;
        Debug.Log($"새 템 : {item.GetProvider().GetHashCode()}");
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.MAINWEAPON, item);
    }
    /// <summary>
    /// 보조무기 설정
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isTwoHander"></param> <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isTwoHander"></param>
        public void SetSubWeapon(Item item, bool isTwoHander = false)
    {
        subWeaponSprite.sprite = isTwoHander ? weaponAtlas.GetSprite(inventory.equipments.MainWeapon.id) : weaponAtlas.GetSprite(item.id);

        if (inventory.equipments.SubWeapon is Item i)
            Data.GetStats().RemoveProvider(i.GetProvider());
        if (isTwoHander) return;
        inventory.equipments.SubWeapon = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.SUBWEAPON, item);
    }
    /// <summary>
    /// 헬멧 설정
    /// </summary>
    /// <param name="item"></param> <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void SetHelmet(Item item)
    {
        if (inventory.equipments.Helmet is Item i)
            Data.GetStats().RemoveProvider(i.GetProvider());
            
        inventory.equipments.Helmet = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.HELMET, item);
    }
    /// <summary>
    /// 갑옷 설정
    /// </summary>
    /// <param name="item"></param> <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void SetArmor(Item item)
    {
        if (inventory.equipments.Armor is Item i)
            Data.GetStats().RemoveProvider(i.GetProvider());
            
        inventory.equipments.Armor = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.ARMOR, item);
    }
    /// <summary>
    /// 바지 설정
    /// </summary>
    /// <param name="item"></param> <summary>
    /// 
    /// </summary>
    /// <param name="item"></param>
    public void SetPants(Item item)
    {
        if (inventory.equipments.Pants is Item i)
            Data.GetStats().RemoveProvider(i.GetProvider());
            
        inventory.equipments.Pants = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.PANTS, item);
    }
    private const int maxStack = 99; //기본 아이템 최대 쌓기 개수
    /// <summary>
    /// 아이템 습득
    /// </summary>
    /// <param name="item"></param>
    /// <param name="ea"></param>
    public void GetItem(Item item, int ea = 1)
    {
        for (int i = 0; i < inventory.backpack.Length; i++)
        {
            if (inventory.backpack[i] != null && inventory.backpack[i].item.id == item.id && item.stackable)
            {
                inventory.backpack[i].ea = Math.Min(inventory.backpack[i].ea + ea, maxStack);
                OnInventoryChanged?.Invoke(i, inventory.backpack[i]);
                return;
            }
        }
    }
}