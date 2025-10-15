using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerHPBarController : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_3 = new(0.3f);
    private PlayableCharacterData Data;
    private int cachedHP;
    public SlicedFilledImage hpBar; // HP 바 이미지
    public SlicedFilledImage hpBarSec; // HP 바 서브 이미지
    public TextMeshProUGUI hpVal; // HP 값 텍스트
    private Coroutine hpSmooth; // HP 바 부드럽게 채우기 코루틴
    private const float hpBarSpd = 5.0f; // HP 바 스피드
    public void Awake()
    {
        Data = PlayableCharacter.Inst.Data;
        cachedHP = Data.health.HP;
        Data.health.OnHPChanged += HPBarAction;
    }
    private void HPBarAction()
    {
        hpVal.text = $"{Mathf.FloorToInt(Data.health.HP)} / {Mathf.FloorToInt(Data.MaxHP)}";
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        if (cachedHP < Data.health.HP)
        {
            hpBar.fillAmount = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
        }
        else
        {
            hpBarSec.fillAmount = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
        }
        cachedHP = Data.health.HP;
    }
    IEnumerator HpBarFillsSmooth(SlicedFilledImage bar)
    {
        yield return _waitForSeconds0_3;
        while (Mathf.Abs(bar.fillAmount - (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP)) > 0.01f)
        {
            bar.fillAmount = Mathf.Lerp(bar.fillAmount, (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP), Time.deltaTime * hpBarSpd);
            yield return null;
        }
        bar.fillAmount = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
    }
}
