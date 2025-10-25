using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
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
            //UnityEngine.Debug.Log($"NPC — {npc}");
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
    public static void SetupMonster(string id)
    {
        var svc = ServiceHub.Get<IAddressablesService>();
        var prefab = svc.GetPrefab(id);
        var npc = prefab.GetComponent<NonPlayableCharacter>();
        var profile = svc.GetProfile(id);
        var abilities = new List<IAbility>();

        npc.BindEngage(abilities);
    }
    public static string[] ToArray()
    {
        return npcDic.Keys.ToArray();
    }
}