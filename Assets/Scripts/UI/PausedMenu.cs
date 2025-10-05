using UnityEngine;
using UnityEngine.InputSystem;

public class PausedMenu : MonoBehaviour, IUI
{
    [SerializeField] private UIContext uiContext;
    private void Awake()
    {
        uiContext = uiContext != null ? uiContext : GetComponentInParent<UIContext>();
        uiContext.UIRegistry.Register(this, UIType.PAUSED_MENU);
        gameObject.SetActive(false);
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        Time.timeScale = PlayableCharacter.Inst.gameTimeScale;
        uiContext.UIRegistry.CloseUI(this);
    }
    public void NegativeInteract(InputAction.CallbackContext context)
    {
        if (transform.GetChild(1).gameObject.activeSelf)
        {
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
            return;
        }
        Hide();
    }
    public void PositiveInteract(InputAction.CallbackContext context)
    {
        Show();
    }
    public void OnSettings()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).gameObject.SetActive(true);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0.0f;
    }
}
