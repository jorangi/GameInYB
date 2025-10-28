using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.U2D;

public interface IAtlasService
{
    /// <summary>필수 아틀라스(라벨/키)를 선로딩하고 Ready를 세움.</summary>
    UniTask InitializeAsync(IEnumerable<object> preloadKeysOrLabels = null, CancellationToken ct = default);

    /// <summary>InitializeAsync 완료 여부.</summary>
    bool IsReady { get; }

    /// <summary>InitializeAsync 완료를 기다릴 때 사용.</summary>
    UniTask Ready { get; }

    /// <summary>아틀라스 1개를 보장 로딩(이미 있으면 즉시 반환).</summary>
    UniTask<SpriteAtlas> EnsureAtlasAsync(object keyOrLabel, CancellationToken ct = default);

    /// <summary>이미 로딩된 아틀라스 가져오기.</summary>
    bool TryGetAtlas(string atlasName, out SpriteAtlas atlas);

    /// <summary>이미 로딩된 아틀라스에서 스프라이트 가져오기.</summary>
    Sprite GetSprite(string atlasName, string spriteName);

    /// <summary>아틀라스를 필요 시 로드하여 스프라이트 가져오기.</summary>
    UniTask<Sprite> GetSpriteAsync(object keyOrLabel, string spriteName, CancellationToken ct = default);

    /// <summary>특정 아틀라스 해제.</summary>
    void ReleaseByAtlasName(string atlasName);

    /// <summary>전부 해제.</summary>
    void ReleaseAll();
}

/// <summary>
/// 선택사항: 프리팹(GameObject)까지 다루는 확장 인터페이스.
/// 기존 IAtlasService와 100% 호환되면서, 프리팹 관리 기능을 추가함.
/// </summary>
public interface IAddressablesService : IAtlasService
{
    /// <summary>프리팹 1개를 보장 로딩(이미 있으면 즉시 반환).</summary>
    UniTask<GameObject> EnsurePrefabAsync(object keyOrLabel, CancellationToken ct = default);

    /// <summary>이미 로딩된 프리팹 가져오기(이름 기반).</summary>
    bool TryGetPrefab(string prefabName, out GameObject prefab);

    /// <summary>이미 로딩된 프리팹을 이름 기반으로 반환(없으면 null).</summary>
    GameObject GetPrefab(string prefabName);

    /// <summary>프리팹을 필요 시 로드하여 가져오기.</summary>
    UniTask<GameObject> GetPrefabAsync(object keyOrLabel, CancellationToken ct = default);

    /// <summary>특정 프리팹 해제(이름 기반).</summary>
    void ReleaseByPrefabName(string prefabName);

    /// <summary>라벨/키를 아틀라스/프리팹으로 구성하여 프리로드.</summary>
    UniTask InitializeAsync(AtlasService.PreloadConfig config, CancellationToken ct = default);



    /// <summary>프로필 1개를 보장 로딩(이미 있으면 즉시 반환).</summary>
    UniTask<NPCProfile> EnsureProfileAsync(object keyOrLabel, CancellationToken ct = default);

    /// <summary>이미 로딩된 프로필 가져오기(이름 기반).</summary>
    bool TryGetProfile(string profileName, out NPCProfile profile);

    /// <summary>이미 로딩된 프로필을 이름 기반으로 반환(없으면 null).</summary>
    NPCProfile GetProfile(string profileId);

    /// <summary>프로필을 필요 시 로드하여 가져오기.</summary>
    UniTask<NPCProfile> GetProfileAsync(object keyOrLabel, CancellationToken ct = default);

    /// <summary>특정 프로필 해제(이름 기반).</summary>
    void ReleaseByProfileName(string profileName);
}
public sealed class AtlasService : IAddressablesService
{
    // ====== Public Preload Config ======
    /// <summary>
    /// Addressables 프리로드 구성체.
    /// 라벨/키를 타입별로 명시해서 한 번에 프리로드 가능.
    /// </summary>
    public sealed class PreloadConfig
    {
        // SpriteAtlas
        public IEnumerable<string> AtlasLabels { get; set; } = null;
        public IEnumerable<object> AtlasKeys { get; set; } = null; // 주소 문자열, AssetReference, IResourceLocation 등

        // Prefab (GameObject)
        public IEnumerable<string> PrefabLabels { get; set; } = null;
        public IEnumerable<object> PrefabKeys { get; set; } = null; // 주소 문자열, AssetReferenceGameObject 등
        
        // Profile (NPCProfile)
        public IEnumerable<string> ProfileLabels { get; set; } = null;
        public IEnumerable<object> ProfileKeys { get; set; } = null; // 주소 문자열, AssetReference 등
    }

