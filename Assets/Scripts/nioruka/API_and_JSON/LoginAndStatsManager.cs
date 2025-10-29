using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

public interface ILoginService
{
    UniTask<bool> InitializeAsync(bool autoLogin = true);
    UniTask LoginAsync(string id, string pw);
    UniTask GetStatsAsync();
}

/// <summary>
/// PlayerStats DTO
/// </summary>
public class PlayerStats
{
    public float hp;
    public float atk;
    public float ats;
    public float def;
    public float cri;
    public float crid;
    public float spd;
    public float jmp;
    public int jcnt;
    public int clear;
    public int chapter;
    public int stage;
    public string mapid;
    public string equiped;//equipped로 바꾸기
    public string inventory;
    public class ArrayWrapper<T> { public T[] items; }
    //public string skills;
    public override string ToString()
    {
        return $"hp: {hp}\natk: {atk}\ndef: {def}\ncri: {cri}\ncrid: {crid}\nspd: {spd}\njmp: {jmp}\nclear: {clear}\nchapter: {chapter}\nstage: {stage}\nmapid: {mapid}\nequiped: {equiped}\ninventory: {inventory}";
    }
}
[System.Serializable]
public class LoginRequestDTO
{
    public string username;
    public string password;
    public LoginRequestDTO(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}
[System.Serializable]
public class TokenResponseDTO
{
    public string accessToken;
    public string refreshToken;
    public string nickname;
    public string[] roles;
}
public class LoginManager : ILoginService
{
    private const string LOGIN_URL = "https://api-looper.duckdns.org/api/auth/login";
    private const string STATS_URL = "https://api-looper.duckdns.org/api/mypage/stats";
    private readonly IAccessTokenProvider _tokenProvider;
    public LoginManager(IAccessTokenProvider tokenProvider) => _tokenProvider = tokenProvider ?? throw new ArgumentException(nameof(tokenProvider));
    public async UniTask<bool> InitializeAsync(bool autoLogin = true)
    {
        if (!autoLogin) return false;
        if (!string.IsNullOrEmpty(_tokenProvider.GetAccessToken()))
        {
            Debug.Log("[LoginManager] 저장된 토큰 발견 -> Stats 호출");
            try
            {
                await GetStatsAsync();
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoginManager] 초기 Stats 호출 실패: {e.Message}");
                return false;
            }
        }
        return false;
    }
    public async UniTask LoginAsync(string id, string pw)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            throw new ArgumentException("ID/PW가 비어있습니다.");
        }
        var body = JsonUtility.ToJson(new LoginRequestDTO(id.Trim(), pw.Trim()));
        using UnityWebRequest request = new(LOGIN_URL, "POST");
        byte[] bodyRaw = Encoding.UTF8.GetBytes(body);

        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Accept", "application/json");
        request.timeout = 10;

        Debug.Log($"[Login] url={LOGIN_URL} method=post body={body}");

        await request.SendWebRequest();

        Debug.Log($"[LOGIN] responseCode={request.responseCode}, result={request.result}, error={request.error}");
        Debug.Log($"[LOGIN] responseText={request.downloadHandler.text}");

        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            throw new Exception($"로그인 실패: {request.error} (code:{request.responseCode})");

        TokenResponseDTO res;
        try
        {
            res = JsonUtility.FromJson<TokenResponseDTO>(request.downloadHandler.text);
        }
        catch (Exception e)
        {
            throw new Exception($"로그인 응답 파싱 실패: {e.Message}");
        }
        PlayerSession.Inst.SetToken(res);

        await GetStatsAsync();
    }
    public async UniTask GetStatsAsync()
    {
        if (string.IsNullOrEmpty(_tokenProvider.GetAccessToken()))
        {
            Debug.LogError("토큰 없음. 로그인 이후 시도");
            return;
        }
        using UnityWebRequest request = UnityWebRequest.Get(STATS_URL);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + _tokenProvider.GetAccessToken());
        request.timeout = 10;

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception($"Stats 호출 실패: {request.error} (code:{request.responseCode})");
        }
        string json = request.downloadHandler.text;
        try
        {
            var parsed = JsonUtility.FromJson<PlayerStats>(json);
            PlayerSession.Inst.SetStats(parsed);
            ServiceHub.isLoadedFromLogin = true;
        }
        catch (Exception e)
        {
            throw new Exception($"Stats 파싱 실패: {e.Message}");
        }
    }
}

public class LoginAndStatsManager : MonoBehaviour
{
    public IStatText statText;

    [Header("Login UI")]
    public TMP_InputField idInput;
    public TMP_InputField pwInput;
    public TMP_Text messageText;
    private ILoginService _login;

