using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemInformationModal : MoveableInformationModal, IPointerClickHandler
{
    #region Legacy
    private TextMeshProUGUI source;
    private void Awake()
    {
        source = transform.Find("description").GetComponent<TextMeshProUGUI>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        int index = TMP_TextUtilities.FindIntersectingLink(source, eventData.position, eventData.pressEventCamera);
        if(index == -1) return;
        TMP_LinkInfo info = source.textInfo.linkInfo[index];
        Debug.Log(info.GetLinkID());
    }
    #endregion
    public void Show(string content, Vector2 screenPos, Camera cam)
    {
    }
    public void SetText(string content)
    {

    }
    public override void Move(Vector2 screenPos, Camera cam)
    {

    }

}
