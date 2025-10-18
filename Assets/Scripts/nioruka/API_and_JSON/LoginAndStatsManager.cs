using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class LoginAndStatsManager : MonoBehaviour
{
    public static PlayerData currentPlayer; 

    private const string LOGIN_URL = "https://api-looper.duckdns.org/api/auth/login";
    private const string STATS_URL = "https://api-looper.duckdns.org/api/mypage/stats";

    [Header("Login UI")]
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;

    void Start()
    {
        // 자동 로그인
        if (currentPlayer != null && !string.IsNullOrEmpty(currentPlayer.accessToken))
        {
            Debug.Log("저장된 토큰 발견 → Stats 호출");
            StartCoroutine(GetStats());
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

        StartCoroutine(LoginRequest(id, pw));
    }

    public IEnumerator LoginRequest(string id, string pw)
    {
        string jsonBody = JsonUtility.ToJson(new LoginData(id, pw));
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(LOGIN_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            Debug.Log($"[LOGIN] url={LOGIN_URL} method=POST");
            Debug.Log($"[LOGIN] requestBody={jsonBody}");
            Debug.Log($"[LOGIN] responseCode={request.responseCode}");
            Debug.Log($"[LOGIN] result={request.result} error={request.error}");
            Debug.Log($"[LOGIN] responseText={request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                TokenResponse res = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                currentPlayer = new PlayerData
                {
                    accessToken = res.accessToken,
                    refreshToken = res.refreshToken,
                    nickname = res.nickname,
                    roles = res.roles
                };

                Debug.Log("토큰: " + currentPlayer.accessToken);
                StartCoroutine(GetStats());
            }
            else
            {
                Debug.LogError("로그인 실패");
            }
        }
    }

    public IEnumerator GetStats()
    {
        if (currentPlayer == null || string.IsNullOrEmpty(currentPlayer.accessToken))
        {
            Debug.LogError("토큰 없음. 로그인 이후 시도");
            yield break;
        }

        using (UnityWebRequest request = UnityWebRequest.Get(STATS_URL))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + currentPlayer.accessToken);
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("API 호출 실패: " + request.error);
            }
            else
            {
                string json = request.downloadHandler.text;
                Debug.Log("Stats: " + json);

                try
                {
                    currentPlayer.stats = JsonUtility.FromJson<PlayerStats>(json);
                    Debug.Log($"HP: {currentPlayer.stats.hp}, ATK: {currentPlayer.stats.atk}, DEF: {currentPlayer.stats.def}");
                    messageText.text = $"HP {currentPlayer.stats.hp} / ATK {currentPlayer.stats.atk}";
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Stats 파싱 실패: " + e.Message);
                }
            }
        }
    }

    //TitleScene에서 호출
    public IEnumerator LoginRequest_Title(string id, string pw, Action onSuccess, Action onFail)
    {
        string jsonBody = JsonUtility.ToJson(new LoginData(id, pw));
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(LOGIN_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            Debug.Log($"[LOGIN_TITLE] responseCode={request.responseCode}");
            Debug.Log($"[LOGIN_TITLE] responseText={request.downloadHandler.text}");

            if (request.result == UnityWebRequest.Result.Success)
            {
                var res = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                if (!string.IsNullOrEmpty(res.accessToken))
                {
                    Debug.Log("로그인 성공 (TitleScene)");
                    currentPlayer = new PlayerData
                    {
                        accessToken = res.accessToken,
                        refreshToken = res.refreshToken,
                        nickname = res.nickname,
                        roles = res.roles
                    };

                    StartCoroutine(GetStats());
                    onSuccess?.Invoke();
                }
                else
                {
                    Debug.LogError("로그인 실패 (TitleScene) - accessToken 없음");
                    onFail?.Invoke();
                }
            }
            else
            {
                Debug.LogError("로그인 실패 (TitleScene) - 서버 에러");
                onFail?.Invoke();
            }
        }
    }


    [System.Serializable]
    public class PlayerStats
    {
        public float hp;
        public float atk;
        public float def;
        public float cri;
        public float crid;
        public float spd;
        public float jmp;
        public int clear;
        public int chapter;
        public int stage;
        public string mapid;
        public string equiped;
        public string inventory;
    }

    [System.Serializable]
    public class PlayerData
    {
        public string accessToken;
        public string refreshToken;
        public string nickname;
        public string[] roles;
        public PlayerStats stats;
    }

    [System.Serializable]
    public class LoginData
    {
        public string username;
        public string password;

        public LoginData(string username, string password)
        {
            this.username = username;
            this.password = password;
        }
    }

    [System.Serializable]
    public class TokenResponse
    {
        public string accessToken;
        public string refreshToken;
        public string nickname;
        public string[] roles;
    }
}
