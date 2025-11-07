using UnityEngine;
using UnityEngine.EventSystems;

public class UIHoverClickSFX : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData) => AudioManager.Inst.PlaySFX("mousehover");
    public virtual void OnPointerClick(PointerEventData eventData) => AudioManager.Inst.PlaySFX("mouseclick");
}
