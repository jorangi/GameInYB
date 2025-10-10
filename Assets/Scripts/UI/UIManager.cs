using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using JetBrains.Annotations;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;


public enum UIType
{
    CHARACTER_INFORMATION,
    COMMAND_PANEL,
    KEYWORD_MODAL,
    PAUSED_MENU,
    NONE
}
public interface IUIRegistry
{
    public void Register(IUI ui, UIType type = UIType.NONE);
    public void Unregister(IUI ui);
    public void Focus(IUI ui);
    public void CloseUI(IUI ui);
}
public interface INegativeSignal
{
    public event Action Negative;
}
public class UIManager : MonoBehaviour, IUIRegistry, INegativeSignal
{
    #region field
    #region LinkEvent & Scope
    [Header("LinkEvent & Scope")]
    [SerializeField] private TMPLinkEvent linkEvent;
    [SerializeField] private Canvas canvas;
    #endregion
    #region Moveable Modal
    [Header("Moveable Modal")]
    [SerializeField] private ItemInformationModal itemModal;
    #endregion
    #region Keyword Static Modal
    [Header("Keyword static modal")]
    [SerializeField] private KeywordInformationModalController keywordModalPrefab;
    [SerializeField] private RectTransform keywordContainer;
    [Min(0)][SerializeField] private int keywordModalPoolingMax = 2;
    #endregion
    #region Backdrop / Input
    [Header("Backdrop / Input")]
    [SerializeField] private GameObject backdrop;
    #endregion
    #region Placement / Camera
    [Header("Placement")]
    [SerializeField] private Camera cam;
    #endregion
    private RectTransform canvasRect;
    private readonly Stack<KeywordInformationModalController> keywordModalStack = new();
    private readonly Dictionary<string, KeywordInformationModalController> keywordModalByTerm = new();
    private readonly Queue<KeywordInformationModalController> keywordModalPooling = new();
    private bool shown;
    public static InputSystem_Actions inputAction;
    public List<IUI> uiList = new();
    private readonly Dictionary<UIType, IUI> uiDic = new();
    private bool keyInput = false;
    public event Action Negative;
    #endregion

