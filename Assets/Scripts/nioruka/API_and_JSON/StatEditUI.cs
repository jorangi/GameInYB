using UnityEngine;
using TMPro;
using UnityEngine.Playables;

public interface IStatText
{
    public TextMeshProUGUI hp{get; }
    public TextMeshProUGUI atk{get; }
    public TextMeshProUGUI def{get; }
    public TextMeshProUGUI ats{get; }
    public TextMeshProUGUI cri{get; }
    public TextMeshProUGUI crid{get; }
    public TextMeshProUGUI spd{get; }
    public TextMeshProUGUI jmp{get; }
    public TextMeshProUGUI clear{get; }
    public TextMeshProUGUI chapter{get; }
    public TextMeshProUGUI stage{get; }
    public TextMeshProUGUI mapId{get; }
    public TextMeshProUGUI helmet{get; }
    public TextMeshProUGUI armor{get; }
    public TextMeshProUGUI pants{get; }
    public TextMeshProUGUI mainWeapon{get; }
    public TextMeshProUGUI subWeapon{get; }
    public TextMeshProUGUI inventory{get; }
    public TextMeshProUGUI skills{get; }
}
public class StatEditUI : MonoBehaviour, IStatText
{
    public TMP_InputField hpInput;
    public TMP_InputField atkInput;
    public TMP_InputField defInput;
    public TMP_InputField atsInput;
    public TMP_InputField criInput;
    public TMP_InputField cridInput;
    public TMP_InputField spdInput;
    public TMP_InputField jmpInput;
    public TMP_InputField clearInput;
    public TMP_InputField chapterInput;
    public TMP_InputField stageInput;
    public TMP_InputField mapIdInput;
    public TMP_InputField helmetInput;
    public TMP_InputField armorInput;
    public TMP_InputField pantsInput;
    public TMP_InputField mainWeaponInput;
    public TMP_InputField subWeaponInput;
    public TMP_InputField inventoryInput;
    public TMP_InputField skillsInput;

    public TextMeshProUGUI hp { get => hpInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI atk { get => atkInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI def { get => defInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI ats { get => atsInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI cri { get => criInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI crid { get => cridInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI spd { get => spdInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI jmp { get => jmpInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI clear { get => clearInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI chapter { get => chapterInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI stage { get => stageInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI mapId { get => mapIdInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI helmet { get => helmetInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI armor { get => armorInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI pants { get => pantsInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI mainWeapon { get => mainWeaponInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI subWeapon { get => subWeaponInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI inventory { get => inventoryInput.GetComponentInChildren<TextMeshProUGUI>();}
    public TextMeshProUGUI skills { get => skillsInput.GetComponentInChildren<TextMeshProUGUI>();}

    void Start()
    {
        if (PlayableCharacter.Inst.Data != null &&
            PlayableCharacter.Inst.Data.statsDTO != null)
        {
            UpdateUIFromStats(PlayableCharacter.Inst.Data.statsDTO);
        }
    }

    void UpdateUIFromStats(PlayerStats stats)
    {
        hpInput.text = stats.hp.ToString();
        atkInput.text = stats.atk.ToString();
        defInput.text = stats.def.ToString();
        //atsInput.text = stats.hp.ToString();
        criInput.text = stats.atk.ToString();
        cridInput.text = stats.def.ToString();
        spdInput.text = stats.hp.ToString();
        jmpInput.text = stats.atk.ToString();
        clearInput.text = stats.def.ToString();
        chapterInput.text = stats.hp.ToString();
        stageInput.text = stats.atk.ToString();
        mapIdInput.text = stats.def.ToString();
        string[] equipped = stats.equiped.ToString().Replace("[", "").Replace("]", "").Split(',');
        helmetInput.text = equipped[0];
        armorInput.text = equipped[1];
        pantsInput.text = equipped[2];
        mainWeaponInput.text = equipped[3];
        subWeaponInput.text = equipped[4];
        inventoryInput.text = stats.atk.ToString();
        //skillsInput.text = stats.atk.ToString();
    }

    public async void OnClick_Save()
    {
        if (PlayableCharacter.Inst.Data is null ||
            PlayableCharacter.Inst.Data.statsDTO is null)
        {
            Debug.LogError("플레이어 데이터 없음");
            return;
        }

        var stats = PlayableCharacter.Inst.Data.statsDTO;
        stats.hp = float.Parse(string.IsNullOrEmpty(hpInput.text) ? hpInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : hpInput.text);
        stats.atk = float.Parse(string.IsNullOrEmpty(atkInput.text) ? atkInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : atkInput.text);
        //stats.ats = float.Parse(string.IsNullOrEmpty(atsInput.text) ? atsInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : hpInput.text);
        stats.def = float.Parse(string.IsNullOrEmpty(defInput.text) ? defInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : defInput.text);
        stats.cri = float.Parse(string.IsNullOrEmpty(criInput.text) ? criInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : criInput.text);
        stats.crid = float.Parse(string.IsNullOrEmpty(cridInput.text) ? cridInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : cridInput.text);
        stats.spd = float.Parse(string.IsNullOrEmpty(spdInput.text) ? spdInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : spdInput.text);
        stats.jmp = int.Parse(string.IsNullOrEmpty(jmpInput.text) ? jmpInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : jmpInput.text);
        stats.clear = int.Parse(string.IsNullOrEmpty(clearInput.text) ? clearInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : clearInput.text);
        stats.chapter = int.Parse(string.IsNullOrEmpty(chapterInput.text) ? chapterInput.transform.Find("Text Area").Find("Placeholder").GetComponent<TextMeshProUGUI>().text : chapterInput.text);
        stats.mapid = mapIdInput.text;
        stats.equiped = "[\"" + helmetInput.text + "\",\"" + armorInput.text + "\",\"" + pantsInput.text + "\",\"" + mainWeaponInput.text + "\",\"" + subWeaponInput.text + "\"]";
        stats.inventory = inventoryInput.text;
        //stats.skills = skillsInput.text;

        Debug.Log($"[SAVE완료] 수정된 Stats → {stats}");

        var tokenProvider = new PlayableCharacterAccessTokenProvider();
        var uiAdapter = new LoginAndStatsManagerAdapter(FindAnyObjectByType<LoginAndStatsManager>());
        var refreshers = new IStatsRefresher[]
        {
            new UIStatsRefresher(uiAdapter)
        };
        IStatsSaver saver = new StatsSaver(tokenProvider, refreshers);

        await saver.SavePlayerStatsAsync(stats);
    }
}
