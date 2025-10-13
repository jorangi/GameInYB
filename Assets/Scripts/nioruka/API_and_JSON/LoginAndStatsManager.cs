using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class LoginAndStatsManager : MonoBehaviour
{
    private const string LOGIN_URL = "https://api-looper.duckdns.org/api/login";
    private const string STATS_URL = "https://api-looper.duckdns.org/api/mypage/stats";

    [Header("Login UI")]
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;

    void Start()
    {
        string savedToken = PlayerPrefs.GetString("auth_token", "");
        if (!string.IsNullOrEmpty(savedToken))
        {
            Debug.Log("저장된 토큰 발견 Stats 호출");
            StartCoroutine(GetStats());
        }
    }

    public void OnClick_Login()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            messageText.text = "ID/PW 입력";
            return;
        }

        StartCoroutine(LoginRequest(id, pw));
    }

    IEnumerator LoginRequest(string id, string pw)
    {
        string jsonBody = JsonUtility.ToJson(new LoginData(id, pw));
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(LOGIN_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("로그인 실패: " + request.error);
                messageText.text = "로그인 실패";
            }
            else
            {
                string responseJson = request.downloadHandler.text;
                Debug.Log("로그인 응답: " + responseJson);

                TokenResponse tokenRes = JsonUtility.FromJson<TokenResponse>(responseJson);
                if (!string.IsNullOrEmpty(tokenRes.token))
                {
                    PlayerPrefs.SetString("auth_token", tokenRes.token);
                    PlayerPrefs.Save();

                    Debug.Log("JWT 저장 완료");
                    messageText.text = "로그인 성공";

                    StartCoroutine(GetStats());
                }
                else
                {
                    messageText.text = "로그인실패 (토큰 없음추정)";
                }
            }
        }
    }

    IEnumerator GetStats()
    {
        string token = PlayerPrefs.GetString("auth_token", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("토큰없음. 로그인 이후 시도");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(STATS_URL))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("API 호출 실패: " + request.error);
                Debug.LogError("응답 코드: " + request.responseCode);
            }
            else
            {
                string json = request.downloadHandler.text;
                Debug.Log("Stats: " + json);

                try
                {
                    PlayerStats stats = JsonUtility.FromJson<PlayerStats>(json);
                    Debug.Log($"레벨: {stats.level}, 경험치: {stats.exp}, 골드: {stats.gold}");
                    messageText.text = $"Lv.{stats.level} / EXP {stats.exp} / Gold {stats.gold}";
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Stats 파싱실패: " + e.Message);
                }
            }
        }
    }
    [System.Serializable]
    public class LoginData
    {
        public string id;
        public string password;
        public LoginData(string id, string pw)
        {
            this.id = id;
            this.password = pw;
        }
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string token;
    }

    [System.Serializable]
    public class PlayerStats
    {
        public int level;
        public int exp;
        public int gold;
        //임시
    }
}
