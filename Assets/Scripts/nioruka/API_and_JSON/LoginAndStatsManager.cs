using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

public interface ILoginService
{
    UniTask InitializeAsync(bool autoLogin = true);
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
    //public float ats;
    public float def;
    public float cri;
    public float crid;
    public float spd;
    //public float jmp;
    public int jmp;
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
    private readonly Func<PlayableCharacterData> _loadPlayerData;
    private readonly Action<PlayableCharacterData> _savePlayerData;
    public PlayableCharacterData playerData { get; private set; }
    public LoginManager(Func<PlayableCharacterData> loadPlayerData, Action<PlayableCharacterData> savePlayerData)
    {
        _loadPlayerData = loadPlayerData ?? throw new ArgumentNullException(nameof(loadPlayerData));
        _savePlayerData = savePlayerData ?? throw new ArgumentNullException(nameof(savePlayerData));
        playerData = _loadPlayerData() ?? PlayableCharacter.Inst.Data;
        // if (playerData != null && !string.IsNullOrEmpty(playerData.accessToken))
        // {
        //     Debug.Log("저장된 토큰 발견 Stats 호출");
        //     GetStats().Forget();
        // }
    }
    public async UniTask InitializeAsync(bool autoLogin = true)
    {
        if (!autoLogin) return;
        if (!string.IsNullOrEmpty(playerData?.accessToken))
        {
            Debug.Log("[LoginManager] 저장된 토큰 발견 -> Stats 호출");
            try
            {
                await GetStatsAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[LoginManager] 초기 Stats 호출 실패: {e.Message}");
            }
        }
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

        playerData ??= PlayableCharacter.Inst.Data;
        playerData.accessToken = res.accessToken;
        playerData.refreshToken = res.refreshToken;
        playerData.nickname = res.nickname;
        playerData.roles = res.roles;

        _savePlayerData(playerData);

        Debug.Log("[LoginManager] 로그인 성공. 토큰 저장 완료. -> Stats 호출");
        await GetStatsAsync();
    }
    public async UniTask GetStatsAsync()
    {
        if (playerData is null || string.IsNullOrEmpty(playerData.accessToken))
        {
            Debug.LogError("토큰 없음. 로그인 이후 시도");
            return;
        }
        using UnityWebRequest request = UnityWebRequest.Get(STATS_URL);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + playerData.accessToken);
        request.timeout = 10;

        await request.SendWebRequest();
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            throw new Exception($"Stats 호출 실패: {request.error} (code:{request.responseCode})");
        }
        string json = request.downloadHandler.text;
        Debug.Log("[LoginManager] Stats: " + json);
        try
        {
            var parsed = JsonUtility.FromJson<PlayerStats>(json);
            playerData.statsDTO = parsed;
            _savePlayerData(playerData);
        }
        catch (Exception e)
        {
            throw new Exception($"Stats 파싱 실패: {e.Message}");
        }
    }

    public async UniTask GetStats()
    {
        if (playerData is null || string.IsNullOrEmpty(playerData.accessToken))
        {
            Debug.LogError("토큰 없음. 로그인 이후 시도");
            return;
        }
        using UnityWebRequest request = UnityWebRequest.Get(STATS_URL);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Accept", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + playerData.accessToken);
        request.timeout = 10;

        await request.SendWebRequest();

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
                playerData.statsDTO = JsonUtility.FromJson<PlayerStats>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Stats 파싱 실패: " + e.Message);
            }

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
        if (statText == null)
            statText = FindAnyObjectByType<StatEditUI>();

        // PlayableCharacter 준비가 되었는지 확인
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        {
            Debug.LogError("[LoginAndStatsManager] PlayableCharacter 또는 Data가 아직 초기화되지 않았습니다. 초기화 순서를 확인하세요.");
            return;
        }

        // LoginManager 구성: 절대 새 데이터 생성하지 않고, Inst.Data만 사용
        _login = new LoginManager(
            loadPlayerData: () =>
            {
                // 오직 Inst.Data만 반환(없으면 예외로 흐름 중단)
                if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
                    throw new InvalidOperationException("PlayableCharacter.Inst.Data가 초기화되지 않았습니다.");
                return PlayableCharacter.Inst.Data;
            },
            savePlayerData: (d) =>
            {
                // 저장은 Inst.Data에 이미 같은 참조가 들어있다는 전제 하에, 별도 동작 불필요.
                // 필요 시 여기에서 PlayerPrefs/파일 저장을 호출하면 됨(옵션).
                // 절대 새 PlayableCharacterData를 생성하거나 치환하지 않음.
            }
        );

        // 자동 로그인/스탯 조회
        try
        {
            await _login.InitializeAsync(autoLogin: true);
            UpdateUIFromPlayableCharacter();
            if (messageText != null && !string.IsNullOrEmpty(PlayableCharacter.Inst.Data?.accessToken))
                messageText.text = "정상적으로 데이터가 적용되었습니다.";
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[LoginAndStatsManager] Initialize 실패: {e.Message}");
        }
    }

    public async void OnClick_Login()
    {
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        {
            Debug.LogError("[LoginAndStatsManager] PlayableCharacter 또는 Data가 아직 초기화되지 않았습니다.");
            return;
        }

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

        var data = PlayableCharacter.Inst?.Data;
        if (data == null)
        {
            Debug.LogWarning("[LoginAndStatsManager] PlayableCharacter.Inst.Data가 null입니다.");
            return;
        }

        var s = data.statsDTO; // PlayerStats
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
}
