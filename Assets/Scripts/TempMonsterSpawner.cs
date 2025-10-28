using UnityEngine;

public class TempMonsterSpawner : MonoBehaviour
{
    void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            NPCDataManager.SetupMonster(transform.GetChild(i).name, transform.GetChild(i).position);
        }
    }
}
