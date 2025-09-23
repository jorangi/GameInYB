using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI 객체 인터페이스
/// </summary>
public interface IUI
{
    public void Show();
    public void Hide();
    public void PositiveInteract(InputAction.CallbackContext context);
    public void NegativeInteract(InputAction.CallbackContext context);
}
