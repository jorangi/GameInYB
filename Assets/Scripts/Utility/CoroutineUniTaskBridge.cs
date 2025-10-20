using UnityEngine;
using System;
using Cysharp.Threading.Tasks;
using System.Collections;

public static class CoroutineUniTaskBridge
{
    public static UniTask RunAsUniTask(this MonoBehaviour host, Func<IEnumerator> coroutineFactory)
    {
        var tcs = new UniTaskCompletionSource();
        host.StartCoroutine(Wrap(coroutineFactory, tcs));
        return tcs.Task;

        static IEnumerator Wrap(Func<IEnumerator> factory, UniTaskCompletionSource tcs)
        {
            yield return factory();
            tcs.TrySetResult();
        }
    }
}
