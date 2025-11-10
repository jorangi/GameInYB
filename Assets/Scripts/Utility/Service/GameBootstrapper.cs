using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public interface IPlayerStatsLoaderFactory
{
    PlayerStatsLoader Create();
}

public sealed class PlayerStatsLoaderFactory : IPlayerStatsLoaderFactory
{
    private readonly IItemRepository _repo;
    private readonly Func<IPlayableCharacterFacade> _facadeAccessor;

    public PlayerStatsLoaderFactory(IItemRepository repo, Func<IPlayableCharacterFacade> facadeAccessor)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _facadeAccessor = facadeAccessor ?? throw new ArgumentNullException(nameof(facadeAccessor));
    }

    public PlayerStatsLoader Create()
    {
        var facade = _facadeAccessor() ?? throw new InvalidOperationException("[PlayerStatsLoaderFactory] Facade is null");
        return new PlayerStatsLoader(_repo, facade);
    }
}
public static class FacadeAccessors
{
    // 즉시형은 여전히 제공하되, 정말 즉시 필요할 때만 사용
    public static IPlayableCharacterFacade GetPlayableCharacterFacadeOrThrow()
    {
        var inst = PlayableCharacter.Inst
            ?? throw new InvalidOperationException("PlayableCharacter.Inst not ready.");
        return new PlayableCharacterFacadeAdapter(() => PlayableCharacter.Inst);
    }

