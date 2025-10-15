using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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

public sealed class UnityServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> map = new();
    public void Add<T>(T imple) where T : class => map[typeof(T)] = imple;
    public object GetService(Type serviceType) => map.TryGetValue(serviceType, out var o) ? o : null;
    public T Get<T>() where T : class => (T)GetService(typeof(T));
}
public struct ItemSlot
{
    public int index;
    public Item item;
    public int ea;
    public override readonly bool Equals(object obj) => base.Equals(obj);
    public override readonly int GetHashCode() => base.GetHashCode();
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
    public static bool operator ==(ItemSlot a, ItemSlot b) => a.item.id == b.item.id && a.ea == b.ea;
    public static bool operator !=(ItemSlot a, ItemSlot b) => a.item.id == b.item.id && a.ea == b.ea;
    /// <summary>
    /// Item을 ItemSlot으로 암시적 변환 (기본 개수 1)
    /// </summary>
    /// <param name="i"></param> <summary>
    /// 
    /// </summary>
    /// <param name="i"></param>
    public static implicit operator ItemSlot(Item i) => new() { item = i, ea = 1 };
    /// <summary>
    /// (Item, int)을 ItemSlot으로 암시적 변환(튜플)
    /// </summary>
    /// <param name="i"></param>
    /// <param name="t"></param>
    public static implicit operator ItemSlot((Item i, int ea) t) => new() { item = t.i, ea = t.ea };
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
    public override CharacterStats GetStats() => stats;
}
public class Backpack : IEnumerable<ItemSlot>
    {
        public Backpack()
        {
            Array.Clear(items, 0, items.Length);
            for (int i = 0; i < items.Length; i++) items[i].index = i;
        }
        [SerializeField] private ItemSlot[] items = new ItemSlot[15]; //아이템, 개수를 담은 리스트
        /// <summary>
        /// Backpack().items[index]가 너무 길어서 인덱서를 구현해서 대체
        /// </summary>
        /// <value></value>
        public ItemSlot this[int index]
        {
            get
            {
#if UNITY_EDITOR
                if ((uint)index >= (uint)items.Length)
                    throw new IndexOutOfRangeException("조회하는 인덱스가 백팩의 크기보다 큽니다.");
#endif
                return items[index];
            }
            set
            {

#if UNITY_EDITOR
                if ((uint)index >= (uint)items.Length)
                    throw new IndexOutOfRangeException("조회하는 인덱스가 백팩의 크기보다 큽니다.");
#endif
                items[index] = value;
            }
        }
        /// <summary>
        /// 백팩에 담긴 아이템 종류의 수
        /// </summary> <summary>
        /// 
        /// </summary>
        /// <value></value>
        public int Count
        {
            get
            {
                int cnt = 0;
                foreach (var item in items)
                {
                    if (item.item.id != null) cnt++;
                }
                return cnt;
            }
        }
        public bool IsFull => FindEmptySlot() == -1;
        /// <summary>
        /// 빈 슬롯 인덱스 조회
        /// </summary>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public int FindEmptySlot()
        {
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == default)
                {
                    Debug.Log($"{items[i].index}번 슬롯이 비어있음.");
                    return i;
                }                     
            }
            return -1;
        }
        public ref ItemSlot EmptySlot()
        {
        int emptySlotIndex = FindEmptySlot();
            if (emptySlotIndex == -1) throw new InvalidOperationException("빈 슬롯이 없습니다.");

            return ref items[emptySlotIndex];
        }
        /// <summary>
        /// 열거형 인터페이스 구현
        /// </summary>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IEnumerator<ItemSlot> GetEnumerator()
        {
            foreach (var item in items)
                yield return item;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        /// <summary>
        /// 아이템 ID를 통해 아이템 슬롯 검색
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public ItemSlot GetItem(string itemId)
        {
            foreach (var itemSlot in items)
            {
                if (itemSlot.item.id == itemId) return itemSlot;
            }
            return default;
        }
        /// <summary>
        /// 아이템 ID를 통해 가방 내의 아이템 개수 검색
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns> <summary>
        /// 
        /// </summary>
        /// <param name="itemId"></param>
        /// <returns></returns>
        public int GetItemCount(string itemId)
        {
            ItemSlot slot = GetItem(itemId);
            if (slot == default) return -1;
            return slot.ea;
        }
    public void Clear()
    {
        Array.Clear(items, 0, items.Length);
        for (int i = 0; i < items.Length; i++) items[i].index = i;
    }
    public void RemoveSlot(int index)
    {
        items[index] = default;
    }
}