    private async void Start()
    {
        // UI 연결
        statText ??= FindAnyObjectByType<StatEditUI>();

        // PlayableCharacter 준비가 되었는지 확인
        // if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        // {
        //     Debug.LogError("[LoginAndStatsManager] PlayableCharacter 또는 Data가 아직 초기화되지 않았습니다. 초기화 순서를 확인하세요.");
        //     return;
        // }
        _login = ServiceHub.Get<ILoginService>();
        // 자동 로그인/스탯 조회
        try
        {
            await _login.InitializeAsync(autoLogin: true);
            UpdateUIFromPlayableCharacter();
            if (messageText != null && !string.IsNullOrEmpty(ServiceHub.Get<IAccessTokenProvider>().GetAccessToken()))
                messageText.text = "정상적으로 데이터가 적용되었습니다.";
            Debug.Log(messageText.text);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoginAndStatsManager] Initialize 실패: {e.Message}");
        }
    }

    public async void OnClick_Login()
    {
        Debug.Log(ServiceHub.Get<IAddressablesService>().GetProfile("10014").id);
        // if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        // {
        //     Debug.LogError("[LoginAndStatsManager] PlayableCharacter 또는 Data가 아직 초기화되지 않았습니다.");
        //     return;
        // }

        string id = idInput != null ? idInput.text.Trim() : string.Empty;
        string pw = pwInput != null ? pwInput.text.Trim() : string.Empty;

        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(pw))
        {
            if (messageText != null) messageText.text = "ID/PW 입력";
            return;
        }

        try
        {
            if (messageText != null) messageText.text = "로그인 중…";
            await _login.LoginAsync(id, pw);   // 로그인 + Stats 호출
            UpdateUIFromPlayableCharacter();
            if (messageText != null) messageText.text = "정상적으로 데이터가 적용되었습니다.";
        }
        catch (Exception e)
        {
            Debug.LogError($"[LoginAndStatsManager] 로그인 실패: {e.Message}");
            if (messageText != null) messageText.text = "로그인 실패";
        }
    }

    public async void OnClick_RefreshStats()
    {
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        {
            Debug.LogError("[LoginAndStatsManager] PlayableCharacter 또는 Data가 아직 초기화되지 않았습니다.");
            return;
        }

        try
        {
            if (messageText != null) messageText.text = "스탯 갱신 중…";
            await _login.GetStatsAsync();
            UpdateUIFromPlayableCharacter();
            if (messageText != null) messageText.text = "정상적으로 데이터가 적용되었습니다.";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoginAndStatsManager] 스탯 갱신 실패: {e.Message}");
            if (messageText != null) messageText.text = "스탯 갱신 실패";
        }
    }

    private void UpdateUIFromPlayableCharacter()
    {
        if (statText == null)
            return;

        var data = ServiceHub.Get<PlayerSession>().Stats;
        if (data == null)
        {
            Debug.LogWarning("[LoginAndStatsManager] PlayableCharacter.Inst.Data가 null입니다.");
            return;
        }

        var s = data; // PlayerStats
        // 기본값과 구분이 필요하면 별도 플래그/필드로 판별(서버 호출 성공 시점에 세팅)하는 것도 방법
        // 여기서는 그대로 표시
        try
        {
            // 기존 테스트 코드와 동일한 UI 갱신 로직 유지
            statText.hp.text   = $"{s.hp}";
            statText.atk.text  = $"{s.atk}";
            // statText.ats.text = $"{s.ats}";
            statText.def.text  = $"{s.def}";
            statText.cri.text  = $"{s.cri}";
            statText.crid.text = $"{s.crid}";
            statText.spd.text  = $"{s.spd}";
            statText.jmp.text  = $"{s.jmp}";
            statText.clear.text   = $"{s.clear}";
            statText.chapter.text = $"{s.clear}";
            statText.stage.text   = $"{s.clear}";
            statText.mapId.text   = $"{s.clear}";

            string[] equipped = SplitEquipped(s.equiped);
            statText.helmet.text     = $"{(equipped.Length > 0 ? equipped[0] : "")}";
            statText.armor.text      = $"{(equipped.Length > 1 ? equipped[1] : "")}";
            statText.pants.text      = $"{(equipped.Length > 2 ? equipped[2] : "")}";
            statText.mainWeapon.text = $"{(equipped.Length > 3 ? equipped[3] : "")}";
            statText.subWeapon.text  = $"{(equipped.Length > 4 ? equipped[4] : "")}";
            statText.inventory.text  = $"{s.inventory}";
            // statText.skills.text  = $"{s.skills}";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoginAndStatsManager] UI 업데이트 중 예외: {e.Message}");
        }
    }

    private static string[] SplitEquipped(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        string trimmed = raw.Replace("[", "").Replace("]", "");
        if (string.IsNullOrWhiteSpace(trimmed)) return Array.Empty<string>();
        var parts = trimmed.Split(',');
        for (int i = 0; i < parts.Length; i++)
            parts[i] = parts[i].Trim();
        return parts;
    }


    // TitleManager에서 호출
    public async UniTask InitializeAutoLogin()
    {
    await _login.InitializeAsync(autoLogin: true);
    }

    public async UniTask TryLogin(string id, string pw)
    {
    await _login.LoginAsync(id, pw);
    }
}
