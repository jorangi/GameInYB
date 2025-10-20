using System.Runtime.InteropServices.ComTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Tilemaps;
using UnityEngine.U2D;
using UnityEngine.UI;
using static PlayerStats;
using System.Text;

public sealed class UnityServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> map = new();
    public void Add<T>(T imple) where T : class => map[typeof(T)] = imple;
    public object GetService(Type serviceType) => map.TryGetValue(serviceType, out var o) ? o : null;
    public T Get<T>() where T : class => (T)GetService(typeof(T));
}
public readonly struct ItemStackSnap
{
    public readonly int index;   // 0~14
    public readonly string id;   // Item 식별자
    public readonly int ea;      // 개수(스택 수)

    public ItemStackSnap(int index, string id, int ea)
    {
        this.index = index;
        this.id = id ?? "";
        this.ea = ea;
    }
}
public readonly struct InventorySnapshot
{
    public readonly string mainWeaponId;
    public readonly string subWeaponId;
    public readonly string helmetId;
    public readonly string armorId;
    public readonly string pantsId;
    public readonly ItemStackSnap[] backpack; // 길이 15 고정 권장
    public InventorySnapshot(
        string main, string sub, string helmet, string armor, string pants,
        ItemStackSnap[] backpack)
    {
        mainWeaponId = main ?? "";
        subWeaponId  = sub ?? "";
        helmetId     = helmet ?? "";
        armorId      = armor ?? "";
        pantsId      = pants ?? "";
        this.backpack = backpack ?? Array.Empty<ItemStackSnap>();
    }
}
public interface IInventorySnapshotProvider { InventorySnapshot Snapshot(); }
public class ItemSlot
{
    public int index;
    public Item item;
    public int ea;
    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
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
    public static bool operator ==(ItemSlot a, ItemSlot b) => ReferenceEquals(a, b);
    public static bool operator !=(ItemSlot a, ItemSlot b) => !(a == b);
    public ItemSlot(int index, Item item = default, int ea = 0)
    {
        this.index = index;
        this.item = item;
        this.ea = ea;
    }
    public override string ToString()
    {
        return $"{index}: {item.id}, {ea}";
    }
}
public enum EquipmentType
{
    ARMOR,
    PANTS,
    HELMET,
    MAINWEAPON,
    SUBWEAPON
}
public interface IPlayerStatMapper
{
    public PlayerStats ToDto(in InventorySnapshot inv);
    public void ApplyDto(in PlayerStats dto);
}
[Serializable]
public class PlayableCharacterData : CharacterData, IPlayerStatMapper
{
    public struct EquipContribution
    {
        public float hp;
        public float atk;
        public float def;
        public float ats;
        public float cri;
        public float crid;
        public float spd;
        public int jCnt;
        public float jmp;
    }
    public Func<string, EquipContribution> ResolveEquip;                                    
    public Action<string, string, string, string, string> ApplyEquipIds;                    
    public Action<int, string, int> ApplyBackpackSlot;                                      

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
    public PlayerStats ToDto(in InventorySnapshot inv)
    {
        // 1) equiped: 단순 ID 5개 → 문자열 배열 JSON
        string equipedJson = BuildJsonStringArray(new[]
        {
            inv.helmetId     ?? "",
            inv.armorId      ?? "",
            inv.pantsId      ?? "",
            inv.mainWeaponId ?? "",
            inv.subWeaponId  ?? "",
        });
        // 2) inventory: 각 슬롯을 {index,itemid,ea} JSON "문자열"로 만들고,
        //    그 "문자열"들의 배열 JSON을 만든다.
        var slotJsonStrings = new string[inv.backpack.Length];
        for (int i = 0; i < inv.backpack.Length; i++)
        {
            // 슬롯을 객체 → JSON 문자열
            string objJson = SlotObjectJson(inv.backpack[i].index, inv.backpack[i].id, inv.backpack[i].ea);
            // 그 JSON을 "문자열"로 배열에 담기 위해 그대로 넣는다(나중에 배열 JSON에 포함)
            slotJsonStrings[i] = objJson;
        }
        // 문자열 배열 JSON으로 포장(각 요소는 JSON 오브젝트를 담은 문자열)
        string inventoryJsonAsStringArray = BuildJsonStringArray(slotJsonStrings);
        return new PlayerStats
        {
            hp   = MaxHP,
            atk  = stats.GetFinal(StatType.ATK),
            def  = stats.GetFinal(StatType.DEF),
            // ats = stats.GetFinal(StatType.ATS),
            cri  = stats.GetFinal(StatType.CRI),
            crid = stats.GetFinal(StatType.CRID),
            spd  = stats.GetFinal(StatType.SPD),
            jmp  = (int)stats.GetFinal(StatType.JCNT),
            //jCnt  = (int)stats.GetFinal(StatType.JCNT),
            //jmp  = stats.GetFinal(StatType.Jmp),

            clear   = 0,
            chapter = 0,
            stage   = 0,
            mapid   = "",

            // 서버 스키마가 문자열 JSON을 요구하므로 string 필드에 넣음
            equiped  = equipedJson,
            inventory = inventoryJsonAsStringArray
        };
    }
    private static string BuildJsonStringArray(string[] values)
    {
        if (values == null || values.Length == 0) return "[]";
        var sb = new StringBuilder(values.Length * 8);
        sb.Append('[');
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append('"');
            sb.Append(EscapeJsonString(values[i] ?? ""));
            sb.Append('"');
        }
        sb.Append(']');
        return sb.ToString();
    }
    private static string SlotObjectJson(int index, string itemId, int ea)
    {
        var sb = new StringBuilder(64);
        sb.Append('{');
        sb.Append("\"index\":").Append(index).Append(',');
        sb.Append("\"itemid\":\"").Append(EscapeJsonString(itemId ?? "")).Append("\",");
        sb.Append("\"ea\":").Append(ea);
        sb.Append('}');
        return sb.ToString();
    }
    private static string EscapeJsonString(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        var sb = new StringBuilder(s.Length + 8);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\"': sb.Append("\\\""); break;
                case '\b': sb.Append("\\b");  break;
                case '\f': sb.Append("\\f");  break;
                case '\n': sb.Append("\\n");  break;
                case '\r': sb.Append("\\r");  break;
                case '\t': sb.Append("\\t");  break;
                default:
                    if (c < 0x20)
                    {
                        sb.Append("\\u");
                        sb.Append(((int)c).ToString("x4"));
                    }
                    else sb.Append(c);
                    break;
            }
        }
        return sb.ToString();
    }
    public void ApplyDto(in PlayerStats dto)
    {
        var equipIds = ParseJsonStringArrayLocal(dto.equiped);
        string helmetId = equipIds[0];
        string armorId  = equipIds[1];
        string pantsId  = equipIds[2];
        string mainId   = equipIds[3];
        string subId    = equipIds[4];
        
        PlayableCharacter.Inst.SetHelmet(ItemDataManager.GetItem(helmetId));
        PlayableCharacter.Inst.SetArmor(ItemDataManager.GetItem(armorId));
        PlayableCharacter.Inst.SetPants(ItemDataManager.GetItem(pantsId));
        PlayableCharacter.Inst.SetSubWeapon(ItemDataManager.GetItem(subId));
        PlayableCharacter.Inst.SetMainWeapon(ItemDataManager.GetItem(mainId));

        Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");

        var probe = new CharacterStats();
        void AddEquipProviderIfAny(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var item = ItemDataManager.GetItem(id);
            probe.AddProvider(item.GetProvider());
        }
        AddEquipProviderIfAny(helmetId);
        AddEquipProviderIfAny(armorId);
        AddEquipProviderIfAny(pantsId);
        AddEquipProviderIfAny(mainId);
        AddEquipProviderIfAny(subId);
        
        _ = stats.GetAllFinal(); // Recalculate 유도
        Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");

        stats.SetBase(StatType.HP,   SolveBase(probe, StatType.HP,   dto.hp));
        stats.SetBase(StatType.ATK,  SolveBase(probe, StatType.ATK,  dto.atk));
        stats.SetBase(StatType.DEF,  SolveBase(probe, StatType.DEF,  dto.def));
        stats.SetBase(StatType.CRI,  SolveBase(probe, StatType.CRI,  dto.cri));
        stats.SetBase(StatType.CRID, SolveBase(probe, StatType.CRID, dto.crid));
        stats.SetBase(StatType.SPD,  SolveBase(probe, StatType.SPD,  dto.spd));
        stats.SetBase(StatType.JCNT, SolveBase(probe, StatType.JCNT, dto.jmp));
        
        _ = stats.GetAllFinal(); // Recalculate 유도
        Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");

        var invStrArr = ParseJsonStringArrayLocal(dto.inventory);
        var pc = PlayableCharacter.Inst;
        var bag = pc.Inventory.backpack; // ItemSlot[]
        for (int i = 0; i < invStrArr.Length; i++)
        {
            if (!TryParseInventoryObject(invStrArr[i], out int idx, out string itemId, out int ea))
                continue;
            if (idx < 0 || idx >= bag.Length) continue;

            // 기존 슬롯의 provider 제거
            var prev = bag[idx];
            if (prev.item.id != null)
                pc.Data.GetStats().RemoveProvider(prev.item.GetProvider());
            // 새 아이템 대입
            var newItem = ItemDataManager.GetItem(itemId);
            prev.index = idx;
            prev.item = newItem;
            prev.ea = ea;
            bag[idx] = prev;

            PlayableCharacter.Inst.InvokeBackpackChanged(idx, bag[idx]);

            // 새 provider 추가
            if (newItem.id != null && newItem.id[0]=='0' && !Array.Exists(new char[] { '1','2','3','4','5'}, s => s == newItem.id[1]))
                pc.Data.GetStats().AddProvider(newItem.GetProvider());
                
            Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");
        }
        
        Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");
        // 6) 최종 재계산 트리거(필요 시)
        _ = stats.GetAllFinal(); // Recalculate 유도
        Debug.Log($"ATK: {GetStats().GetFinal(StatType.ATK)}, DEF: {GetStats().GetFinal(StatType.DEF)}");
    }
    string[] ParseJsonStringArrayLocal(string json)
    {
        // BuildJsonStringArray로 만든 포맷 ["...","..."] 전용 간단 파서
        if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
        var list = new List<string>(16);
        int i = 0, n = json.Length;
        void SkipWs(){ while(i<n && char.IsWhiteSpace(json[i])) i++; }
        SkipWs(); if (i>=n || json[i] != '[') return Array.Empty<string>(); i++;
        SkipWs(); if (i<n && json[i] == ']'){ i++; return list.ToArray(); }
        while (i<n){
            SkipWs(); if (i>=n || json[i] != '"') break; i++; // "
            var sb = new StringBuilder();
            while(i<n){
                char c = json[i++]; 
                if (c=='\\'){
                    if (i>=n) break; char e = json[i++];
                    switch(e){
                        case '\\': sb.Append('\\'); break;
                        case '"':  sb.Append('\"'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        case 'b':  sb.Append('\b'); break;
                        case 'f':  sb.Append('\f'); break;
                        case 'u':
                            if (i+3<n){
                                string hex = json.Substring(i,4);
                                if (ushort.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out var code)){
                                    sb.Append((char)code); i+=4;
                                }
                            }
                            break;
                        default: sb.Append(e); break;
                    }
                } else if (c=='"') { break; }
                else sb.Append(c);
            }
            list.Add(sb.ToString());
            SkipWs(); if (i<n && json[i]==','){ i++; continue; }
            SkipWs(); if (i<n && json[i]==']'){ i++; break; }
        }
        return list.ToArray();
    }
    bool TryParseInventoryObject(string json, out int index, out string itemid, out int ea)
    {
        // 아주 단순한 키-값 추출기: {"index":0,"itemid":"xxx","ea":3}
        // (공백/순서 다소 허용)
        index = 0; itemid = ""; ea = 0;
        if (string.IsNullOrEmpty(json)) return false;
        // index
        {
            int k = json.IndexOf("\"index\"", StringComparison.Ordinal);
            if (k>=0) {
                k = json.IndexOf(':', k); if (k>=0){
                    int s = k+1; while(s<json.Length && char.IsWhiteSpace(json[s])) s++;
                    int e = s; while(e<json.Length && (char.IsDigit(json[e]) || json[e]=='-')) e++;
                    int.TryParse(json.Substring(s, e-s), out index);
                }
            }
        }
        // itemid
        {
            int k = json.IndexOf("\"itemid\"", StringComparison.Ordinal);
            if (k>=0){
                k = json.IndexOf(':', k); if (k>=0){
                    int s = json.IndexOf('"', k); if (s>=0){
                        int e = json.IndexOf('"', s+1);
                        if (e> s) itemid = json.Substring(s+1, e-(s+1));
                    }
                }
            }
        }
        // ea
        {
            int k = json.IndexOf("\"ea\"", StringComparison.Ordinal);
            if (k>=0){
                k = json.IndexOf(':', k); if (k>=0){
                    int s = k+1; while(s<json.Length && char.IsWhiteSpace(json[s])) s++;
                    int e = s; while(e<json.Length && (char.IsDigit(json[e]) || json[e]=='-')) e++;
                    int.TryParse(json.Substring(s, e-s), out ea);
                }
            }
        }
        return true;
    }
    float SolveBase(CharacterStats p, StatType stat, float target)
    {
        p.SetBase(stat, 0f);
        float e0 = p.GetFinal(stat);       // e0 = sumAdd * mul
        p.SetBase(stat, 1f);
        float e1 = p.GetFinal(stat);       // e1 = (1+sumAdd)*mul = e0 + mul
        float mul = Mathf.Max(1e-6f, e1 - e0);
        float sumAdd = e0 / mul;
        float b = (target / mul) - sumAdd;
        return b < 0f ? 0f : b;
    }
    public string accessToken;
    public string refreshToken;
    public string nickname;
    public string[] roles;
    public PlayerStats statsDTO;
}
public class Backpack : IEnumerable<ItemSlot>
{
    public Backpack()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new ItemSlot(i);
        }
    }
    private readonly ItemSlot[] items = new ItemSlot[15]; //아이템, 개수를 담은 리스트
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
    /// 백팩의 길이
    /// </summary>
    public int Length => items.Length;
    /// <summary>
    /// 백팩에 담긴 아이템 종류의 수
    /// </summary>
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
            if (items[i].item == default || items[i].item.id == "00000")
            {
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
    /// <returns></returns>
    public int GetItemCount(string itemId)
    {
        ItemSlot slot = GetItem(itemId);
        if (slot is null) return -1;
        return slot.ea;
    }
    public void Clear()
    {
        for (int i = 0; i < items.Length; i++)
        {
            items[i] = new(i);
        }
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
    public event Action<EquipmentType, Item> OnEquipmentChanged; //옵저버 패턴을 이용해 장비변경시 알림
    public event Action<int, ItemSlot> OnBackpackChanged;//옵저버 패턴을 이용해 인벤토리 변경시 알림
    public void InvokeEquipmentChanged(EquipmentType type, Item item);
    public void InvokeBackpackChanged(int index, ItemSlot item);
}
public class Inventory
{
    public Inventory()
    {
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
    public PlayerEquipments()
    {
        helmet = new(0, default);
        armor = new(1, default);
        pants = new(2, default);
        mainWeapon = new(3, default);
        subWeapon = new(4, default);
    }
}
public interface IPlayableCharacterFacade
{
    CharacterStats Stats { get; }
    Backpack Backpack { get; }

    void SetMainWeapon(Item item);
    void SetSubWeapon(Item item);
    void SetHelmet(Item item);
    void SetArmor(Item item);
    void SetPants(Item item);

    void InvokeBackpackChanged(int index, ItemSlot slot);
}
public sealed class PlayableCharacterFacadeAdapter : IPlayableCharacterFacade
{
    private readonly PlayableCharacter _pc;

    public PlayableCharacterFacadeAdapter(PlayableCharacter pc)
    {
        _pc = pc != null ? pc : throw new System.ArgumentNullException(nameof(pc));
    }

    public CharacterStats Stats => _pc.Data.GetStats();

    public Backpack Backpack => _pc.Inventory.backpack;

    public void SetMainWeapon(Item item) => _pc.SetMainWeapon(item);
    public void SetSubWeapon(Item item)  => _pc.SetSubWeapon(item);
    public void SetHelmet(Item item)     => _pc.SetHelmet(item);
    public void SetArmor(Item item)      => _pc.SetArmor(item);
    public void SetPants(Item item)      => _pc.SetPants(item);

    public void InvokeBackpackChanged(int index, ItemSlot slot)
    {
        _pc.InvokeBackpackChanged(index, slot);
    }
}
public sealed class PlayerStatsLoader
{
    private readonly IItemRepository _items;
    private readonly IPlayableCharacterFacade _pc;

    public PlayerStatsLoader(IItemRepository items, IPlayableCharacterFacade pc)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _pc = pc ?? throw new ArgumentNullException(nameof(pc));
    }

    public void ApplyFromDto(in PlayerStats dto)
    {
        var equipIds = ParseJsonStringArrayLocal(dto.equiped);
        string helmetId = equipIds.Length > 0 ? equipIds[0] : "";
        string armorId  = equipIds.Length > 1 ? equipIds[1] : "";
        string pantsId  = equipIds.Length > 2 ? equipIds[2] : "";
        string mainId   = equipIds.Length > 3 ? equipIds[3] : "";
        string subId    = equipIds.Length > 4 ? equipIds[4] : "";

        var probe = new CharacterStats();

        void AddEquipProviderIfAny(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            var item = _items.GetItem(id);
            if (item != null)
                probe.AddProvider(item.GetProvider());
        }

        AddEquipProviderIfAny(helmetId);
        AddEquipProviderIfAny(armorId);
        AddEquipProviderIfAny(pantsId);
        AddEquipProviderIfAny(mainId);
        AddEquipProviderIfAny(subId);

        var stats = _pc.Stats;

        stats.SetBase(StatType.HP,   SolveBase(probe, StatType.HP,   dto.hp));
        stats.SetBase(StatType.ATK,  SolveBase(probe, StatType.ATK,  dto.atk));
        stats.SetBase(StatType.DEF,  SolveBase(probe, StatType.DEF,  dto.def));
        stats.SetBase(StatType.CRI,  SolveBase(probe, StatType.CRI,  dto.cri));
        stats.SetBase(StatType.CRID, SolveBase(probe, StatType.CRID, dto.crid));
        stats.SetBase(StatType.SPD,  SolveBase(probe, StatType.SPD,  dto.spd));
        stats.SetBase(StatType.JCNT, SolveBase(probe, StatType.JCNT, dto.jmp));

        if (!string.IsNullOrEmpty(mainId))
            _pc.SetMainWeapon(_items.GetItem(mainId));
        if (!string.IsNullOrEmpty(subId))
            _pc.SetSubWeapon(_items.GetItem(subId));
        if (!string.IsNullOrEmpty(helmetId))
            _pc.SetHelmet(_items.GetItem(helmetId));
        if (!string.IsNullOrEmpty(armorId))
            _pc.SetArmor(_items.GetItem(armorId));
        if (!string.IsNullOrEmpty(pantsId))
            _pc.SetPants(_items.GetItem(pantsId));

        var invStrArr = ParseJsonStringArrayLocal(dto.inventory);
        var bag = _pc.Backpack;

        for (int i = 0; i < invStrArr.Length; i++)
        {
            if (!TryParseInventoryObject(invStrArr[i], out int idx, out string itemId, out int ea))
                continue;
            if (idx < 0 || idx >= bag.Length) continue;

            var prev = bag[idx];

            if (prev.item.id != null)
                stats.RemoveProvider(prev.item.GetProvider());

            var newItem = _items.GetItem(itemId);
            prev.index = idx;
            prev.item = newItem;
            prev.ea = ea;
            bag[idx] = prev;

            if (newItem.id != null)
                stats.AddProvider(newItem.GetProvider());

            _pc.InvokeBackpackChanged(idx, bag[idx]);
        }

        _ = stats.GetAllFinal();
    }

    private static string[] ParseJsonStringArrayLocal(string json)
    {
        if (string.IsNullOrEmpty(json)) return Array.Empty<string>();
        try
        {
            return JsonUtility.FromJson<StringArrayWrapper>(Wrap(json)).array ?? Array.Empty<string>();
        }
        catch
        {
            Debug.LogWarning("[PlayerStatsLoader] inventory/equiped 파싱 실패. 빈 배열로 처리.");
            return Array.Empty<string>();
        }
    }

    [Serializable]
    private class StringArrayWrapper { public string[] array; }

    private static string Wrap(string rawJsonArray)
    {
        if (!string.IsNullOrEmpty(rawJsonArray) && rawJsonArray.TrimStart().StartsWith("["))
            return "{\"array\":" + rawJsonArray + "}";
        return "{\"array\":[]}";
    }

    private static bool TryParseInventoryObject(string json, out int index, out string itemId, out int ea)
    {
        index = -1;
        itemId = null;
        ea = 0;
        if (string.IsNullOrEmpty(json)) return false;
        try
        {
            var obj = JsonUtility.FromJson<InvObj>(json);
            index = obj.index;
            itemId = obj.itemId;
            ea = obj.ea;
            return !string.IsNullOrEmpty(itemId);
        }
        catch
        {
            return false;
        }
    }

    [Serializable]
    private struct InvObj
    {
        public int index;
        public string itemId;
        public int ea;
    }

    private static float SolveBase(CharacterStats probe, StatType type, float serverValue)
    {
        return serverValue;
    }
}
public class PlayableCharacter : Character, IInventoryData, IInventorySnapshotProvider
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
        await ItemDataManager.Ready;
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
            Transform subWeapon = subWeaponSprite.transform;
            if (mainWeapon != null)
            {
                float d = Vector3.Dot(arm.up, Vector3.right);
                Vector3 ls = mainWeapon.transform.parent.localScale;
                float absX = Mathf.Abs(ls.x);
                ls.x = (d >= 0f) ? -absX : absX;
                mainWeapon.transform.parent.localScale = ls;
                subWeapon.localScale = new(-ls.x, 1, 1);
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
        if (damage < 0.0f) return;
        int dmg = Mathf.RoundToInt(damage - Data.Def);
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
    /// <param name="item"></param>
    public void SetMainWeapon(Item item)
    {
        SetSubWeapon(inventory.equipments.SubWeapon.item, item.twoHander);
        weaponSprite.sprite = weaponAtlas.GetSprite(item.id);
        Data.GetStats().RemoveProvider(inventory.equipments.MainWeapon.item.GetProvider());
        inventory.equipments.MainWeapon.item = item;
        Data.GetStats().AddProvider(item.GetProvider());
        weaponScript.anim.SetBool("IsSwing", false);
        weaponScript.anim.SetInteger("SwingCount", 0);
        OnEquipmentChanged?.Invoke(EquipmentType.MAINWEAPON, item);
    }
    /// <summary>
    /// 보조무기 설정
    /// </summary>
    /// <param name="item"></param>
    /// <param name="isTwoHander"></param>
    public void SetSubWeapon(Item item, bool isTwoHander = false)
    {
        subWeaponSprite.color = isTwoHander ? new(1, 1, 1, 0) : Color.white;
        subWeaponSprite.sprite = weaponAtlas.GetSprite(item.id);
        if (inventory.equipments.SubWeapon is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());

        inventory.equipments.SubWeapon.item = item;
        if (isTwoHander)
        {
            Data.GetStats().AddProvider(item.GetProvider());
        }
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

        inventory.equipments.Helmet.item = item;
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

        inventory.equipments.Armor.item = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.ARMOR, item);
    }
    /// <summary>
    /// 바지 설정
    /// </summary>
    /// <param name="item"></param>
    public void SetPants(Item item)
    {
        if (inventory.equipments.Pants is ItemSlot i)
            Data.GetStats().RemoveProvider(i.item.GetProvider());

        inventory.equipments.Pants.item = item;
        Data.GetStats().AddProvider(item.GetProvider());
        OnEquipmentChanged?.Invoke(EquipmentType.PANTS, item);
    }
    public void InvokeEquipmentChanged(EquipmentType type, Item item) => OnEquipmentChanged?.Invoke(type, item);
    public void InvokeBackpackChanged(int index, ItemSlot item) => OnBackpackChanged?.Invoke(index, item);
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
            InvokeBackpackChanged(slot.index, slot);
            return;
        }
        ItemSlot emptySlot = inventory.backpack.EmptySlot();
        emptySlot.item = item;
        emptySlot.ea = ea;
        Debug.Log($"{emptySlot.index}번 슬롯에 아이템 {emptySlot.item.id}이 추가되었습니다.");
        InvokeBackpackChanged(emptySlot.index, emptySlot);
    }
    public void SwapItem()
    {

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
        InvokeBackpackChanged(itemslot.index, null);
    }
    /// <summary>
    /// 백팩 초기화
    /// </summary>
    public void BackpackClear()
    {
        Backpack.Clear();
        InvokeBackpackChanged(-1, null);
    }
    /// <summary>
    /// 인벤토리 전체 초기화
    /// </summary>
    public void BackpackClearWithEquiped()
    {
        BackpackClear();
        SetMainWeapon(default);
        SetSubWeapon(default);
        SetHelmet(default);
        SetArmor(default);
        SetPants(default);
    }
    /// <summary>
    /// 시작 지점으로 이동
    /// </summary>
    public void InitPos()
    {
        transform.position = new(-3.93f, 0.08f, transform.position.z);
    }

    public InventorySnapshot Snapshot()
    {
        static string idOrEmpty(ItemSlot s)
            => (s != null && s.item.id != null) ? s.item.id : "";

        var slots = Inventory.backpack; // ItemSlot[] 라고 가정
        int n = slots?.Length ?? 0;
        var list = new ItemStackSnap[n];

        for (int i = 0; i < n; i++)
        {
            var s = slots[i];
            int index = s?.index ?? i;
            string id = idOrEmpty(s);
            int ea = s?.ea ?? 0;
            list[i] = new ItemStackSnap(index, id, ea);
        }

        return new InventorySnapshot(
            idOrEmpty(Inventory.equipments?.MainWeapon),
            idOrEmpty(Inventory.equipments?.SubWeapon),
            idOrEmpty(Inventory.equipments?.Helmet),
            idOrEmpty(Inventory.equipments?.Armor),
            idOrEmpty(Inventory.equipments?.Pants),
            list
        );
    }
}