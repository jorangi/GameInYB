using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class ItemInformationModal : MoveableInformationModal
{
    private TextMeshProUGUI source;
    private void Awake()
    {
        source = transform.Find("description").GetComponent<TextMeshProUGUI>();
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
