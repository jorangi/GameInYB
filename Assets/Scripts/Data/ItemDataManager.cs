using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Text;

public interface IItemRepository
{
    Item GetItem(string id);
}
public sealed class ItemRepositoryAdapter : IItemRepository
{
    public Item GetItem(string id)
    {
        return ItemDataManager.GetItem(id);
    }
}
/// <summary>
/// 아이템 정보를 Item으로 객체화하여 저장, 조회하는 클래스
/// </summary>
public static class ItemDataManager
{
    private static readonly Dictionary<string, Item> itemDic = new();
    private static UniTaskCompletionSource _readyTcs = new();
    public static bool IsReady { get; private set; }
    public static UniTask Ready => _readyTcs.Task;
    /// <summary>
    /// 배열형태로 저장되는 item객체를 담는 클래스. ItemDataManager에서만 접근이 가능함.
    /// </summary>
    [Serializable]
    private class ItemParsingData
    {
        public Item[] items = { };
    }
    [Serializable]
    private class Wrap<T> { public T[] items; }
    /// <summary>
    /// 서버로부터 받은 아이템 json 데이터를 객체화하여 Dictionary에 저장하는 함수
    /// </summary>
    /// <param name="serverData"></param>
    public static void Init(string JSONData)
    {
        JSONData = JSONData.Replace("two-hander", "two_hander");
        try
        {
            var trimmed = JSONData.TrimStart();
            if (trimmed.StartsWith("["))
            {
                var wrapped = $"{{\"items\":{JSONData}}}";
                //Debug.Log(wrapped);
                var data = JsonUtility.FromJson<Wrap<Item>>(wrapped);

                if (data?.items is null)
                {
                    Debug.LogError("[ItemDataManager] 배열을 감싸는 과정에서 실패했습니다.");
                    return;
                }
                foreach (var it in data.items)
                {
                    if (string.IsNullOrEmpty(it.id)) Debug.LogWarning("[ItemDataManager] id가 존재하지 않습니다.");
                    itemDic[it.id] = it;
                }
                Debug.Log($"[ItemDataManager]총 {itemDic.Count}개의 데이터를 불러왔습니다.");
            }
            else if (trimmed.StartsWith("{"))
            {
                var data = JsonUtility.FromJson<ItemParsingData>(JSONData);
                if (data?.items is null)
                {
                    Debug.LogError("[ItemDataManager] 데이터의 root가 items가 아닙니다.");
                    return;
                }
                foreach (var it in data.items)
                {
                    if (string.IsNullOrEmpty(it.id)) Debug.LogWarning("[ItemDataManager] id가 존재하지 않습니다.");
                    itemDic[it.id] = it;
                }
                Debug.Log($"[ItemDataManager]총 {itemDic.Count}개의 데이터를 불러왔습니다.");
            }
            else
            {
                Debug.LogWarning("[ItemDataManager] 받아온 데이터가 객체 혹은 배열이 아닙니다.");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ItemDataManager] {e}");
        }

        IsReady = true;
        if (!_readyTcs.Task.Status.IsCompleted())
        {
            _readyTcs.TrySetResult();
        }
    }
    /// <summary>
    /// Item객체가 담긴 Dictionary를 id를 통해 조회하는 함수, id가 존재하지 않을경우 빈 값 반환
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static Item[] GetItems(params string[] rarity)
    {
        List<Item> results = new();
        foreach (KeyValuePair<string, Item> kv in itemDic)
        {
            if (Array.IndexOf(rarity, kv.Value.rarity) > -1) results.Add(kv.Value);
        }
        return results.ToArray();
    }
    public static Item GetItem(string id)
    {
        if (itemDic.TryGetValue(id, out Item item)) return item;
        return new ItemBuilder().SetName(
            new string[] { $"\"{id}\" is not exist.", $"잘못된 id 조회 : \"{id}\"는 존재하지 않음" }
        ).Build();
    }
    public static void ResetForHotReload()
    {
        itemDic.Clear();
        IsReady = false;
        _readyTcs = new();
    }
}