using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiCaller : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(GetRequest("https://jsonplaceholder.typicode.com/posts/1"));
        StartCoroutine(PostRequest("https://jsonplaceholder.typicode.com/posts"));
    }

    //get
    IEnumerator GetRequest(string url)
    {
        UnityWebRequest request = UnityWebRequest.Get(url);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("GET 실패: " + request.error);
        }
        else
        {
            Debug.Log("GET 성공: " + request.downloadHandler.text);
        }
    }

    // post
    IEnumerator PostRequest(string url)
    {
        WWWForm form = new WWWForm();
        form.AddField("userId", "1");
        form.AddField("title", "테스트 제목");
        form.AddField("body", "테스트 내용");

        UnityWebRequest request = UnityWebRequest.Post(url, form);
        request.downloadHandler = new DownloadHandlerBuffer();

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.LogError("POST 실패: " + request.error);
        }
        else
        {
            Debug.Log("POST 성공: " + request.downloadHandler.text);
        }
    }
}
