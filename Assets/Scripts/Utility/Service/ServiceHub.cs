using System;
using UnityEngine;

public static class ServiceHub
{
    public static bool isLoadedFromLogin;
    public static UnityServiceProvider Root { get; private set; }
    public static UnityServiceProvider SceneScope { get; private set; }

    public static void EnsureRoot()
    {
        Root ??= new();

        Root.Add(PlayerSession.Inst);
        Root.Add<IItemRepository>(new ItemRepositoryAdapter());
        Root.Add<INPCRepository>(new NPCRepositoryAdapter());

        Root.Add<ILoginService>(new LoginManager(new PlayableCharacterAccessTokenProvider()));
    }
    public static void RebuildSceneScope(Action<UnityServiceProvider> register)
    {
        SceneScope = new();
        register?.Invoke(SceneScope);
    }
    public static T Get<T>() where T : class => SceneScope?.Get<T>() ?? Root.Get<T>();
    public static bool TryGet<T>(out T service) where T : class
    {
        if (SceneScope != null && SceneScope.TryGet(out service)) return true;
        if (Root != null && Root.TryGet(out service)) return true;
        service = default;
        return false;
    }
}
