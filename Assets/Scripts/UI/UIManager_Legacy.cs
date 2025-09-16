using TMPro;
using UnityEngine;

public class UIManager_Legacy : MonoBehaviour
{
    private SlicedFilledImage hpBar, hpBarSec;
    private TextMeshProUGUI hpVal;
    private RectTransform stats;
    private RectTransform inventory;
    private void Awake()
    {
        if (hpBar == null) hpBar = transform.Find("healthBar").Find("healthBarMask").Find("health").GetComponent<SlicedFilledImage>();
        if (hpBarSec == null) hpBarSec = transform.Find("healthBar").Find("healthBarMask").Find("healthSec").GetComponent<SlicedFilledImage>();
        if (hpVal == null) hpVal = transform.Find("healthBar").Find("val").GetComponent<TextMeshProUGUI>();
    }
}