    // ====== Internal Caches ======
    private readonly Dictionary<string, (SpriteAtlas atlas, AsyncOperationHandle handle, int refCount)> _atlases
        = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (GameObject prefab, AsyncOperationHandle handle, int refCount)> _prefabs
        = new(StringComparer.Ordinal);
    private readonly Dictionary<string, (NPCProfile profile, AsyncOperationHandle handle, int refCount)> _profiles
        = new(StringComparer.Ordinal);
    private UniTaskCompletionSource _readyTcs;
    public bool IsReady { get; private set; }
    public UniTask Ready => _readyTcs?.Task ?? UniTask.CompletedTask;

    // ====== Initialize (Backward-compatible) ======
    /// <summary>
    /// 기존 호환: 문자열이면 "라벨(아틀라스 전용)"로 간주, 그 외는 개별 키로 간주해 SpriteAtlas만 프리로드.
    /// 프리팹까지 프리로드하려면 PreloadConfig 오버로드 사용.
    /// </summary>
    public async UniTask InitializeAsync(IEnumerable<object> preloadKeysOrLabels = null, CancellationToken ct = default)
    {
        if (IsReady) return;
        _readyTcs ??= new UniTaskCompletionSource();

        try
        {
            if (preloadKeysOrLabels != null)
            {
                var list = preloadKeysOrLabels.ToList();
                var labels = list.OfType<string>().ToList();                 // 라벨(아틀라스 가정)
                var singleKeys = list.Where(o => o is not string).ToList();  // 개별 키(아틀라스 가정)

                // 아틀라스: 라벨 일괄
                foreach (var label in labels)
                {
                    ct.ThrowIfCancellationRequested();
                    var handle = Addressables.LoadAssetsAsync<SpriteAtlas>(label, null, true);
                    handle.Completed += h =>
                    {
                        if (h.Status == AsyncOperationStatus.Succeeded)
                        {
                            foreach (var atlas in h.Result)
                                RegisterLoadedAtlas(atlas, h);
                        }
                    };
                    await handle.WithCancellation(ct);
                }

                // 아틀라스: 단일 키
                foreach (var key in singleKeys)
                {
                    ct.ThrowIfCancellationRequested();
                    var handle = Addressables.LoadAssetAsync<SpriteAtlas>(key);
                    var atlas = await handle.WithCancellation(ct);
                    RegisterLoadedAtlas(atlas, handle);
                }
            }

            IsReady = true;
            _readyTcs.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _readyTcs.TrySetCanceled();
            throw;
        }
        catch (Exception ex)
        {
            _readyTcs.TrySetException(ex);
            throw;
        }
    }

    // ====== Initialize (Structured, Atlas + Prefab) ======
    public async UniTask InitializeAsync(PreloadConfig config, CancellationToken ct = default)
    {
        if (IsReady) return;
        _readyTcs ??= new UniTaskCompletionSource();

        try
        {
            if (config != null)
            {
                // 1) SpriteAtlas 라벨
                if (config.AtlasLabels != null)
                {
                    foreach (var label in config.AtlasLabels)
                    {
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetsAsync<SpriteAtlas>(label, null, true);
                        handle.Completed += h =>
                        {
                            if (h.Status == AsyncOperationStatus.Succeeded)
                            {
                                foreach (var atlas in h.Result)
                                    RegisterLoadedAtlas(atlas, h);
                            }
                        };
                        await handle.WithCancellation(ct);
                    }
                }

                // 2) SpriteAtlas 키
                if (config.AtlasKeys != null)
                {
                    foreach (var key in config.AtlasKeys)
                    {
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetAsync<SpriteAtlas>(key);
                        var atlas = await handle.WithCancellation(ct);
                        RegisterLoadedAtlas(atlas, handle);
                    }
                }

                // 3) Prefab 라벨
                if (config.PrefabLabels != null)
                {
                    foreach (var label in config.PrefabLabels)
                    {
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetsAsync<GameObject>(label, null, true);
                        handle.Completed += h =>
                        {
                            if (h.Status == AsyncOperationStatus.Succeeded)
                            {
                                foreach (var prefab in h.Result)
                                    RegisterLoadedPrefab(prefab, h);
                            }
                        };
                        await handle.WithCancellation(ct);
                    }
                }

                // 4) Prefab 키
                if (config.PrefabKeys != null)
                {
                    foreach (var key in config.PrefabKeys)
                    {
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetAsync<GameObject>(key);
                        var prefab = await handle.WithCancellation(ct);
                        RegisterLoadedPrefab(prefab, handle);
                    }
                }
                
                // 5) Profile 라벨
                if (config.ProfileLabels != null)
                {
                    foreach (var label in config.PrefabLabels)
                    {
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetsAsync<NPCProfile>(label, null, true);
                        handle.Completed += h =>
                        {
                            if (h.Status == AsyncOperationStatus.Succeeded)
                            {
                                foreach (var profile in h.Result)
                                    RegisterLoadedProfile(profile, h);
                            }
                        };
                        await handle.WithCancellation(ct);
                    }
                }

                // 6) Profile 키
                if (config.ProfileKeys != null)
                {
                    foreach (var key in config.ProfileKeys)
                    {
                        if (((string)key)[((string)key).IndexOf('1') + 1] == '1')
                        {
                            continue;
                        }
                        ct.ThrowIfCancellationRequested();
                        var handle = Addressables.LoadAssetAsync<NPCProfile>(key);
                        var profile = await handle.WithCancellation(ct);
                        RegisterLoadedProfile(profile, handle);
                    }
                }
            }

            IsReady = true;
            _readyTcs.TrySetResult();
        }
        catch (OperationCanceledException)
        {
            _readyTcs.TrySetCanceled();
            throw;
        }
        catch (Exception ex)
        {
            _readyTcs.TrySetException(ex);
            throw;
        }
    }

