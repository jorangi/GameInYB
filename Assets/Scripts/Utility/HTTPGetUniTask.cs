using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public static class HttpGetUniTask
{
    public static async UniTask<string> GetJsonAsync(
        Uri uri,
        int timeoutSeconds = 15,
        Dictionary<string, string> headers = null,
        CancellationToken cancellationToken = default)
    {
        using (var req = UnityWebRequest.Get(uri))
        {
            req.timeout = timeoutSeconds;
            if (headers != null)
            {
                foreach (var kv in headers)
                    req.SetRequestHeader(kv.Key, kv.Value);
            }

            await req.SendWebRequest().ToUniTask(cancellationToken: cancellationToken);

            if (req.result != UnityWebRequest.Result.Success)
                throw new Exception(req.error);

            return req.downloadHandler.text;
        }
    }
}