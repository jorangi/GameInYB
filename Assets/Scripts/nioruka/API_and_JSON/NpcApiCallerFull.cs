using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiManager_All : MonoBehaviour
{
    private const string NPC_API_URL = "https://api-looper.duckdns.org/api/npcs";
    private const string SKILL_API_URL = "https://api-looper.duckdns.org/api/skills";

    void Start()
    {
        StartCoroutine(GetNpcData());
        StartCoroutine(GetSkillData());
    }

    IEnumerator GetNpcData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(NPC_API_URL))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("NPC GET Error: " + request.error);
            }
            else
            {
                string npcJson = request.downloadHandler.text;
                Debug.Log("=== NPC Raw JSON ===");
                Debug.Log(npcJson);

                TryParseNpc(npcJson);
            }
        }
    }

    void TryParseNpc(string json)
    {
        try
        {
            Npc[] npcs = JsonHelper.FromJsonNpc(json);
            Debug.Log($"=== NPC 파싱 결과 ({npcs.Length}명) ===");
            foreach (Npc npc in npcs)
            {
                Debug.Log($"NPC — ID: {npc.id}, 이름: {npc.name}, HP: {npc.hp}, MP: {npc.mp}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("NPC 파싱 실패: " + e.Message);
        }
    }

    [System.Serializable]
    public class Npc
    {
        public int id;
        public string name;
        public string description;
        public int hp;
        public int mp;
    }

    IEnumerator GetSkillData()
    {
        using (UnityWebRequest request = UnityWebRequest.Get(SKILL_API_URL))
        {
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Accept", "application/json");
            request.timeout = 10;

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError ||
                request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError("Skill GET Error: " + request.error);
            }
            else
            {
                string skillJson = request.downloadHandler.text;
                Debug.Log("=== Skill Raw JSON ===");
                Debug.Log(skillJson);

                TryParseSkill(skillJson);
            }
        }
    }

    void TryParseSkill(string json)
    {
        try
        {
            Skill[] skills = JsonHelper.FromJsonSkill(json);
            Debug.Log($"=== Skill 파싱 결과 ({skills.Length}개) ===");
            foreach (Skill sk in skills)
            {
                Debug.Log($"Skill — ID: {sk.id}, 이름: {sk.name}, Power: {sk.power}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("Skill 파싱 실패: " + e.Message);
        }
    }

    [System.Serializable]
    public class Skill
    {
        public int id;
        public string name;
        public int power;
    }

    public static class JsonHelper
    {
        public static Npc[] FromJsonNpc(string json)
        {
            string wrapped = "{\"items\":" + json + "}";
            NpcList wrapper = JsonUtility.FromJson<NpcList>(wrapped);
            return wrapper.items;
        }
        [System.Serializable]
        public class NpcList { public Npc[] items; }
        public static Skill[] FromJsonSkill(string json)
        {
            string wrapped = "{\"items\":" + json + "}";
            SkillList wrapper = JsonUtility.FromJson<SkillList>(wrapped);
            return wrapper.items;
        }
        [System.Serializable]
        public class SkillList { public Skill[] items; }
    }
}
