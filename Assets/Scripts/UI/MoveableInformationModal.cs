using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MoveableInformationModal : MonoBehaviour, IUI, IPointerEnterHandler, IPointerExitHandler
{
    public ModalController modalController;
    public MoveableInformationModal ParentModal;
    [SerializeField] private RectTransform rect;
    [SerializeField]protected TextMeshProUGUI title, context;

    private void Awake()
    {
        rect = rect != null ? rect : GetComponent<RectTransform>();
    }
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Hide() => gameObject.SetActive(false);

    public void PositiveInteract(InputAction.CallbackContext context)
    {
        //Focus
    }
    public void NegativeInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
    public virtual void SetFollow(bool enabled = true)
    {

    }
    public virtual void SetOffset(Vector2 offset)
    {
        if (ParentModal == this)
        {
            transform.parent.position = offset;
        }
    }
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        modalController.isShowing = true;
    }
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _ = modalController.HideModal();
    }
}
