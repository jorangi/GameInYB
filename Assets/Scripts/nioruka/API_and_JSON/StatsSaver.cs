using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public interface IStatsSaver
{
    UniTask<bool> SavePlayerStatsAsync(PlayerStats stats);
}
public interface IAccessTokenProvider
{
    string GetAccessToken();
}
public interface IStatsRefresher
{
    string Name { get; }
    UniTask<bool> TryRefreshAsync();
}
public interface IStatsUIRefresher
{
    void RefreshStats();
}

public sealed class StatsSaver : IStatsSaver
{
    private readonly string _saveUrl;
    private readonly IAccessTokenProvider _tokenProvider;
    private readonly IReadOnlyList<IStatsRefresher> _refreshers;
    private readonly int _timeoutSeconds;

    public StatsSaver(
        IAccessTokenProvider tokenProvider,
        IReadOnlyList<IStatsRefresher> refreshers = null,
        string saveUrl = "https://api-looper.duckdns.org/api/mypage/stats",
        int timeoutSeconds = 10)
    {
        _tokenProvider = tokenProvider ?? throw new ArgumentNullException(nameof(tokenProvider));
        _refreshers = refreshers ?? Array.Empty<IStatsRefresher>();
        _saveUrl = string.IsNullOrWhiteSpace(saveUrl) ? throw new ArgumentException("saveUrl is empty") : saveUrl;
        _timeoutSeconds = timeoutSeconds > 0 ? timeoutSeconds : 10;
    }

    public async UniTask<bool> SavePlayerStatsAsync(PlayerStats stats)
    {
        if (stats == null)
        {
            Debug.LogError("[StatsSaver] stats is null");
            return false;
        }

        if (stats.mapid == null || string.IsNullOrEmpty(stats.mapid)) stats.mapid = "none";
        if (string.IsNullOrEmpty(stats.equiped)) stats.equiped = "[]";
        if (string.IsNullOrEmpty(stats.inventory)) stats.inventory = "[]";

        var accessToken = _tokenProvider.GetAccessToken();
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogError("[StatsSaver] 토큰 없음. 로그인 이후 시도하세요.");
            return false;
        }

        string jsonBody = JsonUtility.ToJson(stats);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using var request = new UnityWebRequest(_saveUrl, UnityWebRequest.kHttpVerbPUT);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + accessToken);
        request.timeout = _timeoutSeconds;

        Debug.Log("[SAVE] PUT 전송 JSON: " + jsonBody);

        await request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("[StatsSaver] Stats 저장 성공 (PUT)");

            bool anyRefreshed = false;
            foreach (var r in _refreshers)
            {
                try
                {
                    var ok = await r.TryRefreshAsync();
                    if (ok)
                    {
                        Debug.Log($"[StatsSaver] 갱신 성공: {r.Name}");
                        anyRefreshed = true;
                        break;
                    }
                    else
                    {
                        Debug.Log($"[StatsSaver] 갱신 시도 실패: {r.Name}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[StatsSaver] 갱신 중 예외({r.Name}): {ex.Message}");
                }
            }

            if (!anyRefreshed)
            {
                Debug.Log("[StatsSaver] 스탯 갱신 경로를 찾지 못했습니다. UI는 다음 주기에서 갱신될 수 있습니다.");
            }
            return true;
        }
        else
        {
            Debug.LogError("[StatsSaver] Stats 저장 실패: " + request.error);
            Debug.LogError("[StatsSaver] 응답 코드: " + request.responseCode);
            Debug.LogError("[StatsSaver] 서버 응답: " + request.downloadHandler.text);
            return false;
        }
    }
}

public sealed class LoginServiceStatsRefresher : IStatsRefresher
{
    private readonly IServiceProvider _serviceProvider;

    public string Name => "LoginServiceStatsRefresher";

    public LoginServiceStatsRefresher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public async UniTask<bool> TryRefreshAsync()
    {
        var svc = _serviceProvider.GetService(typeof(ILoginService)) as ILoginService;
        if (svc == null) return false;
        await svc.GetStatsAsync();
        return true;
    }
}
public sealed class UIStatsRefresher : IStatsRefresher
{
    private readonly IStatsUIRefresher _ui;

    public string Name => "UIStatsRefresher";

    public UIStatsRefresher(IStatsUIRefresher ui)
    {
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
    }

    public UniTask<bool> TryRefreshAsync()
    {
        _ui.RefreshStats();
        return UniTask.FromResult(true);
    }
}
public sealed class PlayableCharacterAccessTokenProvider : IAccessTokenProvider
{
    public string GetAccessToken()
    {
        var data = PlayableCharacter.Inst != null ? PlayableCharacter.Inst.Data : null;
        if (data == null || string.IsNullOrEmpty(data.accessToken))
        {
            Debug.LogWarning("[TokenProvider] accessToken 없음");
            return null;
        }
        return data.accessToken;
    }
}
public sealed class LoginAndStatsManagerAdapter : IStatsUIRefresher
{
    private readonly LoginAndStatsManager _mgr;

    public LoginAndStatsManagerAdapter(LoginAndStatsManager mgr)
    {
        _mgr = mgr;
    }

    public void RefreshStats()
    {
        if (_mgr == null)
        {
            Debug.LogWarning("[LoginAndStatsManagerAdapter] mgr is null");
            return;
        }
        _mgr.OnClick_RefreshStats();
    }
}