using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using static ApiManager_All;

public interface INPCRepository
{
    public Npc GetNPC(string id);
    public string[] ToArray();
}
public sealed class NPCRepositoryAdapter : INPCRepository
{
    public Npc GetNPC(string id)
    {
        return NPCDataManager.GetNPC(id);
    }
    public string[] ToArray()
    {
        return NPCDataManager.ToArray();
    }
}
public static class NPCDataManager
{
    private static readonly Dictionary<string, Npc> npcDic = new();
    private static UniTaskCompletionSource _readyTcs = new();
    public static bool IsReady { get; private set; }
    public static UniTask Ready => _readyTcs.Task;
    public static void Init(Npc[] npcs)
    {
        foreach (var npc in npcs)
        {
            npcDic[npc.id] = npc;
        }
        IsReady = true;
        if (!_readyTcs.Task.Status.IsCompleted())
        {
            _readyTcs.TrySetResult();
        }
    }
    public static Npc GetNPC(string id)
    {
        try
        {
            npcDic.TryGetValue(id, out Npc npc);
            return npc;
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[Error: GetNPC]id: {id}\n{e}");
            return null;
        }
    }
    public static GameObject SetupMonster(string id, Vector3 pos, Transform parent = null)
    {
        var svc = ServiceHub.Get<IAddressablesService>();
        var prefab = svc.GetPrefab(id);
        GameObject gameObject = UnityEngine.Object.Instantiate(prefab, pos, Quaternion.identity);
        var npc = gameObject.GetComponent<INPCProfileInjector>();
        var profile = svc.GetProfile(id);
        Debug.Log(profile.id);
        npc.InjectProfile(profile);
        var abilities = AbilityFactory.BuildFromProfile(profile, npc as NonPlayableCharacter);
        npc.BindAbilites(abilities);

        return gameObject;
    }
    public static string[] ToArray()
    {
        return npcDic.Keys.ToArray();
    }
}
public static class AbilityFactory
{
    public static List<IAbility> BuildFromProfile(NPCProfile profile, NonPlayableCharacter npc)
    {
        var list = new List<IAbility>(profile.abilityConfigs.Count);
        foreach (var cfg in profile.abilityConfigs)
        {
            if (cfg == null) continue;
            var a = cfg.Build(npc);
            if (a != null) list.Add(a);
        }
        return list;
    }
}