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
    private bool _applied = false;
    private void Awake()
    {
        ServiceHub.EnsureRoot();

        ServiceHub.RebuildSceneScope(scope =>
        {
            if (uiManager == null) Debug.LogError("[GameBootstrapper] UIManager 참조 누락");
            if (playableCharacter == null) Debug.LogError("[GameBootstrapper] PlayableCharacter 참조 누락");
            if (characterInformation == null) Debug.LogError("[GameBootstrapper] CharacterInformation 참조 누락");

            scope.Add<INegativeSignal>(uiManager);
            scope.Add<IInventoryData>(playableCharacter);
            scope.Add<IInventoryUI>(characterInformation);

            var facade = new PlayableCharacterFacadeAdapter(PlayableCharacter.Inst);
            scope.Add<IPlayableCharacterFacade>(facade);

            var loader = new PlayerStatsLoader(ServiceHub.Get<IItemRepository>(), facade);
            scope.Add<PlayerStatsLoader>(loader);

            var tokenProvider = new PlayableCharacterAccessTokenProvider();
            var saver = new StatsSaver(tokenProvider, Array.Empty<IStatsRefresher>());
            scope.Add<IStatsSaver>(saver);

        });
        if (PlayerSession.Inst is not null && !_applied)
        {
            var s = PlayerSession.Inst.Stats;
            PlayableCharacter.Inst.Data.ApplyDto(s);
            _applied = true;
        }
    }
}