using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemInformationModal : MoveableInformationModal, IPointerClickHandler
{
    private TextMeshProUGUI source;
    private void Awake()
    {
        source = transform.Find("description").GetComponent<TextMeshProUGUI>();
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        int index = TMP_TextUtilities.FindIntersectingLink(source, eventData.position, eventData.pressEventCamera);
        if (index == -1) return;
        TMP_LinkInfo info = source.textInfo.linkInfo[index];
        Debug.Log(info.GetLinkID());
        Debug.Log(ItemDataManager.GetItem(info.GetLinkID()));
    }
    public void Show(string title, string context, Vector2 pos)
    {
        this.title.text = title;
        this.context.text = context;
        Show();
        SetOffset(pos);
    }
    public override void Hide()
    {
        base.Hide();
    }
    public override void OnPointerEnter(PointerEventData eventData)
    {
        base.OnPointerEnter(eventData);
    }
    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }
}
