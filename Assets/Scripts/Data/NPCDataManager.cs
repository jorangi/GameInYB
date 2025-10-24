using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using static ApiManager_All;

public interface INPCRepository
{
    public Npc GetNPC(string id);
}
public sealed class NPCRepositoryAdapter : INPCRepository
{
    public Npc GetNPC(string id)
    {
        return NPCDataManager.GetNPC(id);
    }
}
public static class NPCDataManager
{
    private static readonly Dictionary<string, Npc> npcDic = new();
    private static UniTaskCompletionSource _readyTcs = new();
    public static bool IsReady { get; private set; }

    public static void Init(Npc[] npcs)
    {
        foreach (var npc in npcs)
        {
            npcDic[npc.id] = npc;
            //UnityEngine.Debug.Log($"NPC — {npc}");
        }
    }
    public static Npc GetNPC(string id)
    {
        UnityEngine.Debug.Log(id);
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
}