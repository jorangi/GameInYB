using System.Collections;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHPBarController : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_3 = new(0.3f);
    private PlayableCharacterData Data;
    private int cachedHP;
    public SlicedFilledImage hpBar; // HP 바 이미지
    public SlicedFilledImage hpBarSec; // HP 바 서브 이미지
    public TextMeshProUGUI hpVal; // HP 값 텍스트
    private Coroutine hpSmooth; // HP 바 부드럽게 채우기 코루틴
    private Coroutine playerHitCoroutine; // HP 바 부드럽게 채우기 코루틴
    private const float hpBarSpd = 5.0f; // HP 바 스피드
    [SerializeField] private Image playerHit;
    [SerializeField] private Image playerHealthWarning;
    public async void Awake()
    {
        await PlayableCharacter.ReadyAsync(this.GetCancellationTokenOnDestroy());
        Data = PlayableCharacter.Inst.Data;
        cachedHP = Data.health.HP;
        Data.health.OnHPChanged += HPBarAction;
    }
    private void HPBarAction()
    {
        hpVal.text = $"{Mathf.FloorToInt(Data.health.HP)} / {Mathf.FloorToInt(Data.MaxHP)}";
        if (hpSmooth != null)
            StopCoroutine(hpSmooth);
        if (cachedHP < Data.health.HP) // 힐
        {
            hpBar.fillAmount = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBarSec));
        }
        else // 딜
        {
            hpBarSec.fillAmount = (float)Mathf.FloorToInt(Data.health.HP) / Mathf.FloorToInt(Data.MaxHP);
            hpSmooth = StartCoroutine(HpBarFillsSmooth(hpBar));
            if (playerHitCoroutine != null)StopCoroutine(playerHitCoroutine);
            playerHitCoroutine = StartCoroutine(PlayerHit());
        }
        cachedHP = Data.health.HP;
        float lossRatio = (Data.MaxHP - Data.health.HP) / Data.MaxHP;
        playerHealthWarning.color = new(playerHealthWarning.color.r, playerHealthWarning.color.g, playerHealthWarning.color.b, lossRatio > 0.5f ? lossRatio : 0f);
    }
    private readonly WaitForSeconds playerHitWait = new(0.1f);
    IEnumerator PlayerHit()
    {
        playerHit.color = new(1, 1, 1, 1);
        playerHit.gameObject.SetActive(true);
        yield return playerHitWait;
        float a = 0.0f;
        while (playerHit.color.a > 0.0f)
        {
            a += Time.deltaTime;
            playerHit.color = new(1, 1, 1, 1 - a);
            yield return null;
        }
        playerHit.gameObject.SetActive(false);
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