public interface IInventoryData
{
    Backpack Backpack { get; }
    Inventory Inventory { get; }
}
public class Inventory
{
    public Inventory()
    {
        equipments.Helmet = default;
        equipments.Armor = default;
        equipments.Pants = default;
        equipments.MainWeapon = default;
        equipments.SubWeapon = default;
        backpack.Clear();
    }
    public PlayerEquipments equipments = new();
    public Backpack backpack = new(); //아이템, 개수를 담은 리스트
    public event Action<int> OnSlotPicked;
    public event Action OnPickCanceled;
    private bool picking;
    public bool BeginPickSlot()
    {
        if (picking) return false;
        picking = true;
        return true;
    }
    public void CancelPick()
    {
        if (!picking) return;
        picking = false;
        OnPickCanceled?.Invoke();
    }
    public void NotifySlotClicked(int index)
    {
        if (!picking) return;
        picking = false;
        OnSlotPicked?.Invoke(index);
    }
    public UniTask<int> PickSlotAsync(CancellationToken token)
    {
        if (!BeginPickSlot())
            return UniTask.FromException<int>(new InvalidOperationException("Pick작업이 이미 진행중입니다."));
        var tcs = new UniTaskCompletionSource<int>();
        bool completed = false;
        void HandlePicked(int i)
        {
            if (completed) return;
            completed = true;
            UnSubscribe();
            tcs.TrySetResult(i);
        }
        void HandleCanceled()
        {
            if (completed) return;
            completed = true;
            UnSubscribe();
            tcs.TrySetCanceled();
        }
        void UnSubscribe()
        {
            OnSlotPicked -= HandlePicked;
            OnPickCanceled -= HandleCanceled;
        }
        OnSlotPicked += HandlePicked;
        OnPickCanceled += HandleCanceled;
        var reg = token.Register(() =>
        {
            CancelPick();
        });
        return tcs.Task.ContinueWith(result =>
        {
            reg.Dispose();
            return result;
        });
    }
}
public class PlayerEquipments
{
    private ItemSlot mainWeapon;
    private ItemSlot subWeapon;
    private ItemSlot helmet;
    private ItemSlot armor;
    private ItemSlot pants;
    public ItemSlot MainWeapon
    {
        get => mainWeapon;
        set => mainWeapon = value;
    }
    public ItemSlot SubWeapon
    {
        get => subWeapon;
        set => subWeapon = value;
    }
    public ItemSlot Helmet
    {
        get => helmet;
        set => helmet = value;
    }
    public ItemSlot Armor
    {
        get => armor;
        set => armor = value;
    }
    public ItemSlot Pants
    {
        get => pants;
        set => pants = value;
    }
}
public class PlayableCharacter : Character, IInventoryData
{
    public float gameTimeScale = 1.0f;
    private readonly Inventory inventory = new();
    public Inventory Inventory => inventory;
    public Backpack Backpack => inventory.backpack;
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
    private Camera cam; // 메인 카메라
    public Transform arm; // 플레이어의 팔 트랜스폼
    private SpriteRenderer weaponSprite; // 플레이어의 무기 스프라이트 렌더러
    private SpriteRenderer subWeaponSprite; // 플레이어의 무기 스프라이트 렌더러
    private Weapon weaponScript; // 플레이어의 무기 스크립트
    private bool isDropdown; // 드롭다운 여부
    private SpriteAtlas weaponAtlas; // 무기 스프라이트 아틀라스
    public event Action<EquipmentType, Item> OnEquipmentChanged; //옵저버 패턴을 이용해 장비변경시 알림
    public event Action<int, ItemSlot> OnBackpackChanged;//옵저버 패턴을 이용해 인벤토리 변경시 알림
    [SerializeField] private LogMessageParent logMessageParent;
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
        // 데이터 초기화(우선적으로 getter에서 처리하지만, 명시적으로 초기화도 해둠)
        data ??= new PlayableCharacterData(new CharacterData("Player").SetInvicibleTime(0.2f))
            .SetJCnt(4)
            .SetJPow(12.0f)
            .SetCri(0.1f)
            .SetCriDmg(1.5f);
        data.GetStats().SetBase(StatType.ATS, 0.0f);
        data.GetStats().SetBase(StatType.SPD, 5.0f);
        data.GetStats().SetBase(StatType.DEF, 0f);
        data.health.ApplyHP(data.MaxHP);

