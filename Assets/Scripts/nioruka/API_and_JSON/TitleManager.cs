using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleManager : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup loginPanel;
    public Button loginButton;
    public Button startButton;
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;
    public Image fadePanel;
    public float fadeSpeed = 1.5f;

    private LoginAndStatsManager api;

    void Start()
    {
        api = FindObjectOfType<LoginAndStatsManager>();

        startButton.gameObject.SetActive(false);
        loginPanel.alpha = 1;
        fadePanel.color = new Color(0, 0, 0, 1);

        StartCoroutine(FadeIn());

        // 자동 로그인
        if (LoginAndStatsManager.currentPlayer != null &&
            !string.IsNullOrEmpty(LoginAndStatsManager.currentPlayer.accessToken))
        {
            messageText.text = "자동 로그인";
            loginPanel.alpha = 0;
            startButton.gameObject.SetActive(true);
        }
    }

    IEnumerator FadeIn()
    {
        while (fadePanel.color.a > 0)
        {
            Color c = fadePanel.color;
            c.a -= Time.deltaTime * fadeSpeed;
            fadePanel.color = c;
            yield return null;
        }
    }

    public void OnClick_Login()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            messageText.text = "ID/PW 입력 필요";
            return;
        }

        messageText.text = "로그인 중...";
        StartCoroutine(LoginAndShowStartButton(id, pw));
    }

    private IEnumerator LoginAndShowStartButton(string id, string pw)
    {
        yield return StartCoroutine(api.LoginRequest(id, pw));

        if (LoginAndStatsManager.currentPlayer != null &&
            !string.IsNullOrEmpty(LoginAndStatsManager.currentPlayer.accessToken))
        {
            Debug.Log("TitleScene 로그인 성공 — UI 갱신 ");
            messageText.text = "로그인 성공!";
            loginPanel.alpha = 0;
            startButton.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("TitleScene 로그인 실패 — 토큰 없음");
            messageText.text = "로그인 실패";
        }
    }

    public void OnClick_StartGame()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    IEnumerator FadeOutAndLoad()
    {
        while (fadePanel.color.a < 1)
        {
            Color c = fadePanel.color;
            c.a += Time.deltaTime * fadeSpeed;
            fadePanel.color = c;
            yield return null;
        }

        SceneManager.LoadScene("MainScene"); //임시
    }
}
