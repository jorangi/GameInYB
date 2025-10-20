using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TextMeshProUGUI))]
public class InteractableText : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMPLinkEvent e;
    [SerializeField] private Camera cam;
    [SerializeField] private bool enableHover = true;

    private TextMeshProUGUI source;
    private Canvas canvas;
    private bool isOver;
    private int index = -1;
    /// <summary>
    /// Awake시 Canvas, Camera 등록
    /// </summary>
    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (cam is null && canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }
    }
    /// <summary>
    /// 검증을 통해 TMPUGUI가 존재하지 않을 경우 등록
    /// </summary>
    private void OnValidate()
    {
        if (source is null) source = GetComponent<TextMeshProUGUI>();
    }
    /// <summary>
    /// 실제 Link 감지 처리로직
    /// </summary>
    private void Update()
    {

    }

    public void OnPointerDown(PointerEventData eventData)
    {
        source.ForceMeshUpdate();
        Camera c = eventData.pressEventCamera != null ? eventData.pressEventCamera : cam;
        index = TMP_TextUtilities.FindIntersectingLink(source, eventData.position, c);
        if (index != -1) {
            Raise(TMPLinkEvent.EventType.CLICK, eventData.position, c, eventData.pointerId);
        }
    }
    public void OnPointerUp(PointerEventData eventData)
    {

    }
    public void OnPointerEnter(PointerEventData eventData) => isOver = true;

    public void OnPointerExit(PointerEventData eventData)
    {
        isOver = false;
        if (index != -1)
        {
            Raise(TMPLinkEvent.EventType.MOUSEOUT, eventData.position, eventData.pressEventCamera);
            index = -1;
        }
    }
    private void Raise(TMPLinkEvent.EventType type, Vector2 screenPos, Camera c, int pointerId = -1)
    {
        if (e is null) return;
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(source, screenPos, c ?? cam);
        string linkId = null;
        string linkText = null;
        if (linkIndex != -1)
        {
            TMP_LinkInfo info = source.textInfo.linkInfo[linkIndex];
            linkId = info.GetLinkID();
            linkText = source.text.Substring(info.linkIdFirstCharacterIndex, info.linkTextLength);
        }
        TMPLinkEvent.TMPLinkEventPayload payload = new TMPLinkEvent.TMPLinkEventPayload
        {
            type = type,
            id = linkId,
            linkText = linkText,
            screenPos = screenPos,
            cam = c,
            can = canvas,
            source = source,
            index = pointerId
        };
        e.Raise(payload);
    }
}