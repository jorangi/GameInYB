using UnityEngine;
using UnityEngine.UI;
using TMPro;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(ContentSizeFitter))]
public class EnhancedContentSizeFitter : MonoBehaviour
{
    [Header("Width Range")]
    [MinMaxRange(0f, 2000f)]
    [SerializeField] private MinMaxFloat widthRange = new MinMaxFloat(0f, 600f);

    [Header("Height Range (optional)")]
    [SerializeField] private bool clampHeight = false;
    [MinMaxRange(0f, 2000f)]
    [SerializeField] private MinMaxFloat heightRange = new MinMaxFloat(0f, 99999f);

    [Header("Rect Settings")]
    [SerializeField] private bool forceLeftPivot = true;          // pivot.x = 0
    [SerializeField] private bool forceNonStretchAnchors = true;  // 좌우 anchor 고정

    RectTransform rt;
    ContentSizeFitter fitter;
    LayoutElement le;                // 있으면 활용
    TextMeshProUGUI tmp;             // 있으면 정확 계산

    void Awake()  { CacheAndEnforce(); }
    void OnEnable()
    {
        CacheAndEnforce();
        Canvas.willRenderCanvases += OnWillRenderCanvases;
    }
    void OnDisable()
    {
        Canvas.willRenderCanvases -= OnWillRenderCanvases;
    }

    void CacheAndEnforce()
    {
        if (!rt)     rt     = GetComponent<RectTransform>();
        if (!fitter) fitter = GetComponent<ContentSizeFitter>();
        if (!le)     le     = GetComponent<LayoutElement>();
        if (!tmp)    tmp    = GetComponent<TextMeshProUGUI>();

        // 가로는 우리가 관리
        if (fitter.horizontalFit != ContentSizeFitter.FitMode.Unconstrained)
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        // 세로는 보통 PreferredSize 권장
        if (fitter.verticalFit == ContentSizeFitter.FitMode.Unconstrained)
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 왼쪽 기준으로 커지게
        if (forceNonStretchAnchors)
        {
            var aMin = rt.anchorMin; var aMax = rt.anchorMax;
            aMin.x = aMax.x = 0f; // Left
            rt.anchorMin = aMin; rt.anchorMax = aMax;
        }
        if (forceLeftPivot)
        {
            var p = rt.pivot;
            p.x = 0f;
            rt.pivot = p;
        }
    }

    void LateUpdate() { ApplyClamp(); }

    void OnWillRenderCanvases() { ApplyClamp(); }

    void ApplyClamp()
    {
        if (!rt) return;

        widthRange.Sort();
        heightRange.Sort();

        // 1) 내용 기준 선호 폭 계산
        float prefW = ComputePreferredWidth();
        float targetW = Mathf.Clamp(prefW, widthRange.min, widthRange.max);

        // 2) 부모가 자식 폭을 컨트롤하는 레이아웃인지 확인
        var parentLayout = GetComponentInParent<LayoutGroup>();
        bool parentControlsChildWidth = false;
        if (parentLayout is HorizontalOrVerticalLayoutGroup hv)
            parentControlsChildWidth = hv.childControlWidth;
        else if (parentLayout is GridLayoutGroup)
            parentControlsChildWidth = true; // 그리드는 셀 크기로 컨트롤

        // 3) 부모가 컨트롤하면 LayoutElement로 의사 전달, 아니면 RectTransform 직접 세팅
        if (parentControlsChildWidth)
        {
            if (!le) le = gameObject.AddComponent<LayoutElement>();
            // min이 250 같은 값으로 묶여 있으면 안 늘어나니, 여기서 해제/설정
            le.minWidth = -1; // 강제 최소를 없애 고정폭 방지
            le.preferredWidth = targetW;
        }
        else
        {
            // 앵커 좌우 고정 상태에서 실폭 세팅
            SetWidth(targetW);
        }

        // 4) 높이 옵션
        if (clampHeight)
        {
            float prefH = ComputePreferredHeight(targetW);
            float targetH = Mathf.Clamp(prefH, heightRange.min, heightRange.max);

            if (parentControlsChildWidth)
            {
                if (!le) le = gameObject.AddComponent<LayoutElement>();
                le.minHeight = -1;
                le.preferredHeight = targetH;
            }
            else
            {
                SetHeight(targetH);
            }
        }

        // 5) 레이아웃 갱신
        LayoutRebuilder.MarkLayoutForRebuild(rt);
    }

    float ComputePreferredWidth()
    {
        // TMP가 있으면 가장 정확
        if (tmp)
        {
            // 무제한 폭에서 한 줄 기준 선호폭
            var pref = tmp.GetPreferredValues(tmp.text, Mathf.Infinity, Mathf.Infinity);
            return pref.x;
        }

        // 일반 ILayoutElement 기반
        return LayoutUtility.GetPreferredWidth(rt);
    }

    float ComputePreferredHeight(float forWidth)
    {
        if (tmp)
        {
            var pref = tmp.GetPreferredValues(tmp.text, forWidth, Mathf.Infinity);
            return pref.y;
        }
        // 일반 ILayoutElement 기반
        return LayoutUtility.GetPreferredHeight(rt);
    }

    void SetWidth(float w)
    {
        // Stretch가 아니어야 sizeDelta가 폭을 의미
        var s = rt.sizeDelta;
        if (!Mathf.Approximately(s.x, w))
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
    }

    void SetHeight(float h)
    {
        var s = rt.sizeDelta;
        if (!Mathf.Approximately(s.y, h))
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
    }
}
