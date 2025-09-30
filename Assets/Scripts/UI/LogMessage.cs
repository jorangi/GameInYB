using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class LogMessage : MonoBehaviour
{
    private int index;
    private WaitForSeconds waitTime = new(2f);
    private TextMeshProUGUI source;
    private RectTransform rect;
    private CanvasGroup canvasGroup;
    private LayoutElement layoutElement;
    private float lifeTime = 2.0f;
    private float fadeDuration = 0.35f;
    public void SetMessage(string message) => source.text = message;
    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        source = GetComponent<TextMeshProUGUI>();
        canvasGroup = GetComponent<CanvasGroup>();
        layoutElement = GetComponent<LayoutElement>();

        float initialHeight = GetCurrentVisualHeight();
        layoutElement.minHeight = -1;
        layoutElement.preferredHeight = initialHeight;
        layoutElement.flexibleHeight = -1;
        StartCoroutine(FadeOut());
    }
    private float GetCurrentVisualHeight()
    {
        float h = rect.rect.height;
        if (source != null)
        {
            source.ForceMeshUpdate();
            float textH = source.preferredHeight;
            h = Mathf.Max(h, textH);
        }
        return h;
    }
    private void Update()
    {
        index = transform.GetSiblingIndex();
        rect.anchoredPosition = new(rect.anchoredPosition.x, Mathf.Lerp(rect.anchoredPosition.y, -10.005f - 35.0f * index, Time.deltaTime));
    }
    private IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(lifeTime);

        float t = 0f;
        float startH = layoutElement.preferredHeight <= 0f ? GetCurrentVisualHeight() : layoutElement.preferredHeight;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / fadeDuration);

            canvasGroup.alpha = 1f - k;
            layoutElement.preferredHeight = Mathf.Lerp(startH, 0f, k);

            var p = rect.parent as RectTransform;
            if (p) LayoutRebuilder.MarkLayoutForRebuild(p);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        layoutElement.preferredHeight = 0f;
        var parent = rect.parent as RectTransform;
        if(parent) LayoutRebuilder.MarkLayoutForRebuild(parent);

        DestroyImmediate(gameObject);
    }
}