    private void Awake()
    {
        if (inputAction == null)
        {
            inputAction = new();
            inputAction.UserInterface.Enable();
        }
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && cam == null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            if (itemModal != null) itemModal.Hide();
            if (backdrop != null) backdrop.SetActive(false);
            KeywordModalPooling(keywordModalPoolingMax);
        }
    }
    private void OnEnable()
    {
        inputAction.Enable();
        if (linkEvent != null) linkEvent.OnRaised += OnLinkEvent;
        inputAction.UserInterface.ClickAction.performed += OnClickAction;
        inputAction.UserInterface.Command.performed += OnCommand;
        inputAction.UserInterface.CharacterInformation.performed += OnCharacterInformation;
        inputAction.UserInterface.Positive.performed += OnPositive;
        inputAction.UserInterface.Negative.performed += KeyInput;
        inputAction.UserInterface.Negative.performed += OnNegative;
        inputAction.UserInterface.PausedMenu.performed += OnPausedMenu;
    }
    private void KeyInput(InputAction.CallbackContext context) => keyInput = true;
    private List<RaycastResult> raycastResults = new();
    /// <summary>
    /// 클릭 상호작용(UI 외부 마우스 클릭 감지)
    /// </summary>
    /// <param name="context"></param>
    private void OnClickAction(InputAction.CallbackContext context)
    {
        if (uiList.Count == 0) return;
        PointerEventData pointer = new(EventSystem.current)
        {
            position = Mouse.current.position.ReadValue()
        };
        EventSystem.current.RaycastAll(pointer, raycastResults);
        // 일시정지 메뉴가 열려있을 때
        if (uiList.Contains(uiDic[UIType.PAUSED_MENU]))
        {
            PausedMenu menu = null;
            foreach (var r in raycastResults)
            {
                if (r.gameObject.transform.IsChildOf((uiDic[UIType.PAUSED_MENU] as MonoBehaviour).transform))
                {
                    menu = uiDic[UIType.PAUSED_MENU] as PausedMenu;
                    break;
                }
            }
            if (menu == null && uiList[^1] is PausedMenu) uiList[^1].NegativeInteract(context);
            return;
        }
        // 키워드 모달이 열려있을 때
        else if (uiList.Contains(uiDic[UIType.KEYWORD_MODAL]))
        {
            MoveableInformationModal modal = null;
            foreach (var r in raycastResults)
            {
                if (r.gameObject.TryGetComponent<MoveableInformationModal>(out modal))
                    break;
            }
            if (modal == null && uiList[^1] is MoveableInformationModal) uiList[^1].NegativeInteract(context);
        }
    }
    /// <summary>
    /// 명령어 창을 여는 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnCommand(InputAction.CallbackContext context)
    {
        if (!uiList.Contains(uiDic[UIType.PAUSED_MENU]) && uiDic.TryGetValue(UIType.COMMAND_PANEL, out IUI ui))
        {
            if (uiList.Contains(ui))
            {
                ui.Hide();
                return;
            }
            ui.Show();
            uiList.Add(ui);
        }
    }
    /// <summary>
    /// 일시정지 메뉴를 여는 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnPausedMenu(InputAction.CallbackContext context)
    {
        if (!keyInput) return;
        keyInput = false;
        if (Time.deltaTime > 0f && uiList.Count == 0 && uiDic.TryGetValue(UIType.PAUSED_MENU, out IUI ui))
        {
            ui.Show();
            uiList.Add(ui);
        }
    }
    /// <summary>
    /// 캐릭터 정보 창을 여는 함수
    /// </summary>
    /// <param name="context"></param>
    private void OnCharacterInformation(InputAction.CallbackContext context)
    {
        if (!uiList.Contains(uiDic[UIType.PAUSED_MENU]) && !uiList.Contains(uiDic[UIType.COMMAND_PANEL])  && uiDic.TryGetValue(UIType.CHARACTER_INFORMATION, out IUI ui))
        {
            ui.Show();
            uiList.Add(ui);
        }
    }
    private void OnDisable()
    {
        inputAction?.Disable();
        if (linkEvent != null) linkEvent.OnRaised -= OnLinkEvent;

        inputAction.UserInterface.Positive.performed += OnPositive;
        inputAction.UserInterface.Negative.performed += OnNegative;
    }
    /// <summary>
    /// UI 긍정 상호작용
    /// </summary>
    /// <param name="context"></param>
    private void OnPositive(InputAction.CallbackContext context)
    {
    }
    /// <summary>
    /// UI 부정 상호작용
    /// </summary>
    /// <param name="context"></param>
    public void OnNegative(InputAction.CallbackContext context)
    {
        Negative?.Invoke();
        if (uiList.Count == 0 || !keyInput) return;
        IUI ui = uiList[^1];
        ui.NegativeInteract(context);
        keyInput = false;
    }
    private void OnLinkEvent(TMPLinkEvent.TMPLinkEventPayload payload)
    {
        if (payload.can != null && payload.can != canvas) return;
        switch (payload.type)
        {
            case TMPLinkEvent.EventType.MOUSEOVER:
                HandleMouseOver(payload);
                break;
            case TMPLinkEvent.EventType.MOUSEOUT:
                HandleMouseOut(payload);
                break;
            case TMPLinkEvent.EventType.CLICK:
                HandleClick(payload);
                break;
        }
    }
    private void HandleMouseOver(TMPLinkEvent.TMPLinkEventPayload payload)
    {
        string content = !string.IsNullOrEmpty(payload.id) ? payload.id : payload.linkText;
        if (string.IsNullOrEmpty(content)) return;
        ShowItemInfo(content, payload.screenPos, payload.cam);
    }
    private void ShowItemInfo(string content, Vector2 screenPos, Camera eventCam)
    {
        if (itemModal == null) return;
        if (shown)
        {
            itemModal.Show(content, screenPos, eventCam != null ? eventCam : cam);
            shown = true;
        }
        else
        {
            itemModal.SetText(content);
            itemModal.Move(screenPos, eventCam != null ? eventCam : cam);
        }
    }
    private void MoveItemInfo(Vector2 screenPos, Camera eventCam)
    {
        if (itemModal == null || shown) return;
        itemModal.Move(screenPos, eventCam != null ? eventCam : cam);
    }
    private void HandleMouseOut(TMPLinkEvent.TMPLinkEventPayload payload)
    {
        HideItemInfo();
    }
    private void HideItemInfo()
    {
        if (itemModal == null) return;
        if (shown)
        {
            itemModal.Hide();
            shown = false;
        }
    }
    private void HandleClick(TMPLinkEvent.TMPLinkEventPayload payload)
    {
        string termId = !string.IsNullOrEmpty(payload.id) ? payload.id : payload.linkText;
        if (string.IsNullOrEmpty(termId)) return;
        SpawnKeywordInfo(termId, payload.screenPos, cam);
    }
    private KeywordInformationModalController SpawnKeywordInfo(string termId, Vector2 screenPos, Camera cam)
    {
        if (keywordModalPrefab == null)
        {
            Debug.LogWarning("keywordModalPrefab(KeywordInformationModalController)이 없음");
            return null;
        }
        if (keywordContainer == null)
        {
            Debug.LogWarning("keywordContainer(RectTransform)이 없음");
            return null;
        }
        if (keywordModalByTerm.TryGetValue(termId, out KeywordInformationModalController existingModal))
        {
            Focus(existingModal);
            return existingModal;
        }
        KeywordInformationModalController modal = GetKeywordModalInstance();
        modal.transform.SetParent(keywordContainer, false);
        modal.transform.SetAsLastSibling();
        modal.gameObject.SetActive(true);

        modal.Show(termId, screenPos, cam != null ? cam : this.cam);

        keywordModalStack.Push(modal);
        keywordModalByTerm[termId] = modal;
        SetBackdrop(true);

        return modal;
    }
    private void SetBackdrop(bool v)
    {
        if (backdrop == null) return;
        backdrop.SetActive(v);
        if (v) backdrop.transform.SetAsLastSibling();
    }
    private KeywordInformationModalController GetKeywordModalInstance()
    {
        if (keywordModalPooling.Count > 0) return keywordModalPooling.Dequeue();
        return Instantiate(keywordModalPrefab);
    }
    private void Focus(KeywordInformationModalController existingModal)
    {
        if (existingModal == null) return;
        existingModal.Focus();
        existingModal.transform.SetAsLastSibling();
        SetBackdrop(true);
    }
    private void CloseTopKeywordModal()
    {
        if (keywordModalStack.Count == 0) return;

        KeywordInformationModalController top = keywordModalStack.Pop();
        string keyToRemove = null;
        foreach (var m in keywordModalByTerm)
        {
            if (m.Value == top)
            {
                keyToRemove = m.Key;
                break;
            }
        }
        if (!string.IsNullOrEmpty(keyToRemove)) keywordModalByTerm.Remove(keyToRemove);

        CloseAndRecycle(top);
        if (keywordModalStack.Count == 0) SetBackdrop(false);
        else Focus(keywordModalStack.Peek());
    }
    private void CloseAllKeywordModal()
    {
        while (keywordModalStack.Count > 0)
        {
            var m = keywordModalStack.Pop();
            CloseAndRecycle(m);
        }
        keywordModalByTerm.Clear();
        SetBackdrop(false);
    }
    private void FocusTopKeywordModal()
    {
        if (keywordModalStack.Count == 0) return;
        Focus(keywordModalStack.Peek());
    }
    private void OnBackdropClicked()
    {
        CloseTopKeywordModal();
    }
    private void KeywordModalPooling(int count)
    {
        if (keywordModalPrefab == null)
        {
            Debug.LogWarning("keywordModalPrefab(KeywordInformationModalController)이 없음");
        }
        if (keywordContainer == null)
        {
            Debug.LogWarning("keywordContainer(RectTransform)이 없음");
        }
        for (int i = 0; i < count; i++)
        {
            var m = Instantiate(keywordModalPrefab, keywordContainer);
            m.gameObject.SetActive(false);
            keywordModalPooling.Enqueue(m);
        }
    }
    private void CloseAndRecycle(KeywordInformationModalController top)
    {
        if (top == null) return;
        top.Hide();

        if (top.transform.parent != keywordContainer) top.transform.SetParent(keywordContainer, false);
        keywordModalPooling.Enqueue(top);
    }
    /// <summary>
    /// UI를 등록하는 함수
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="type"></param>
    public void Register(IUI ui, UIType type = UIType.NONE)
    {
        if (ui == null) return;
        if (type != UIType.NONE && !uiDic.ContainsKey(type)) uiDic[type] = ui;
        ui.Hide();
    }
    /// <summary>
    /// UI 등록을 해제하는 함수
    /// </summary>
    /// <param name="ui"></param> <summary>
    /// 
    /// </summary>
    /// <param name="ui"></param>
    public void Unregister(IUI ui)
    {
    }
    /// <summary>
    /// UI에 포커스를 주는 함수
    /// </summary>
    /// <param name="ui"></param> <summary>
    /// 
    /// </summary>
    /// <param name="ui"></param>
    public void Focus(IUI ui)
    {
    }
    /// <summary>
    /// UI를 닫는 함수(uiList에서 제거)
    /// </summary>
    /// <param name="ui"></param>
    public void CloseUI(IUI ui)
    {
        uiList.Remove(ui);
    }
}