using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class PausedMenu : MonoBehaviour, IUI
{
    [SerializeField] private UIContext uiContext;
    private void Awake()
    {
        uiContext = uiContext != null ? uiContext : GetComponentInParent<UIContext>();
        uiContext.UIRegistry.Register(this, UIType.PAUSED_MENU);
        gameObject.SetActive(false);
    }
    public async void Hide()
    {
        await PlayableCharacter.ReadyAsync(this.GetCancellationTokenOnDestroy());
        gameObject.SetActive(false);
        Time.timeScale = PlayableCharacter.Inst.gameTimeScale;
        uiContext.UIRegistry.CloseUI(this);
    }
    public void NegativeInteract(InputAction.CallbackContext context)
    {
        if (transform.GetChild(1).gameObject.activeSelf)
        {
            transform.GetChild(1).gameObject.SetActive(false);
            transform.GetChild(0).gameObject.SetActive(true);
            return;
        }
        Hide();
    }
    public void PositiveInteract(InputAction.CallbackContext context)
    {
        Show();
    }
    public void OnSettings()
    {
        transform.GetChild(0).gameObject.SetActive(false);
        transform.GetChild(1).GetComponent<OptionsUIManager>().Show();
    }
    public void Show()
    {
        gameObject.SetActive(true);
        Time.timeScale = 0.0f;
    }
    public async UniTask OnSave()
    {
        var saver = ServiceHub.Get<IStatsSaver>();

        if (saver == null)
        {
            Debug.LogWarning("[SaveCommand] IStatsSaver가 ServiceProvider에 없습니다. 폴백 인스턴스를 생성합니다.");

            var tokenProvider = new PlayableCharacterAccessTokenProvider();
            var refreshers = new IStatsRefresher[]
            {
                new LoginServiceStatsRefresher()
            };
            saver = new StatsSaver(tokenProvider, refreshers);
        }
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        {
            Debug.LogError("[SaveCommand] PlayableCharacter.Inst 또는 Inst.Data가 null입니다.");
            return;
        }
        var statsDto = PlayableCharacter.Inst.Data.ToDto(PlayableCharacter.Inst.Snapshot());
        var ok = await saver.SavePlayerStatsAsync(statsDto);
        if (!ok)
        {
            Debug.LogWarning("[SaveCommand] SavePlayerStatsAsync 실패.");
        }
    }
    public async void OnQuit()
    {
        await OnSave();
        var f = FindAnyObjectByType<SceneTransition>();
        await UniTask.WaitUntil(() => f.end);
        Application.Quit();
    }
}