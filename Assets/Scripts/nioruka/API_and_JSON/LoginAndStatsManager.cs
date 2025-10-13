using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class LoginAndStatsManager : MonoBehaviour
{
    private const string LOGIN_URL = "https://api-looper.duckdns.org/api/auth/login";
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
            Debug.Log("이미 로그인했던 토큰으로 자동연결합니다");
            StartCoroutine(GetStats());
        }
    }

    public void OnClick_Login()
    {
        string id = idInput.text.Trim();
        string pw = pwInput.text.Trim();

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            messageText.text = "ID/PW를 입력하세요.";
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
                var res = JsonUtility.FromJson<TokenResponse>(request.downloadHandler.text);
                Debug.Log("토큰: " + res.accessToken);

                if (!string.IsNullOrEmpty(res.accessToken))
                {
                    PlayerPrefs.SetString("auth_token", res.accessToken);
                    PlayerPrefs.Save();
                    StartCoroutine(GetStats());
                }
                else
                {
                    Debug.LogError("로그인에 성공했지만 accessToken이 비어있음");
                }
            }
            else
            {
                Debug.LogError("로그인 실패");
            }
        }
    }

    IEnumerator GetStats()
    {
        string token = PlayerPrefs.GetString("auth_token", "");
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogError("토큰 없음. 로그인 이후 시도");
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

    Debug.Log($"HP: {stats.hp}, ATK: {stats.atk}, DEF: {stats.def}, CRI: {stats.cri}, CRID: {stats.crid}");
    Debug.Log($"SPD: {stats.spd}, JMP: {stats.jmp}, CLEAR: {stats.clear}, CHAPTER: {stats.chapter}, STAGE: {stats.stage}");

        messageText.text =
        $"HP {stats.hp} / ATK {stats.atk} / DEF {stats.def}\n" +
        $"CH {stats.chapter}-{stats.stage} | SPD {stats.spd} | JMP {stats.jmp}";

                }
                catch (System.Exception e)
                {
                    Debug.LogWarning("Stats 파싱 실패: " + e.Message);
                }
            }
        }
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
    public void OnClick_Logout()
{
    PlayerPrefs.DeleteKey("auth_token");
    PlayerPrefs.Save();
    messageText.text = "로그아웃";
    Debug.Log("토큰 삭제됨");
}

    [System.Serializable]
    public class TokenResponse
    {
        public string accessToken;
        public string refreshToken;
        public string nickname;
        public string[] roles;
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

}