    // 널 허용형: 대부분은 이걸 쓰거나, DI로 주입받고 Start 이후 접근
    public static IPlayableCharacterFacade GetPlayableCharacterFacadeOrNull()
        => new PlayableCharacterFacadeAdapter(() => PlayableCharacter.Inst);
}
public interface ISceneManager
{
    public List<Portal> portals { get; set; }
    public List<NonPlayableCharacter> monsters { get; set; }
    public void PortalRegistry(Portal portal);
    public void PortalUnRegistry(Portal portal);
    public void MonsterRegistry(NonPlayableCharacter monster);
    public void MonsterUnRegistry(NonPlayableCharacter monster);
    public UniTask LoadSubSceneAsync(string mapName);
    public UniTask NewRunAsync();
    public void Giveup();
}
public enum HitObjectType
{
    HITBOX,
    NPC_HITBOX,
    HIT_EFFECT,
    CRITICAL_EFFECT
}
public interface IHitManager
{
    public GameObject GetGameObject(HitObjectType type);
    public HitBox GetHitBox(IStatProvider provider);
    public NPC__AttackHitBox GetNPCHitBox(IStatProvider provider);
    public HitSpark GetHitEffect();
    public HitSpark GetCriticalEffect();
}
public class GameBootstrapper : MonoBehaviour, ISceneManager, IHitManager
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayableCharacter playableCharacter;
    [SerializeField] private CharacterInformation characterInformation;
    [SerializeField] private GameObject hitBox;
    [SerializeField] private Transform hitBoxPool;
    [SerializeField] private GameObject monsterHitBox;
    [SerializeField] private Transform MonsterHitBoxPool;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private Transform hitEffectPool;
    [SerializeField] private GameObject criticalHitEffect;
    [SerializeField] private Transform criticalHitEffectPool;
    private bool _applied = false;

    private void Awake()
    {
        ServiceHub.EnsureRoot();
        ServiceHub.RebuildSceneScope(scope =>
        {
            scope.Add<ISceneManager>(this);
            scope.Add<IHitManager>(this);
            scope.Add<INegativeSignal>(uiManager);
            scope.Add<IInventoryData>(playableCharacter);
            scope.Add<IInventoryUI>(characterInformation);

            for (int i = 0; i < 100; i++)
            {
                GameObject _hitBox = Instantiate(hitBox, hitBoxPool);
                _hitBox.name = "AttackHitBox";

                GameObject _monsterHitBox = Instantiate(monsterHitBox, MonsterHitBoxPool);
                _monsterHitBox.name = "MonsterAttackHitBox";

                GameObject _hitEffect = Instantiate(hitEffect, hitEffectPool);
                _hitEffect.name = "HitEffect";
                
                GameObject _criticalHitEffect = Instantiate(criticalHitEffect, criticalHitEffectPool);
                _criticalHitEffect.name = "CriticalHitEffect";
            }

            // 🔸 옵션 A 유지: 지연 접근 Facade 등록 (호출 ‘시점’에만 Inst 쓰도록)
            var facade = new PlayableCharacterFacadeAdapter(() => PlayableCharacter.Inst);
            scope.Add<IPlayableCharacterFacade>(facade);

            var loader = new PlayerStatsLoader(ServiceHub.Get<IItemRepository>(), facade);
            scope.Add<PlayerStatsLoader>(loader);

            var tokenProvider = new PlayableCharacterAccessTokenProvider();
            var saver = new StatsSaver(tokenProvider, Array.Empty<IStatsRefresher>());
            scope.Add<IStatsSaver>(saver);
        });
        monsters = new();
        portals = new();
    }

    private async void Start()
    {
        // 한 프레임 유예
        await UniTask.NextFrame();

        // Inst가 아직 null일 수 있으니 안전 대기 (선택)
        var ct = this.GetCancellationTokenOnDestroy();
        if (PlayableCharacter.Inst == null)
            await UniTask.WaitUntil(
                () => PlayableCharacter.Inst != null,
                cancellationToken: ct
            );

        var pc = PlayableCharacter.Inst;
        if (PlayerSession.Inst is not null && !_applied && pc != null)
        {
            pc.Data.ApplyDto(PlayerSession.Inst.Stats);
            _applied = true;
        }

        _ = NewRunAsync();
    }
    string initialMap = "Forest_Stage_01";
    string _loadedSubScene;
    public Action monsterAction;
    public List<NonPlayableCharacter> monsters { get; set; }
    public List<Portal> portals { get; set; }
    public void PortalRegistry(Portal portal) => portals.Add(portal);
    public void PortalUnRegistry(Portal portal)
    {
        if (portals.IndexOf(portal) != -1)
            portals.Remove(portal);
    }
    public void MonsterRegistry(NonPlayableCharacter monster)
    {
        monsters.Add(monster);
        monsterAction?.Invoke();
    }
    public void MonsterUnRegistry(NonPlayableCharacter monster)
    {
        if (monsters.IndexOf(monster) != -1)
            monsters.Remove(monster);
        monsterAction?.Invoke();
        if (monsters.Count == 0)
        {
            foreach (var p in portals) p.PortalOn();
            ServiceHub.Get<ILogMessage>().Spawn($"모든 몬스터를 처치하여 {portals.Count}개의 포탈이 열렸습니다.");
        }
    }
    public async UniTask LoadSubSceneAsync(string mapName)
    {
        // 기존 맵 정리
        if (!string.IsNullOrEmpty(_loadedSubScene))
            await SceneManager.UnloadSceneAsync(_loadedSubScene).ToUniTask();

        // 새 맵 로드
        var op = SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
        await op.ToUniTask();

        var scn = SceneManager.GetSceneByName(mapName);
        SceneManager.SetActiveScene(scn);
        _loadedSubScene = mapName;

        // 맵 진입 훅: 카메라/레이어/스폰포인트 등 재바인드
        OnSubSceneLoaded();
    }
    public async UniTask NewRunAsync()
    {
        await LoadSubSceneAsync(initialMap);
    }
    void OnSubSceneLoaded()
    {
        var pc = PlayableCharacter.Inst;
        // 카메라, 스폰포인트 찾기
        var spawn = GameObject.FindWithTag("PlayerSpawn");
        if (spawn) pc.transform.position = spawn.transform.position;

        var vcam = UnityEngine.Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null && PlayableCharacter.Inst != null)
        {
            vcam.Target.TrackingTarget = PlayableCharacter.Inst.transform;
        }
    }
    public async UniTask GiveUpAsync() // “게임 포기”
    {
        var pc = PlayableCharacter.Inst;
        pc.ResetPlayerData();           // 데이터만 리셋, Player는 유지
        // 현재 맵 정리 후 시작맵으로
        await LoadSubSceneAsync(initialMap);

    }
    public void Giveup()
    {
        _ = GiveUpAsync();
    }
    public GameObject GetGameObject(HitObjectType type)
    {
        return type switch
        {
            HitObjectType.HITBOX => hitBoxPool.GetChild(0).gameObject,
            HitObjectType.NPC_HITBOX => MonsterHitBoxPool.GetChild(0).gameObject,
            HitObjectType.HIT_EFFECT => hitEffectPool.GetChild(0).gameObject,
            HitObjectType.CRITICAL_EFFECT => criticalHitEffectPool.GetChild(0).gameObject,
            _ => null,
        };
    }
    public HitBox GetHitBox(IStatProvider provider)
    {
        var h = GetGameObject(HitObjectType.HITBOX).GetComponent<HitBox>();
        h.provider ??= provider;
        h.gameObject.SetActive(true);
        return h;
    }
    public NPC__AttackHitBox GetNPCHitBox(IStatProvider provider)
    {
        var h = GetGameObject(HitObjectType.NPC_HITBOX).GetComponent<NPC__AttackHitBox>();
        h.provider = provider;
        h.gameObject.SetActive(true);
        return h;
    }
    public HitSpark GetHitEffect()
    {
        var h = GetGameObject(HitObjectType.HIT_EFFECT).GetComponent<HitSpark>();
        h.gameObject.SetActive(true);
        return h;
    }
    public HitSpark GetCriticalEffect()
    {
        var h = GetGameObject(HitObjectType.CRITICAL_EFFECT).GetComponent<HitSpark>();
        h.gameObject.SetActive(true);
        return h;
    }
}