    // ====== SpriteAtlas APIs (unchanged contracts) ======
    public async UniTask<SpriteAtlas> EnsureAtlasAsync(object keyOrLabel, CancellationToken ct = default)
    {
        if (keyOrLabel == null) throw new ArgumentNullException(nameof(keyOrLabel));

        // 문자열인데 .spriteatlas 확장자로 끝나지 않으면 "라벨"로 간주(기존 정책 유지)
        if (keyOrLabel is string label && !label.EndsWith(".spriteatlas", StringComparison.OrdinalIgnoreCase))
        {
            var handle = Addressables.LoadAssetsAsync<SpriteAtlas>(label, null, true);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    foreach (var atlas in h.Result)
                        RegisterLoadedAtlas(atlas, h);
                }
            };
            await handle.WithCancellation(ct);

            // 해당 handle로 등록된 첫 아틀라스 반환 시도
            var first = _atlases.Values.FirstOrDefault(v => v.handle.Equals(handle)).atlas;
            if (first != null) return first;

            // 혹시 못 찾으면 라벨에서 위치를 얻어 하나 직접 로드
            var locsH = Addressables.LoadResourceLocationsAsync(label, typeof(SpriteAtlas));
            await locsH.Task;
            if (locsH.Status == AsyncOperationStatus.Succeeded && locsH.Result.Count > 0)
            {
                var loadH = Addressables.LoadAssetAsync<SpriteAtlas>(locsH.Result[0]);
                var atlas = await loadH.Task;
                RegisterLoadedAtlas(atlas, loadH);
                Addressables.Release(locsH);
                return atlas;
            }
            Addressables.Release(locsH);
            throw new InvalidOperationException($"[AtlasService] 라벨 '{label}'에서 SpriteAtlas 리소스 위치를 찾지 못했습니다.");
        }
        else
        {
            // 주소/AssetReference 등 단일 키 로드
            var handle = Addressables.LoadAssetAsync<SpriteAtlas>(keyOrLabel);
            var atlas = await handle.WithCancellation(ct);
            RegisterLoadedAtlas(atlas, handle);
            return atlas;
        }
    }

    public bool TryGetAtlas(string atlasName, out SpriteAtlas atlas)
    {
        if (string.IsNullOrWhiteSpace(atlasName))
        {
            atlas = null;
            return false;
        }
        if (_atlases.TryGetValue(atlasName, out var entry) && entry.atlas != null)
        {
            atlas = entry.atlas;
            return true;
        }
        atlas = null;
        return false;
    }

    public Sprite GetSprite(string atlasName, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(atlasName) || string.IsNullOrWhiteSpace(spriteName) || spriteName == "00000")
            return null;

        if (!_atlases.TryGetValue(atlasName, out var entry) || entry.atlas == null)
            throw new NullReferenceException($"[AtlasService] 아틀라스 '{atlasName}'가 로드되어 있지 않습니다.");

        var sprite = entry.atlas.GetSprite(spriteName);
        if (sprite == null)
            Debug.LogWarning($"[AtlasService] 아틀라스 '{atlasName}'에서 스프라이트 '{spriteName}'를 찾지 못했습니다.");

        return sprite;
    }

    public async UniTask<Sprite> GetSpriteAsync(object keyOrLabel, string spriteName, CancellationToken ct = default)
    {
        var atlas = await EnsureAtlasAsync(keyOrLabel, ct);
        var sprite = atlas.GetSprite(spriteName);
        if (sprite == null)
            Debug.LogWarning($"[AtlasService] (동적) 스프라이트 '{spriteName}'를 찾지 못했습니다. atlas='{atlas.name}'");
        return sprite;
    }

    public void ReleaseByAtlasName(string atlasName)
    {
        if (string.IsNullOrWhiteSpace(atlasName)) return;

        if (_atlases.TryGetValue(atlasName, out var entry))
        {
            try
            {
                if (entry.handle.IsValid())
                    Addressables.Release(entry.handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtlasService] Release(Atlas) 실패: {atlasName}, {ex.Message}");
            }
            _atlases.Remove(atlasName);
        }
    }

    // ====== Prefab APIs (new) ======
    public async UniTask<GameObject> EnsurePrefabAsync(object keyOrLabel, CancellationToken ct = default)
    {
        if (keyOrLabel == null) throw new ArgumentNullException(nameof(keyOrLabel));

        // 문자열이면서 ".prefab"으로 끝나지 않으면 라벨로 간주(일괄 로드)
        if (keyOrLabel is string label && !label.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
        {
            var handle = Addressables.LoadAssetsAsync<GameObject>(label, null, true);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    foreach (var prefab in h.Result)
                        RegisterLoadedPrefab(prefab, h);
                }
            };
            await handle.WithCancellation(ct);

            var first = _prefabs.Values.FirstOrDefault(v => v.handle.Equals(handle)).prefab;
            if (first != null) return first;

            var locsH = Addressables.LoadResourceLocationsAsync(label, typeof(GameObject));
            await locsH.Task;
            if (locsH.Status == AsyncOperationStatus.Succeeded && locsH.Result.Count > 0)
            {
                var loadH = Addressables.LoadAssetAsync<GameObject>(locsH.Result[0]);
                var prefab = await loadH.Task;
                RegisterLoadedPrefab(prefab, loadH);
                Addressables.Release(locsH);
                return prefab;
            }
            Addressables.Release(locsH);
            throw new InvalidOperationException($"[AtlasService] 라벨 '{label}'에서 Prefab 리소스 위치를 찾지 못했습니다.");
        }
        else
        {
            // 주소/AssetReference 등 단일 키 로드
            var handle = Addressables.LoadAssetAsync<GameObject>(keyOrLabel);
            var prefab = await handle.WithCancellation(ct);
            RegisterLoadedPrefab(prefab, handle);
            return prefab;
        }
    }

    public bool TryGetPrefab(string prefabName, out GameObject prefab)
    {
        if (string.IsNullOrWhiteSpace(prefabName))
        {
            prefab = null;
            return false;
        }
        if (_prefabs.TryGetValue(prefabName, out var entry) && entry.prefab != null)
        {
            prefab = entry.prefab;
            return true;
        }
        prefab = null;
        return false;
    }

    public GameObject GetPrefab(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return null;
        return _prefabs.TryGetValue(prefabName, out var entry) ? entry.prefab : null;
    }

    public async UniTask<GameObject> GetPrefabAsync(object keyOrLabel, CancellationToken ct = default)
    {
        // 필요 시 로드
        var prefab = await EnsurePrefabAsync(keyOrLabel, ct);
        if (prefab == null)
            Debug.LogWarning($"[AtlasService] (동적) 프리팹을 찾지 못했습니다. keyOrLabel='{keyOrLabel}'");
        return prefab;
    }

    public void ReleaseByPrefabName(string prefabName)
    {
        if (string.IsNullOrWhiteSpace(prefabName)) return;

        if (_prefabs.TryGetValue(prefabName, out var entry))
        {
            try
            {
                if (entry.handle.IsValid())
                    Addressables.Release(entry.handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtlasService] Release(Prefab) 실패: {prefabName}, {ex.Message}");
            }
            _prefabs.Remove(prefabName);
        }
    }

    // ====== NPC Profile ======

    public async UniTask<NPCProfile> EnsureProfileAsync(object keyOrLabel, CancellationToken ct = default)
    {
        if (keyOrLabel == null) throw new ArgumentNullException(nameof(keyOrLabel));

        // 문자열이면서 ".profile"으로 끝나지 않으면 라벨로 간주(일괄 로드)
        if (keyOrLabel is string label && !label.EndsWith(".profile", StringComparison.OrdinalIgnoreCase))
        {
            var handle = Addressables.LoadAssetsAsync<NPCProfile>(label, null, true);
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    foreach (var profile in h.Result)
                        RegisterLoadedProfile(profile, h);
                }
            };
            await handle.WithCancellation(ct);

            var first = _profiles.Values.FirstOrDefault(v => v.handle.Equals(handle)).profile;
            if (first != null) return first;

            var locsH = Addressables.LoadResourceLocationsAsync(label, typeof(NPCProfile));
            await locsH.Task;
            if (locsH.Status == AsyncOperationStatus.Succeeded && locsH.Result.Count > 0)
            {
                var loadH = Addressables.LoadAssetAsync<NPCProfile>(locsH.Result[0]);
                var profile = await loadH.Task;
                RegisterLoadedProfile(profile, loadH);
                Addressables.Release(locsH);
                return profile;
            }
            Addressables.Release(locsH);
            throw new InvalidOperationException($"[AtlasService] 라벨 '{label}'에서 Prefab 리소스 위치를 찾지 못했습니다.");
        }
        else
        {
            // 주소/AssetReference 등 단일 키 로드
            var handle = Addressables.LoadAssetAsync<NPCProfile>(keyOrLabel);
            var profile = await handle.WithCancellation(ct);
            RegisterLoadedProfile(profile, handle);
            return profile;
        }
    }
    public bool TryGetProfile(string profileName, out NPCProfile profile)
    {
        if (string.IsNullOrWhiteSpace(profileName))
        {
            profile = null;
            return false;
        }
        if (_profiles.TryGetValue(profileName, out var entry) && entry.profile != null)
        {
            profile = entry.profile;
            return true;
        }
        profile = null;
        return false;
    }
    public NPCProfile GetProfile(string profileId)
    {
        if (string.IsNullOrWhiteSpace(profileId)) return null;
        return _profiles.TryGetValue(profileId, out var entry) ? entry.profile : null;
    }
    public async UniTask<NPCProfile> GetProfileAsync(object keyOrLabel, CancellationToken ct = default)
    {
        // 필요 시 로드
        var profile = await EnsureProfileAsync(keyOrLabel, ct);
        if (profile == null)
            Debug.LogWarning($"[AtlasService] (동적) 프리팹을 찾지 못했습니다. keyOrLabel='{keyOrLabel}'");
        return profile;
    }
    public void ReleaseByProfileName(string profileName)
    {
        if (string.IsNullOrWhiteSpace(profileName)) return;

        if (_profiles.TryGetValue(profileName, out var entry))
        {
            try
            {
                if (entry.handle.IsValid())
                    Addressables.Release(entry.handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtlasService] Release(Prefab) 실패: {profileName}, {ex.Message}");
            }
            _profiles.Remove(profileName);
        }
    }
    // ====== Global Release ======
    public void ReleaseAll()
    {
        // Atlas
        foreach (var kv in _atlases.ToArray())
        {
            var (atlas, handle, _) = kv.Value;
            try
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtlasService] ReleaseAll(Atlas) 실패: {kv.Key}, {ex.Message}");
            }
        }
        _atlases.Clear();

        // Prefab
        foreach (var kv in _prefabs.ToArray())
        {
            var (prefab, handle, _) = kv.Value;
            try
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AtlasService] ReleaseAll(Prefab) 실패: {kv.Key}, {ex.Message}");
            }
        }
        _prefabs.Clear();

        IsReady = false;
        _readyTcs = null;
    }

    // ====== Internals ======
    private void RegisterLoadedAtlas(SpriteAtlas atlas, AsyncOperationHandle handle)
    {
        if (atlas == null) return;

        var key = atlas.name; // 기본 키: asset.name
        if (_atlases.TryGetValue(key, out var entry))
        {
            _atlases[key] = (entry.atlas, handle, entry.refCount + 1);
        }
        else
        {
            _atlases[key] = (atlas, handle, 1);
        }
    }
    private void RegisterLoadedPrefab(GameObject prefab, AsyncOperationHandle handle)
    {
        if (prefab == null) return;

        var key = prefab.name; // 기본 키: asset.name (라벨 일괄 로드시 주소를 모를 수 있음)
        if (_prefabs.TryGetValue(key, out var entry))
        {
            _prefabs[key] = (entry.prefab, handle, entry.refCount + 1);
        }
        else
        {
            _prefabs[key] = (prefab, handle, 1);
        }
    }
    private void RegisterLoadedProfile(NPCProfile profile, AsyncOperationHandle handle)
    {
        if (profile == null) return;
        var key = profile.id; // 기본 키: asset.name (라벨 일괄 로드시 주소를 모를 수 있음)
        if (_profiles.TryGetValue(key, out var entry))
        {
            _profiles[key] = (entry.profile, handle, entry.refCount + 1);
        }
        else
        {
            _profiles[key] = (profile, handle, 1);
        }
    }
}
