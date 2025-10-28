using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class TitleBootstrapper : MonoBehaviour
{
    [SerializeField] private string[] preloadLabels = { "IconsAtlas", "WeaponAtlas" };

    private async void Awake()
    {
        ServiceHub.EnsureRoot();
        CancellationToken ct = this.GetCancellationTokenOnDestroy();
        await NPCDataManager.Ready;
        if (!ServiceHub.TryGet<IAddressablesService>(out var addrSvc))
        {
            if (!ServiceHub.TryGet<IAtlasService>(out var atlasSvcExisting))
            {
                var impl = new AtlasService();
                ServiceHub.Root.Add<IAtlasService>(impl);
                ServiceHub.Root.Add<IAddressablesService>(impl);
                addrSvc = impl;
            }
            else
            {
                addrSvc = atlasSvcExisting as IAddressablesService;
                if (addrSvc == null)
                {
                    Debug.LogWarning("[TitleBootstrapper] IAddressablesService를 찾을 수 없어 라벨 기반 아틀라스 초기화 경로로 진행합니다.");
                }
                else
                {
                    ServiceHub.Root.Add<IAddressablesService>(addrSvc);
                }
            }
        }

        try
        {
            if (addrSvc != null)
            {
                var ids = ServiceHub.Get<INPCRepository>().ToArray();
                static string ToAddress(string id) => $"NPCProfile/{id}";
                var profileKey = ids
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Select(ToAddress)
                    .Distinct()
                    .ToArray();
                Debug.Log($"{string.Join(",", profileKey)}");
                var config = new AtlasService.PreloadConfig
                {
                    AtlasLabels = new[] { "IconsAtlas", "WeaponAtlas" },
                    AtlasKeys = null,
                    PrefabLabels = new[] { "EffectPrefab", "NPCPrefab" },
                    PrefabKeys = null,
                    ProfileLabels = null,
                    ProfileKeys = profileKey
                };
                await addrSvc.InitializeAsync(config, ct);
            }
            else
            {
                var atlasSvc = ServiceHub.Get<IAtlasService>();
                await atlasSvc.InitializeAsync(preloadLabels, ct);
            }

            Debug.Log("[TitleBootstrapper] Atlas/Prefabs/NPCProfile Ready");
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[TitleBootstrapper] 초기화가 취소되었습니다.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[TitleBootstrapper] 초기화 실패: {ex}");
        }
        ServiceHub.RebuildSceneScope(scope =>
        {
        });
    }
}
