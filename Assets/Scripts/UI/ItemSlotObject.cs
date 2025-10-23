using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ItemSlotObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IEndDragHandler, IDragHandler, IDragAndDropHandler
{
    [SerializeField]private int index;
    bool equiped = false;
    private Inventory inventory;
    private IconOver icon;
    private RectTransform rect;
    private Transform iconTransform;
    private IInventoryData inventoryData;
    private ItemSlot itemSlot;
    public string idcode;
    private void Start()
    {
        inventoryData = ServiceHub.Get<IInventoryData>();
        inventory = PlayableCharacter.Inst.Inventory;
        icon = GetComponentInChildren<IconOver>();
        rect = icon.GetComponent<RectTransform>();
        iconTransform = icon.transform;
        if (transform.parent.name.Equals("Backpack"))
            index = transform.GetSiblingIndex();
        else
        {
            index = transform.parent.GetSiblingIndex();
            equiped = true;
        }
        SetItemSlot(index);
        gameObject.name = "slot" + index;
    }
    private void SetItemSlot(int i)
    {
        if (equiped)
        {
            switch (i)
            {
                case 0:
                    itemSlot = inventory.equipments.Helmet;
                    break;
                case 1:
                    itemSlot = inventory.equipments.Armor;
                    break;
                case 2:
                    itemSlot = inventory.equipments.Pants;
                    break;
                case 3:
                    itemSlot = inventory.equipments.MainWeapon;
                    break;
                case 4:
                    itemSlot = inventory.equipments.SubWeapon;
                    break;
            }
        }
        else
        {
            itemSlot = inventory.backpack[i];
        }
    }
    public DragAndDropVisualMode dragAndDropVisualMode => throw new System.NotImplementedException();
    public bool AcceptsDragAndDrop()
    {
        Debug.Log("AcceptsDragAndDrop");
        return false;
    }
    public void DrawDragAndDropPreview()
    {
        Debug.Log("DrawDragAndDropPreview");
    }
    public void ExitDragAndDrop()
    {
        Debug.Log("ExitDragAndDrop");
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if (itemSlot is null) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;
        inventory.NotifySlotClicked(equiped ? index-5 : index);
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (itemSlot is null) return;
        //정보 모달창 표시하기
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (itemSlot is null) return;
        //정보 모달창 숨기기
    }
    public void PerformDragAndDrop()
    {
        Debug.Log("PerformDragAndDrop");
    }
    public void UpdateDragAndDrop()
    {
        Debug.Log("UpdateDragAndDrop");
    }
    /// <summary>
    /// 드래그 시작
    /// </summary>
    /// <param name="eventData"></param>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (itemSlot is null)return;
        if (transform.parent.name.Equals("Backpack"))
            iconTransform.SetParent(transform.parent.parent.parent);
        else
            iconTransform.SetParent(transform.parent.parent.parent.parent);
    }
    public void OnEndDrag(PointerEventData eventData)
    {
        if (itemSlot is null || itemSlot.item == default || itemSlot.item.id == "00000")
            return;
        iconTransform.SetParent(transform);
        iconTransform.SetSiblingIndex(1);
        if (eventData.pointerEnter.TryGetComponent<ItemSlotObject>(out var targetSlot) && targetSlot != this)
        {
            int targetIndex = targetSlot.index;
            if (ReferenceEquals(targetSlot,itemSlot))
            {
                rect.anchoredPosition = Vector2.zero;
                return;
            }
            ItemSlot targetItemSlot = targetSlot.itemSlot;
            Debug.Log($"({index}){itemSlot.item.id}에서 ({targetIndex}){targetItemSlot.item.id}로");
            if (!targetSlot.equiped) // 가방으로
            {
                Debug.Log("가방으로");
                if (equiped) // 장비를
                {
                    Debug.Log("장비를");
                    EquipmentType sourceType = EquipmentType.MAINWEAPON;
                    switch (index) // 현재 선택한 장비가 무엇인지
                    {
                        case 0:
                            sourceType = EquipmentType.HELMET;
                            break;
                        case 1:
                            sourceType = EquipmentType.ARMOR;
                            break;
                        case 2:
                            sourceType = EquipmentType.PANTS;
                            break;
                        case 3:
                            sourceType = EquipmentType.MAINWEAPON;
                            break;
                        case 4:
                            sourceType = EquipmentType.SUBWEAPON;
                            break;
                    }
                    if (targetItemSlot.item == default || targetItemSlot.item.id == "00000") // 대상 슬롯이 비었을 경우
                    {
                        Debug.Log("장비를 빈 슬롯 가방으로");
                        Item targetItem = targetItemSlot.item;
                        int temp = targetItemSlot.ea;
                        targetItemSlot.item = itemSlot.item;
                        targetItemSlot.ea = itemSlot.ea;
                        switch (sourceType) // 현재 선택한 장비가 무엇인지
                        {
                            case EquipmentType.HELMET:
                                PlayableCharacter.Inst.SetHelmet(targetItem);
                                break;
                            case EquipmentType.ARMOR:
                                PlayableCharacter.Inst.SetArmor(targetItem);
                                break;
                            case EquipmentType.PANTS:
                                PlayableCharacter.Inst.SetPants(targetItem);
                                break;
                            case EquipmentType.MAINWEAPON:
                                PlayableCharacter.Inst.SetMainWeapon(targetItem);
                                break;
                            case EquipmentType.SUBWEAPON:
                                PlayableCharacter.Inst.SetSubWeapon(targetItem);
                                break;
                        }
                        itemSlot.ea = temp;
                        rect.anchoredPosition = Vector2.zero;
                        inventoryData.InvokeBackpackChanged(targetIndex, targetItemSlot);
                        return;
                    }
                    EquipmentType targetType = EquipmentType.MAINWEAPON;
                    if (targetItemSlot.item.id[0] == '0') // 가방에 있는 아이템 슬롯 id가 0으로 시작할경우
                    {
                        switch (targetItemSlot.item.id[1]) // 가방의 아이템이 어떤 장비인지
                        {
                            case '1':
                                targetType = EquipmentType.MAINWEAPON;
                                break;
                            case '2':
                                targetType = EquipmentType.SUBWEAPON;
                                break;
                            case '3':
                                targetType = EquipmentType.HELMET;
                                break;
                            case '4':
                                targetType = EquipmentType.ARMOR;
                                break;
                            case '5':
                                targetType = EquipmentType.PANTS;
                                break;
                            default:
                                rect.anchoredPosition = Vector2.zero;
                                return;
                        }
                        if (sourceType == targetType) //장비의 타입이 같을 경우
                        {
                            Debug.Log("장비를 인벤토리의 다른 장비로");
                            Item targetItem = targetItemSlot.item;
                            int temp = targetItemSlot.ea;
                            targetItemSlot.ea = itemSlot.ea;
                            switch (sourceType) // 현재 선택한 장비가 무엇인지
                            {
                                case EquipmentType.HELMET:
                                    PlayableCharacter.Inst.SetHelmet(itemSlot.item);
                                    break;
                                case EquipmentType.ARMOR:
                                    PlayableCharacter.Inst.SetArmor(itemSlot.item);
                                    break;
                                case EquipmentType.PANTS:
                                    PlayableCharacter.Inst.SetPants(itemSlot.item);
                                    break;
                                case EquipmentType.MAINWEAPON:
                                    PlayableCharacter.Inst.SetMainWeapon(itemSlot.item);
                                    break;
                                case EquipmentType.SUBWEAPON:
                                    PlayableCharacter.Inst.SetSubWeapon(itemSlot.item);
                                    break;
                            }
                            itemSlot.item = targetItem;
                            itemSlot.ea = temp;
                            inventoryData.InvokeBackpackChanged(targetIndex, targetItemSlot);
                            rect.anchoredPosition = Vector2.zero;
                            return;
                        }
                        else // 장비의 타입이 다르므로 초기화
                        {
                            rect.anchoredPosition = Vector2.zero;
                            return;
                        }
                    }
                    else // 가방에 있는 아이템이 0으로 시작하지 않으므로 초기화
                    {
                        rect.anchoredPosition = Vector2.zero;
                        return;
                    }
                }
                else // 가방의 아이템을
                {
                    Debug.Log("가방의 아이템을 다른 슬롯으로");
                    Item newInstance = itemSlot.item;
                    Item targetInstance = targetItemSlot.item;
                    int temp = targetItemSlot.ea;
                    targetItemSlot.item = newInstance;
                    targetItemSlot.ea = itemSlot.ea;
                    itemSlot.item = targetInstance;
                    itemSlot.ea = temp;

                    inventoryData.InvokeBackpackChanged(index, itemSlot);
                    inventoryData.InvokeBackpackChanged(targetIndex, targetItemSlot);
                    rect.anchoredPosition = Vector2.zero;
                    return;
                }
            }
            else // 장비로
            {
                Debug.Log("장비로");
                if (itemSlot.item.id[0] == '0') // id가 0일 경우
                {
                    EquipmentType sourceType = EquipmentType.MAINWEAPON;
                    EquipmentType targetType = EquipmentType.MAINWEAPON;
                    //SourceType 책정
                    switch (itemSlot.item.id[1])
                    {
                        case '1':
                            sourceType = EquipmentType.MAINWEAPON;
                            break;
                        case '2':
                            sourceType = EquipmentType.SUBWEAPON;
                            break;
                        case '3':
                            sourceType = EquipmentType.HELMET;
                            break;
                        case '4':
                            sourceType = EquipmentType.ARMOR;
                            break;
                        case '5':
                            sourceType = EquipmentType.PANTS;
                            break;
                        default:
                            rect.anchoredPosition = Vector2.zero;
                            return;
                    }
                    //TargetType 책정
                    switch (targetIndex)
                    {
                        case 0:
                            targetType = EquipmentType.HELMET;
                            break;
                        case 1:
                            targetType = EquipmentType.ARMOR;
                            break;
                        case 2:
                            targetType = EquipmentType.PANTS;
                            break;
                        case 3:
                            targetType = EquipmentType.MAINWEAPON;
                            break;
                        case 4:
                            targetType = EquipmentType.SUBWEAPON;
                            break;
                    }
                    if (sourceType == targetType) // 같은 타입의 장비일 경우
                    {
                        Debug.Log("가방의 장비를 장비슬롯으로");
                        Item targetInstance = targetItemSlot.item;
                        int temp = targetItemSlot.ea;
                        switch (sourceType)
                        {
                            case EquipmentType.HELMET:
                                PlayableCharacter.Inst.SetHelmet(itemSlot.item);
                                break;
                            case EquipmentType.ARMOR:
                                PlayableCharacter.Inst.SetArmor(itemSlot.item);
                                break;
                            case EquipmentType.PANTS:
                                PlayableCharacter.Inst.SetPants(itemSlot.item);
                                break;
                            case EquipmentType.MAINWEAPON:
                                PlayableCharacter.Inst.SetMainWeapon(itemSlot.item);
                                break;
                            case EquipmentType.SUBWEAPON:
                                PlayableCharacter.Inst.SetSubWeapon(itemSlot.item);
                                break;
                        }
                        targetItemSlot.ea = itemSlot.ea;
                        itemSlot.item = targetInstance;
                        itemSlot.ea = temp;
                        rect.anchoredPosition = Vector2.zero;
                        inventoryData.InvokeBackpackChanged(index, itemSlot);
                        return;
                    }
                    else // 다른 타입의 장비이므로 초기화
                    {
                        rect.anchoredPosition = Vector2.zero;
                        return;
                    }
                }
                else // id가 0이 아니므로 초기화
                {
                    rect.anchoredPosition = Vector2.zero;
                    return;
                }
            }
        }
        else
        {
            rect.anchoredPosition = Vector2.zero;
        }
    }
    public void OnDrag(PointerEventData eventData)
    {
        if (itemSlot is null) return;
        iconTransform.position = eventData.position;
    }
}
