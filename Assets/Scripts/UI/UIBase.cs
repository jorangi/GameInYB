using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// UI 객체 인터페이스
/// </summary>
public interface IUI
{
    /// <summary>
    /// UI를 보여주는 함수
    /// </summary>
    public void Show();
    /// <summary>
    /// UI를 숨기는 함수
    /// </summary> <summary>
    /// 
    /// </summary>
    public void Hide();
    /// <summary>
    /// UI에 긍정 상호작용을 하는 함수
    /// </summary>
    /// <param name="context"></param>
    public void PositiveInteract(InputAction.CallbackContext context);
    /// <summary>
    /// UI에 부정 상호작용을 하는 함수
    /// </summary>
    /// <param name="context"></param>
    public void NegativeInteract(InputAction.CallbackContext context);
}
