using UnityEngine;
using TMPro;

public class StatEditUI : MonoBehaviour
{
    public TMP_InputField hpInput;
    public TMP_InputField atkInput;
    public TMP_InputField defInput;

    void Start()
    {
        if (LoginAndStatsManager.currentPlayer != null &&
            LoginAndStatsManager.currentPlayer.stats != null)
        {
            UpdateUIFromStats(LoginAndStatsManager.currentPlayer.stats);
        }
    }

    void UpdateUIFromStats(LoginAndStatsManager.PlayerStats stats)
    {
        hpInput.text = stats.hp.ToString();
        atkInput.text = stats.atk.ToString();
        defInput.text = stats.def.ToString();
    }

    public void OnClick_Save()
{
    if (LoginAndStatsManager.currentPlayer == null ||
        LoginAndStatsManager.currentPlayer.stats == null)
    {
        Debug.LogError("플레이어 데이터 없음");
        return;
    }

    var stats = LoginAndStatsManager.currentPlayer.stats;
    stats.hp = float.Parse(hpInput.text);
    stats.atk = float.Parse(atkInput.text);
    stats.def = float.Parse(defInput.text);

    Debug.Log($"[SAVE완료] 수정된 Stats → HP:{stats.hp}, ATK:{stats.atk}, DEF:{stats.def}");

    var saver = FindObjectOfType<StatsSaver>();
    if (saver != null)
    {
        StartCoroutine(saver.PutPlayerStats(stats));
    }
    else
    {
        Debug.LogError("StatsSaver 없음");
    }
}

}
