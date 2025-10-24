using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System;

public class TitleManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup loginPanel;
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;
    public Button loginButton;
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
        if (optionsPanel != null)
            optionsPanel.SetActive(false);
    }

    private async void Start()
    {
        loginManager = FindAnyObjectByType<LoginAndStatsManager>();

        if (loginManager == null)
        {
            Debug.LogError("[TitleManager] LoginAndStatsManager를 찾을 수 없습니다!");
            return;
        }

        loginButton.onClick.AddListener(OnClick_Login);
        startButton.onClick.AddListener(OnClick_StartGame);
        optionButton.onClick.AddListener(ToggleOptionsPanel);

        await FadeIn();

        // 자동 로그인 시도
        var data = PlayableCharacter.Inst?.Data;
        if (data != null && !string.IsNullOrEmpty(data.accessToken))
        {
            messageText.text = "자동 로그인 중...";
            try
            {
                await UniTask.Delay(500);
                await loginManager.InitializeAutoLogin(); // ✅ 이 함수를 새로 추가할 거야
                OnLoginSuccess();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"자동 로그인 실패: {e.Message}");
                messageText.text = "자동 로그인 실패";
            }
        }
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
            await loginManager.TryLogin(id, pw); // ✅ 이 함수도 LoginAndStatsManager에 추가할거야
            OnLoginSuccess();
        }
        catch (Exception e)
        {
            Debug.LogError($"[TitleManager] 로그인 실패: {e.Message}");
            messageText.text = "로그인 실패. 다시 시도하세요.";
        }
    }

    private void OnLoginSuccess()
    {
        messageText.text = "로그인 성공!";
        loginPanel.alpha = 0;
        loginPanel.interactable = false;
        loginPanel.blocksRaycasts = false;

        startButton.gameObject.SetActive(true);
        Debug.Log("[TitleManager] 로그인 성공 — Start 버튼 활성화");
    }

    private async void OnClick_StartGame()
    {
        await FadeOut();
        SceneManager.LoadScene("MainScene");
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

    private async UniTask FadeOut()
    {
        if (fadePanel == null) return;

        Color c = fadePanel.color;
        while (c.a < 1)
        {
            c.a += Time.deltaTime * fadeSpeed;
            fadePanel.color = c;
            await UniTask.Yield();
        }
    }

    private void ToggleOptionsPanel()
    {
        if (optionsPanel == null) return;
        bool active = !optionsPanel.activeSelf;
        optionsPanel.SetActive(active);
        Debug.Log($"[OPTION] {(active ? "열림" : "닫힘")}");
    }
}
