using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
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
    // KEYWORD_MODAL,
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
public interface IModalController
{
    public ItemInformationModal[] Modals { get; set; }
    public UniTask SpawnModal(bool isParent, string title, string ctx, Vector2 pos);
}
public class ModalController : IModalController
{
    public ModalController()
    {
        Modals = new ItemInformationModal[4];
        modalCount = 0;
    }
    public bool isShowing = false;
    public ItemInformationModal[] Modals { get; set; }
    int modalCount = 0;
    public async UniTask SpawnModal(bool isParent, string title, string ctx, Vector2 pos)
    {
        if (isParent)
        {
            await HideModal();
            Modals[0].Show(title, ctx, pos);
        }
        else
        {
            modalCount = ++modalCount == 4 ? 1 : modalCount;
            Modals[modalCount].Show(title, ctx, Vector2.zero);
        }
        isShowing = true;
    }
    //readonly UniTask delayTask = UniTask.Delay(50); // 50ms 대기
    /// <summary>
    /// 모달창 숨기기
    /// </summary>
    /// <returns></returns>
    public async UniTask HideModal()
    {
        isShowing = false;
        int check = await UniTask.WhenAny(UniTask.Delay(50)/*delayTask*/, UniTask.WaitUntil(() => isShowing == true)); //시간 대기 후 숨김 0 반환, 만일 중간에 isShowing이 true일 경우 1 반환
        if (check == 0)
        {
            //Hide
            for (int i = Modals.Length - 1; i >= 0; i--) //역순으로 모달창 비활성화
            {
                Modals[i].gameObject.SetActive(false);
            }
        }
        else if (check == 1)
        {
            return;
        }
        modalCount = 0;
    }
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
    [SerializeField] private ItemInformationModal parentModal;
    public ItemInformationModal ParentModal{get => parentModal; set => parentModal = value;}
    [SerializeField] private ItemInformationModal itemModal;
    [SerializeField] private Transform keywordModalContainer;
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
    public static InputSystem_Actions inputAction;
    public List<IUI> uiList = new();
    private readonly Dictionary<UIType, IUI> uiDic = new();
    private bool keyInput = false;
    public event Action Negative;
    #endregion

    private void Awake()
    {
        modalController = new();
        cam = Camera.main;
        if (inputAction is null)
        {
            inputAction = new();
            inputAction.UserInterface.Enable();
        }
        if (canvas is null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay && cam == null)
        {
            canvasRect = canvas.GetComponent<RectTransform>();
            if (itemModal != null) itemModal.Hide();
            if (backdrop != null) backdrop.SetActive(false);
        }
        ModalSetup();
    }
    private void OnEnable()
    {
        inputAction.Enable();
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
    public ModalController modalController;

    /// <summary>
    /// 모달창 Pooling
    /// </summary>
    public void ModalSetup()
    {
        //부모 모달 1 + 자식 모달 (최대 3)
        for (int i = 0; i < 4; i++)
        {
            if (i == 0)
            {
                modalController.Modals[0] = ParentModal;
                ParentModal.ParentModal = ParentModal;
                ParentModal.modalController = modalController;
                ParentModal.gameObject.SetActive(false);
            }
            else
            {
                GameObject obj = Instantiate(itemModal.gameObject);
                obj.name = "ChildModal_" + i;
                modalController.Modals[i] = obj.GetComponent<ItemInformationModal>();
                modalController.Modals[i].ParentModal = modalController.Modals[0];
                modalController.Modals[i].modalController = modalController;
                obj.transform.SetParent(modalController.Modals[0].transform.parent.Find("KeywordModalContainer"));
                obj.SetActive(false);
            }
        }
    }
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
            if (menu is null && uiList[^1] is PausedMenu) uiList[^1].NegativeInteract(context);
            return;
        }
        // // 키워드 모달이 열려있을 때
        // else if (uiList.Contains(uiDic[UIType.KEYWORD_MODAL]))
        // {
        //     MoveableInformationModal modal = null;
        //     foreach (var r in raycastResults)
        //     {
        //         if (r.gameObject.TryGetComponent<MoveableInformationModal>(out modal))
        //             break;
        //     }
        //     if (modal is null && uiList[^1] is MoveableInformationModal) uiList[^1].NegativeInteract(context);
        // }
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
        if (!uiList.Contains(uiDic[UIType.PAUSED_MENU]) && !uiList.Contains(uiDic[UIType.COMMAND_PANEL]) && uiDic.TryGetValue(UIType.CHARACTER_INFORMATION, out IUI ui))
        {
            ui.Show();
            uiList.Add(ui);
        }
    }
    private void OnDisable()
    {
        inputAction?.Disable();

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
    /// <summary>
    /// UI를 등록하는 함수
    /// </summary>
    /// <param name="ui"></param>
    /// <param name="type"></param>
    public void Register(IUI ui, UIType type = UIType.NONE)
    {
        if (ui is null) return;
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