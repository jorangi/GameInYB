using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
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
    //private  
    InputSystem_Actions inputAction;
    #endregion

    private void Awake()
    {
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
    }
    private void OnDisable()
    {
        inputAction.Disable();
        if (linkEvent != null) linkEvent.OnRaised -= OnLinkEvent;
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
    private void MoveItemInfo(Vector2 screenPos, Camera eventCam) {
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
        if (keywordContainer == null) {
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

        if(top.transform.parent != keywordContainer) top.transform.SetParent(keywordContainer, false);
        keywordModalPooling.Enqueue(top);
    }
}
