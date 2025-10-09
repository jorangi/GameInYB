using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace nioruka.API_and_JSON
{
    public class ApiCallerRawJson : MonoBehaviour
    {
        public Text responseText;

        const string GET_URL = "https://jsonplaceholder.typicode.com/posts/1";
        const string POST_URL = "https://jsonplaceholder.typicode.com/posts";

        public void OnClick_GetRawJson()
        {
            StartCoroutine(GetRawJson(GET_URL));
        }

        public void OnClick_PostJson()
        {
            string jsonBody = "{\"title\":\"foo\",\"body\":\"bar\",\"userId\":1}";
            StartCoroutine(PostJson(POST_URL, jsonBody));
        }

        //GET
        IEnumerator GetRawJson(string url)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Accept", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("GET Error: " + request.error);
                    if (responseText) responseText.text = "GET Error: " + request.error;
                }
                else
                {
                    string json = request.downloadHandler.text;
                    Debug.Log("GET JSON: " + json);

                    if (responseText)
                    {
                        responseText.text = json;
                    }
                    TryParseSinglePost(json);
                }
            }
        }

        //POST
        IEnumerator PostJson(string url, string json)
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "application/json");
                request.timeout = 10;

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError ||
                    request.result == UnityWebRequest.Result.ProtocolError)
                {
                    Debug.LogError("POST Error: " + request.error);
                    if (responseText) responseText.text = "POST Error: " + request.error;
                }
                else
                {
                    string responseJson = request.downloadHandler.text;
                    Debug.Log("POST Response: " + responseJson);
                    if (responseText) responseText.text = responseJson;
                }
            }
        }
        [Serializable]
        public class Post
        {
            public int userId;
            public int id;
            public string title;
            public string body;
        }

        void TryParseSinglePost(string json)
        {
            try
            {
                Post p = JsonUtility.FromJson<Post>(json);
                Debug.Log($"Parsed Post -> id:{p.id}, title:{p.title}");
            }
            catch (Exception e)
            {
                Debug.LogWarning("파싱 실패: " + e);
            }
        }
        public static class JsonHelper
        {
            public static T[] FromJson<T>(string json)
            {
                string wrapped = "{\"items\":" + json + "}";
                Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrapped);
                return wrapper.items;
            }

            [Serializable]
            private class Wrapper<T> { public T[] items; }
        }
    }
}