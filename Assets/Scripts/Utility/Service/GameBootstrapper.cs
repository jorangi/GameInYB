using System;
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
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayableCharacter playableCharacter;
    [SerializeField] private CharacterInformation characterInformation;
    private bool _applied = false;

    private void Awake()
    {
        ServiceHub.EnsureRoot();

        ServiceHub.RebuildSceneScope(scope =>
        {
            scope.Add<INegativeSignal>(uiManager);
            scope.Add<IInventoryData>(playableCharacter);
            scope.Add<IInventoryUI>(characterInformation);

            // 🔸 옵션 A 유지: 지연 접근 Facade 등록 (호출 ‘시점’에만 Inst 쓰도록)
            var facade = new PlayableCharacterFacadeAdapter(() => PlayableCharacter.Inst);
            scope.Add<IPlayableCharacterFacade>(facade);

            var loader = new PlayerStatsLoader(ServiceHub.Get<IItemRepository>(), facade);
            scope.Add<PlayerStatsLoader>(loader);

            var tokenProvider = new PlayableCharacterAccessTokenProvider();
            var saver = new StatsSaver(tokenProvider, Array.Empty<IStatsRefresher>());
            scope.Add<IStatsSaver>(saver);
        });
    }

    private async void Start()
    {
        // 한 프레임 유예
        await Cysharp.Threading.Tasks.UniTask.NextFrame();

        // Inst가 아직 null일 수 있으니 안전 대기 (선택)
        var ct = this.GetCancellationTokenOnDestroy();
        if (PlayableCharacter.Inst == null)
            await Cysharp.Threading.Tasks.UniTask.WaitUntil(
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
    string initialMap = "Forest_Stage_02";
    string _loadedSubScene;
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
        // 카메라, 레이어, FSM 타깃, 이벤트 등 재설정
        // ex) CameraFollow.Target = pc; pc.RebindLevelRefs();
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
}