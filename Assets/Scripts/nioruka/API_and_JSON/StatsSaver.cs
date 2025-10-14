using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class StatsSaver : MonoBehaviour
{
    private const string SAVE_URL = "https://api-looper.duckdns.org/api/mypage/stats";

    [System.Serializable]
    public class StatsWrapper
    {
        public LoginAndStatsManager.PlayerStats stats;
        public StatsWrapper(LoginAndStatsManager.PlayerStats s)
        {
            stats = s;
        }
    }

    public IEnumerator PostPlayerStats(LoginAndStatsManager.PlayerStats stats)
    {
        StatsWrapper wrapper = new StatsWrapper(stats);
        string jsonBody = JsonUtility.ToJson(wrapper);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(SAVE_URL, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + LoginAndStatsManager.currentPlayer.accessToken);

            Debug.Log("[SAVE] 보낼 JSON: " + jsonBody);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("저장 성공");
            }
            else
            {
                Debug.LogError("저장 실패: " + request.error);
                Debug.LogError("응답 코드: " + request.responseCode);
                Debug.LogError("서버 응답: " + request.downloadHandler.text);
            }
        }
    }
}
