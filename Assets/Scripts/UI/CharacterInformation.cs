using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CharacterInformation : MonoBehaviour, IUI
{
    [SerializeField]private UIContext uiContext;
    private Stack<MoveableInformationModal> modals = new();
    private void Awake()
    {
        uiContext = uiContext != null ? uiContext : GetComponentInParent<UIContext>();
        uiContext.uiRegistry.Register(this, UIType.CHARACTER_INFORMATION);
        gameObject.SetActive(false);
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
    }
}
