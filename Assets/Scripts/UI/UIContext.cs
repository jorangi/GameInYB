using UnityEngine;

[RequireComponent(typeof(UIManager))]

/// <summary>
/// IUIRegistry를 주입하기 위한 컨텍스트
/// </summary>
public class UIContext : MonoBehaviour
{
    private IUIRegistry uiRegistry;
    public IUIRegistry UIRegistry
    {
        get
        {
            uiRegistry ??= GetComponent<UIManager>();
            return uiRegistry;
        }
    }
}
