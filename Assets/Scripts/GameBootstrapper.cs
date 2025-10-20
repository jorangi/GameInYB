using System;
using UnityEngine;

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
    public static IPlayableCharacterFacade GetPlayableCharacterFacadeOrNull()
    {
        var inst = PlayableCharacter.Inst;
        if (inst == null) return null;
        return new PlayableCharacterFacadeAdapter(inst);
    }

    public static IPlayableCharacterFacade GetPlayableCharacterFacadeOrThrow()
    {
        var facade = GetPlayableCharacterFacadeOrNull();
        if (facade == null)
            throw new InvalidOperationException("[FacadeAccessors] PlayableCharacter.Inst not ready.");
        return facade;
    }
}
public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private UIManager uiManager;
    [SerializeField] private PlayableCharacter playableCharacter;
    [SerializeField] private CharacterInformation characterInformation;

    public static UnityServiceProvider ServiceProvider { get; private set; }

    private void Awake()
    {
        if (ServiceProvider != null) return;

        if (uiManager == null) Debug.LogError("[GameBootstrapper] UIManager 참조 누락");
        if (playableCharacter == null) Debug.LogError("[GameBootstrapper] PlayableCharacter 참조 누락");
        if (characterInformation == null) Debug.LogError("[GameBootstrapper] CharacterInformation 참조 누락");

        // PlayableCharacter.Inst/Data 초기화 보장 필요(스크립트 실행 순서에서 PlayableCharacter가 먼저)
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
        {
            Debug.LogError("[GameBootstrapper] PlayableCharacter.Inst 또는 Inst.Data가 아직 초기화되지 않았습니다. 실행 순서를 확인하세요.");
            // 계속 진행하면 이후 NRE 가능. 필요 시 return; 고려
        }

        var sp = new UnityServiceProvider();

        sp.Add<INegativeSignal>(uiManager);
        sp.Add<IInventoryData>(playableCharacter);
        sp.Add<IInventoryUI>(characterInformation);

        // LoginManager는 오직 Inst.Data만 사용하도록 훅을 주입
        sp.Add<ILoginService>(new LoginManager(LoadPlayableCharacterData, SavePlayableCharacterData));

        var repo = new ItemRepositoryAdapter();
        var facade = new PlayableCharacterFacadeAdapter(PlayableCharacter.Inst);
        var loader = new PlayerStatsLoader(repo, facade);
        var tokenProvider = new PlayableCharacterAccessTokenProvider();
        var saver = new StatsSaver(tokenProvider, Array.Empty<IStatsRefresher>());

        sp.Add<IItemRepository>(repo);
        sp.Add<IPlayableCharacterFacade>(facade);
        sp.Add<PlayerStatsLoader>(loader);
        sp.Add<IStatsSaver>(saver);

        ServiceProvider = sp;
    }

    // 항상 기존 인스턴스만 반환(새로 생성 금지)
    private PlayableCharacterData LoadPlayableCharacterData()
    {
        if (PlayableCharacter.Inst == null || PlayableCharacter.Inst.Data == null)
            throw new System.InvalidOperationException("PlayableCharacter.Inst.Data가 초기화되지 않았습니다.");
        return PlayableCharacter.Inst.Data;
        // 또는 직렬화 참조를 우선시하려면:
        // return playableCharacter != null && playableCharacter.Data != null
        //     ? playableCharacter.Data
        //     : PlayableCharacter.Inst.Data;
    }
    // 서버가 영속화하므로 여기서는 별도 저장 없음(동일 참조 유지)
    private void SavePlayableCharacterData(PlayableCharacterData data)
    {
        // no-op: LoginManager가 토큰/스탯을 Inst.Data(동일 참조)에 갱신함.
        // 서버와의 동기화/저장은 LoginManager(Login/Stats API)에서 처리.
        // 필요하다면 여기서 캐시 갱신 이벤트 발행 등만 수행.
    }
}