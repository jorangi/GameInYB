using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class IteractableText : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    private TextMeshProUGUI target;
    private void OnValidate()
    {
        if (target == null) target = GetComponent<TextMeshProUGUI>();
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        int index = TMP_TextUtilities.FindIntersectingLink(target, Input.mousePosition, Camera.main);
    }
    public void OnPointerUp(PointerEventData eventData)
    {

    }
}
