using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemSlotObject : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IDragAndDropHandler
{
    [SerializeField]private int index;
    bool equiped = false;
    private Inventory inventory;
    private void Awake()
    {
        inventory = PlayableCharacter.Inst.Inventory;

        if (transform.parent.name.Equals("Backpack"))
            index = transform.GetSiblingIndex() + 5;
        else
        {
            index = transform.parent.GetSiblingIndex();
            equiped = true;
        }

    }
    public DragAndDropVisualMode dragAndDropVisualMode => throw new System.NotImplementedException();

    public bool AcceptsDragAndDrop()
    {
        return false;
    }

    public void DrawDragAndDropPreview()
    {
    }

    public void ExitDragAndDrop()
    {
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        inventory.NotifySlotClicked(equiped ? index : index - 5);        
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
    }

    public void OnPointerExit(PointerEventData eventData)
    {
    }

    public void PerformDragAndDrop()
    {
    }

    public void UpdateDragAndDrop()
    {
    }
}
