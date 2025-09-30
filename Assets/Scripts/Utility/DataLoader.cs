using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;
using UnityEngine.Networking;
using System;
using System.Threading.Tasks;

/// <summary>
/// 배열 타입의 로그시 안의 데이터까지 정상적으로 출력되게 만들어주는 함수
/// </summary>
public static class EnhancedToString
{
    public static string EnToString<T>(this IEnumerable<T> source, string sep = ", ") => source == null ? "null" : string.Join(sep, source.Select(x => $"{x}"));
}
public class DataLoader : MonoBehaviour
{
    private static DataLoader instance;
    private string getResult;
    private readonly Uri baseURI = new("https://developer-looper.duckdns.org/api/");
#if UNITY_EDITOR
    [SerializeField] private TextAsset itemData;
#endif

    private async void Awake()
    {
        //싱글톤 인스턴스 초기화 (DataLoader은 클라이언트 실행시 최초 1회만 발생하여야 함.)
        if (DataLoader.instance != null)
        {
            Destroy(this);
            return;
        }
        DataLoader.instance = this;
        DontDestroyOnLoad(this);
#if UNITY_EDITOR
        ItemDataManager.Init(itemData.text);
        Debug.Log("Editor 모드로 아이템 데이터를 초기화합니다.");
        return;
#endif
        try
        {
            await ItemDataLoad();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }
    private IEnumerator Get(Uri uri)
    {
        UnityWebRequest wW = UnityWebRequest.Get(uri);
        yield return wW.SendWebRequest();

        if (wW.result > UnityWebRequest.Result.Success)
        {
            Debug.Log(wW.error);
        }
        else
        {
            getResult = wW.downloadHandler.text;
            Debug.Log(getResult);
        }
    }
    private async Task ItemDataLoad()
    {
        string itemData = await HttpGetUniTask.GetJsonAsync(new Uri(baseURI, "items"));
        ItemDataManager.Init(itemData);
        Debug.Log("items 데이터를 성공적으로 받아왔습니다.");
    }
}