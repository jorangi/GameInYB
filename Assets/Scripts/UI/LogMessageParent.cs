using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System;

public class LogMessageParent : MonoBehaviour
{
    private AsyncOperationHandle<GameObject> _prefabHandle;
    private GameObject _prefab;
    private async void Awake()
    {
        var ct = this.GetCancellationTokenOnDestroy();

        try
        {
            _prefabHandle = Addressables.LoadAssetAsync<GameObject>("Prefabs/LogMessage");
            _prefab = await _prefabHandle.ToUniTask(cancellationToken: ct);

            if (_prefab is null) Debug.LogWarning($"[LogMessageParent] Addressable 'prefabs/LogMessage'가 로딩되지 않음.");
        }
        catch (OperationCanceledException) { }
        catch (Exception e)
        {
            Debug.LogWarning($"[LogMessageParent]로드 실패함: {e}");
        }
    }
    private void OnDestroy()
    {
        if (_prefabHandle.IsValid()) Addressables.Release(_prefabHandle);
    }
    public void Spawn(string message)
    {
        GameObject obj = Instantiate(_prefab, transform);
        obj.GetComponent<LogMessage>().SetMessage(message);
    }
}