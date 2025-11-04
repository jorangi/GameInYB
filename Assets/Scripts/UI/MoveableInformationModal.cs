using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MoveableInformationModal : MonoBehaviour, IUI, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private RectTransform rect;
    [SerializeField]private UIContext uiContext;
    [SerializeField]protected TextMeshProUGUI title, context;
    private WaitForSeconds _wait = new(0.05f);
    public Coroutine hideCoroutine = null;

    private void Awake()
    {
        rect = rect != null ? rect : GetComponent<RectTransform>();
        uiContext = FindAnyObjectByType<UIContext>();
    }
    private void Start()
    {
        //uiContext.UIRegistry.Register(this, UIType.KEYWORD_MODAL);
    }
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }
    public virtual void Show(Transform parent)
    {
        transform.SetParent(parent);
        Show();
    }
    public virtual void Hide()
    {
        if (!gameObject.activeSelf) return;
        CancleHide();
        hideCoroutine = StartCoroutine(Hide_Coroutine());
    }
    public void CancleHide()
    {
        if (hideCoroutine is not null) StopCoroutine(hideCoroutine);
    }
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
        if (transform.parent.name != "Canvas")
        {
            transform.parent.position = offset;
        }
    }
    public IEnumerator Hide_Coroutine()
    {
        yield return _wait;
        gameObject.SetActive(false);
        hideCoroutine = null;
    }
    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        CancleHide();
    }
    public virtual void OnPointerExit(PointerEventData eventData)
    {
        Hide();
    }
}
