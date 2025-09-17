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
        Debug.DrawLine(Input.mousePosition, Input.mousePosition + Vector3.left * 10, Color.red);
        Debug.Log(index);
    }
    #endregion
    public void Show(string content, Vector2 screenPos, Camera cam)
    {
        base.Show();
    }
    public void SetText(string content)
    {

    }
    public override void Move(Vector2 screenPos, Camera cam)
    {
        
    }

}
