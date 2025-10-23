using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

public interface IRemoteStore
{
    UniTask SaveAsync(string key, string json);
    UniTask<string> LoadAsync(string key);
}
public sealed class PlayerSession
{
    private static readonly Lazy<PlayerSession> _lazy = new(() => new PlayerSession());
    public static PlayerSession Inst => _lazy.Value;
    private PlayerSession() { }
    public TokenResponseDTO Token { get; private set; }
    public PlayerStats Stats { get; private set; }
    public bool HasToken => !string.IsNullOrEmpty(Token?.accessToken);
    public event Action OnChanged;
    public void SetToken(TokenResponseDTO token)
    {
        Token = token;
        OnChanged?.Invoke();
    }
    public void SetStats(PlayerStats stats)
    {
        Stats = stats;
        OnChanged?.Invoke();
    }
    public void Clear()
    {
        Token = null;
        Stats = null;
        OnChanged?.Invoke();
    }
    [Serializable]
    private class SessionSnapshotDTO
    {
        public TokenResponseDTO token;
        public PlayerStats stats;
    }
    public async UniTask SaveToRemoteAsync(IRemoteStore store)
    {
        var dto = new SessionSnapshotDTO
        {
            token = Token,
            stats = Stats
        };
        var json = JsonUtility.ToJson(dto);
        await store.SaveAsync("player_session", json);
    }
    public async UniTask LoadFromRemoteAsync(IRemoteStore store)
    {
        var json = await store.LoadAsync("player_session");
        if (string.IsNullOrEmpty(json)) return;
        var dto = JsonUtility.FromJson<SessionSnapshotDTO>(json);
        Token = dto?.token;
        Stats = dto?.stats;
        OnChanged?.Invoke();
    }
}
