using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using Cysharp.Threading.Tasks;

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
    ILoginService loginSvc;
    private async void Start()
    {
        loginSvc = ServiceHub.Get<ILoginService>();

        startButton.gameObject.SetActive(false);
        loginPanel.alpha = 1;
        fadePanel.color = new Color(0, 0, 0, 1);

        StartCoroutine(FadeIn());
        try
        {
            bool autoLogin = await loginSvc.InitializeAsync(true);
            if (autoLogin)
            {
                messageText.text = "자동 로그인";
                loginPanel.alpha = 0;
                startButton.gameObject.SetActive(true);
            }
            else
            {
                //Debug.Log("자동 로그인 실패");
            }
        }
        catch
        {
            //Debug.Log("자동 로그인 실패");
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
        _ = LoginAndShowStartButton(id, pw);
    }
    private async UniTask LoginAndShowStartButton(string id, string pw)
    {
        try
        {
            await loginSvc.LoginAsync(id, pw);
            
            messageText.text = "로그인 성공!";
            loginPanel.alpha = 0;
            startButton.gameObject.SetActive(true);
        }
        catch
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

        SceneManager.LoadScene("TUTORIAL_MAP"); //임시
    }
}
