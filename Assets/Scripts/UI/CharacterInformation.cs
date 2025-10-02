using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CharacterInformation : MonoBehaviour, IUI
{
    [SerializeField] private UIContext uiContext;
    [SerializeField] private TextMeshProUGUI hp;
    [SerializeField] private TextMeshProUGUI atk;
    [SerializeField] private TextMeshProUGUI ats;
    [SerializeField] private TextMeshProUGUI def;
    [SerializeField] private TextMeshProUGUI cri;
    [SerializeField] private TextMeshProUGUI crid;
    [SerializeField] private TextMeshProUGUI spd;
    [SerializeField] private TextMeshProUGUI jmp;
    private SpriteAtlas iconAtlas;
    private readonly Image[] slotIcons = new Image[20];
    private void Awake()
    {
        InitAltas();
        uiContext = uiContext != null ? uiContext : GetComponentInParent<UIContext>();
        uiContext.UIRegistry.Register(this, UIType.CHARACTER_INFORMATION);
        Transform equipment = transform.GetChild(0).Find("Inventory").Find("Equipment");
        Transform backpack = transform.GetChild(0).Find("Inventory").Find("Backpack");
        slotIcons[0] = equipment.Find("Head").GetChild(0).GetChild(1).GetComponent<Image>();
        slotIcons[1] = equipment.Find("Armor").GetChild(0).GetChild(1).GetComponent<Image>();
        slotIcons[2] = equipment.Find("Pants").GetChild(0).GetChild(1).GetComponent<Image>();
        slotIcons[3] = equipment.Find("MainWeapon").GetChild(0).GetChild(1).GetComponent<Image>();
        slotIcons[4] = equipment.Find("SubWeapon").GetChild(0).GetChild(1).GetComponent<Image>();
        for (int i = 0; i < 15; i++)
        {
            slotIcons[i + 5] = backpack.GetChild(i).GetChild(1).GetComponent<Image>();
        }
        PlayableCharacter.Inst.OnEquipmentChanged += RefreshEquipment;
        PlayableCharacter.Inst.OnInventoryChanged += RefreshItemIcons;
        gameObject.SetActive(false);
    }
    /// <summary>
    /// 인벤토리 아이템 변경시 아이콘 갱신
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="item"></param> <summary>
    /// 
    /// </summary>
    /// <param name="arg1"></param>
    /// <param name="item"></param>
    private void RefreshItemIcons(int arg1, ItemSlot item)
    {
        slotIcons[arg1 + 5].color = Color.white;
        slotIcons[arg1 + 5].sprite = iconAtlas.GetSprite(item.item.id);
        slotIcons[arg1 + 5].transform.parent.GetChild(2).GetComponent<TextMeshProUGUI>().text = item.ea == 0 || !item.item.stackable ? "" : item.ea.ToString();
        
        Refresh();
    }
    /// <summary>
    /// 장비 아이템 변경시 아이콘 갱신
    /// </summary>
    /// <param name="type"></param>
    /// <param name="item"></param>
    private void RefreshEquipment(EquipmentType type, Item item)
    {
        if (item.Equals(null)) return;
        switch (type)
        {
            case EquipmentType.HELMET:
                slotIcons[0].sprite = iconAtlas.GetSprite(item.id);
                slotIcons[0].transform.parent.GetComponentInChildren<TextMeshProUGUI>().text = "";
                slotIcons[0].color = item.id == "00000" ? new Color(1, 1, 1, 0) : Color.white;
                break;
            case EquipmentType.ARMOR:
                slotIcons[1].sprite = iconAtlas.GetSprite(item.id);
                slotIcons[1].transform.parent.GetComponentInChildren<TextMeshProUGUI>().text = "";
                slotIcons[1].color = item.id == "00000" ? new Color(1, 1, 1, 0) : Color.white;
                break;
            case EquipmentType.PANTS:
                slotIcons[2].sprite = iconAtlas.GetSprite(item.id);
                slotIcons[2].transform.parent.GetComponentInChildren<TextMeshProUGUI>().text = "";
                slotIcons[2].color = item.id == "00000" ? new Color(1, 1, 1, 0) : Color.white;
                break;
            case EquipmentType.MAINWEAPON:
                slotIcons[3].sprite = iconAtlas.GetSprite(item.id);
                slotIcons[3].transform.parent.GetComponentInChildren<TextMeshProUGUI>().text = "";
                slotIcons[3].color = item.id == "00000" ? new Color(1, 1, 1, 0) : Color.white;
                break;
            case EquipmentType.SUBWEAPON:
                slotIcons[4].sprite = iconAtlas.GetSprite(item.id);
                slotIcons[4].transform.parent.GetComponentInChildren<TextMeshProUGUI>().text = "";
                slotIcons[4].color = item.id == "00000" ? new Color(1, 1, 1, 0) : Color.white;
                break;
            default:
                break;
        }
        Refresh();
    }
    /// <summary>
    /// 아이콘 아틀라스 초기화
    /// </summary> <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    private async void InitAltas()
    {
        var ct = this.GetCancellationTokenOnDestroy();
        var handle = Addressables.LoadAssetAsync<SpriteAtlas>("Icon/Icons");
        iconAtlas = await handle.ToUniTask(cancellationToken: ct);
    }
    public void NegativeInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
    public void PositiveInteract(InputAction.CallbackContext context)
    {
        Show();
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        uiContext.UIRegistry.CloseUI(this);
    }
    /// <summary>
    /// 스탯 갱신
    /// </summary>
    public void Refresh()
    {
        #region status
        hp.text = $"{PlayableCharacter.Inst.Data.HP}/{PlayableCharacter.Inst.Data.MaxHP}";
        atk.text = $"{PlayableCharacter.Inst.Data.Atk}";
        ats.text = $"{PlayableCharacter.Inst.Data.Ats}";
        def.text = $"{PlayableCharacter.Inst.Data.Def}";
        cri.text = $"{PlayableCharacter.Inst.Data.Cri}";
        crid.text = $"{PlayableCharacter.Inst.Data.CriDmg}";
        spd.text = $"{PlayableCharacter.Inst.Data.Spd}";
        jmp.text = $"{PlayableCharacter.Inst.Data.JumpPower}({PlayableCharacter.Inst.Data.JumpCnt})";
        #endregion
    }
}