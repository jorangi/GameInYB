using UnityEngine;
using UnityEngine.InputSystem;

public class MoveableInformationModal : MonoBehaviour, IUI
{
    private UIContext uiContext;
    private void Start()
    {
        uiContext = FindAnyObjectByType<UIContext>();
        uiContext.uiRegistry.Register(this, UIType.KEYWORD_MODAL);
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }
    public void PositiveInteract(InputAction.CallbackContext context)
    {
        
    }
    public void NegativeInteract(InputAction.CallbackContext context)
    {
        Hide();
    }
    public virtual void Move(Vector2 screenPos, Camera cam)
    {

    }
    public virtual void SetFollow(bool enabled = true)
    {

    }
    public virtual void SetOffset(Vector2 offset)
    {
        
    }
}
