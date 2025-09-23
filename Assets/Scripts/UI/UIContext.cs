using UnityEngine;

[RequireComponent(typeof(UIManager))]

/// <summary>
/// IUIRegistry를 주입하기 위한 컨텍스트
/// </summary>
public class UIContext : MonoBehaviour
{
    public IUIRegistry uiRegistry;

    private void Awake()
    {
        uiRegistry = GetComponent<UIManager>();
        Debug.Log(uiRegistry);
    }
}
