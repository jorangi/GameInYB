using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class StatsSaver : MonoBehaviour
{
    private const string SAVE_URL = "https://api-looper.duckdns.org/api/mypage/stats";

    public IEnumerator PutPlayerStats(LoginAndStatsManager.PlayerStats stats)
    {
        if (string.IsNullOrEmpty(stats.mapid))
            stats.mapid = "none";

        // 빈 배열 전송용 문자열
        if (string.IsNullOrEmpty(stats.equiped)) stats.equiped = "[]";
        if (string.IsNullOrEmpty(stats.inventory)) stats.inventory = "[]";

        string jsonBody = JsonUtility.ToJson(stats);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);

        using (UnityWebRequest request = new UnityWebRequest(SAVE_URL, "PUT"))
        {
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + LoginAndStatsManager.currentPlayer.accessToken);

            Debug.Log("[SAVE] PUT 전송 JSON: " + jsonBody);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Stats 저장 성공 (PUT)");
                StartCoroutine(FindObjectOfType<LoginAndStatsManager>().GetStats());
            }
            else
            {
                Debug.LogError("Stats 저장 실패: " + request.error);
                Debug.LogError("응답 코드: " + request.responseCode);
                Debug.LogError("서버 응답: " + request.downloadHandler.text);
            }
        }
    }
}