        ((PlayableCharacterData)data).SetInfoObj(CharacterInformationObj);

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
    private async void Start()
    {
        await InitAtlas();
        BackpackClearWithEquiped();
        SetMainWeapon(ItemDataManager.GetItem("01001"));
        SetSubWeapon(ItemDataManager.GetItem("02001"));
        SetHelmet(ItemDataManager.GetItem("03001"));
        SetArmor(ItemDataManager.GetItem("04001"));
        SetPants(ItemDataManager.GetItem("05001"));

        InitPos();
    }
    /// <summary>
    /// 무기 아틀라스 로드
    /// </summary>
    /// <returns></returns>
    private async Task<int> InitAtlas()
    {
        AsyncOperationHandle<SpriteAtlas> loadSprite = Addressables.LoadAssetAsync<SpriteAtlas>($"Characters/Weapons");
        var ct = this.GetCancellationTokenOnDestroy();
        weaponAtlas = await loadSprite.ToUniTask(cancellationToken: ct);

        return 1;
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
    void OnDisable()
    {
        inputAction.Player.Move.performed -= OnMovement;
        inputAction.Player.Move.canceled -= OnMovement;
        inputAction.Player.Jump.performed -= OnJump;
        inputAction.Player.Attack.performed -= OnAttack;
        inputAction.Player.Dropdown.performed -= OnDropdown;
        inputAction.Disable();
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
        
        if (InvincibleTimer > 0.0f)
            InvincibleTimer -= Time.deltaTime;
        else
            hitBox.gameObject.SetActive(true);
    }
    /// <summary>
    /// 부드러운 체력바 채우기 코루틴
    /// </summary>
    /// <param name="bar"></param>
    /// <returns></returns>
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
        SetDesiredMove(context.ReadValue<Vector2>().x);
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
    public void SetHP(int value) => Data.health.ApplyHP(value);
    public override void TakeDamage(float damage) // 피해 적용 로직
    {
        base.TakeDamage(damage);
        Debug.Log(damage);
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - Data.Def);
        Debug.Log($"{damage} - {Data.Def} = {dmg}");
        if (dmg < 0) dmg = 1;
        SetHP(Data.health.HP - dmg);
        if (Data.health.HP <= 0)
        {
            Debug.Log($"{Data.UnitName} has died.");
        }
    }
    protected override void Movement()
    {
        base.Movement();
        if (isGround) anim.SetBool("1_Move", !Mathf.Approximately(desiredMoveX, 0f));
    }
    protected override IEnumerator Hit()
    {
        InvincibleTimer = data.InvincibleTime;
        hitBox.gameObject.SetActive(false);

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
        else if (inventory.equipments.MainWeapon.item.two_hander)
            SetSubWeapon(ItemDataManager.GetItem("00000"), false);

        weaponSprite.sprite = weaponAtlas.GetSprite(item.id);

        Data.GetStats().RemoveProvider(inventory.equipments.MainWeapon.item.GetProvider());
        inventory.equipments.MainWeapon = item;
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
        subWeaponSprite.sprite = isTwoHander ? weaponAtlas.GetSprite("00000") : weaponAtlas.GetSprite(item.id);

        if (inventory.equipments.SubWeapon is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());
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
        if (inventory.equipments.Helmet is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());

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
        if (inventory.equipments.Armor is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());

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
        if (inventory.equipments.Pants is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());

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
        if (inventory.backpack.IsFull)
        {
            Debug.Log("인벤토리가 가득 찼습니다.");
            logMessageParent.Spawn("인벤토리가 가득 찼습니다.");
            return;
        }
        if (inventory.backpack.GetItem(item.id) is ItemSlot slot && slot.ea >= maxStack && item.stackable)
        {
            slot.ea += ea;
            OnBackpackChanged?.Invoke(slot.index, slot);
            return;
        }
        ref ItemSlot emptySlot = ref inventory.backpack.EmptySlot();
        ItemSlot newInstance = (item, ea);
        newInstance.index = emptySlot.index;
        emptySlot = newInstance;
        OnBackpackChanged?.Invoke(emptySlot.index, emptySlot);
    }
    /// <summary>
    /// 아이템 제거
    /// </summary>
    /// <param name="itemslot"></param> <summary>
    /// 
    /// </summary>
    /// <param name="itemslot"></param>
    public void RemoveItem(ItemSlot itemslot)
    {
        Backpack[itemslot.index] = default;
        OnBackpackChanged?.Invoke(itemslot.index, default);
    }
    /// <summary>
    /// 백팩 초기화
    /// </summary>
    public void BackpackClear()
    {
        Backpack.Clear();
        OnBackpackChanged?.Invoke(-1, default);
    }
    /// <summary>
    /// 인벤토리 전체 초기화
    /// </summary>
    public void BackpackClearWithEquiped()
    {
        BackpackClear();
        SetMainWeapon(ItemDataManager.GetItem("00000"));
        SetSubWeapon(ItemDataManager.GetItem("00000"));
        SetHelmet(ItemDataManager.GetItem("00000"));
        SetArmor(ItemDataManager.GetItem("00000"));
        SetPants(ItemDataManager.GetItem("00000"));
    }
    /// <summary>
    /// 시작 지점으로 이동
    /// </summary>
    public void InitPos()
    {
        transform.position = new(-3.93f, 0.08f, transform.position.z);
    }
}