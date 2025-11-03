using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System;
using System.Threading;

public interface ITitleManager
{
    public void OnLoginSuccess();
    public void SetMessageText(string text);
    public void LoginPanelShow(bool show = true);
}
public class TitleManager : MonoBehaviour, ITitleManager
{
    [Header("UI References")]
    public CanvasGroup loginPanel;
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;
    public Button loginButton;
    public Button logoutButton;
    public Button startButton;
    public Button optionButton;
    public GameObject optionsPanel;
    public Image fadePanel;
    public float fadeSpeed = 1.5f;

    private LoginAndStatsManager loginManager;

    private void Awake()
    {
        if (fadePanel != null)
            fadePanel.color = new Color(0, 0, 0, 1);

        startButton.gameObject.SetActive(false);
        logoutButton.gameObject.SetActive(false);
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }
    private async void Start()
    {
        loginManager = FindAnyObjectByType<LoginAndStatsManager>();

        if (loginManager == null)
        {
            Debug.LogError("LoginAndStatsManager가 없는것으로보임");
            return;
        }

        loginButton.onClick.AddListener(OnClick_Login);
        logoutButton.onClick.AddListener(OnClick_Logout);
        await FadeIn();


    }
    private async void OnClick_Login()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            messageText.text = "ID/PW 입력 필요";
            return;
        }

        try
        {
            messageText.text = "로그인 중...";
            await loginManager.TryLogin(id, pw);
        }
        catch (Exception e)
        {
            Debug.LogError($"[TitleManager] 로그인 실패: {e.Message}");
            messageText.text = "로그인 실패. 다시 시도하세요.";
            return;
        }
    }
    private void OnClick_Logout()
    {
        messageText.text = "";
        idInput.text = "";
        pwInput.text = "";
        loginPanel.alpha = 1;
        loginPanel.interactable = true;
        loginPanel.blocksRaycasts = true;
        startButton.gameObject.SetActive(false);
        logoutButton.gameObject.SetActive(false);
    }
    public void OnLoginSuccess()
    {
        messageText.text = "로그인 성공!";
        loginPanel.alpha = 0;
        loginPanel.interactable = false;
        loginPanel.blocksRaycasts = false;

        startButton.gameObject.SetActive(true);
        logoutButton.gameObject.SetActive(true);
    }
    public string sceneName = "CoreScene";
    public SceneTransition c;
    public async void StartGame()
    {
        c.gameObject.SetActive(true);
        await UniTask.WaitUntil(() => c.end);
        SceneManager.LoadScene(sceneName);
    }
    private async UniTask FadeIn()
    {
        if (fadePanel == null) return;

        Color c = fadePanel.color;
        while (c.a > 0)
        {
            c.a -= Time.deltaTime * fadeSpeed;
            fadePanel.color = c;
            await UniTask.Yield();
        }
    }
    public void SetMessageText(string text)
    {
        messageText.text = text;
    }
    public void LoginPanelShow(bool show = true)
    {
        loginPanel.alpha = show ? 1 : 0;
        loginPanel.interactable = show;
        loginPanel.blocksRaycasts = show;
    }
}
