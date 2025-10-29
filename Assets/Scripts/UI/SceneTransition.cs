using System.Collections;
using UnityEngine;

public class SceneTransition : MonoBehaviour
{
    [SerializeField] private Transform fadePanel;
    readonly WaitForSecondsRealtime waitTime = new(0.01f);
    private const int COLS = 16;
    private const int ROWS = 9;
    public bool end = false;

    CanvasGroup c;
    private void Awake()
    {
        c = GetComponent<CanvasGroup>();
        fadePanel = transform;
        StartCoroutine(FadeOut());
    }
    public void OnEnable()
    {
        c.alpha = 1.0f;
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(false);
        }
        end = false;
    }
    public IEnumerator FadeOut()
    {
        int topRow = 0;
        int bottomRow = ROWS - 1;
        int leftCol = 0;
        int rightCol = COLS - 1;

        int count = 1;
        int total = COLS * ROWS;

        while (topRow <= bottomRow && leftCol <= rightCol)
        {
            for (int j = leftCol; j <= rightCol; j++)
            {
                int index = topRow * COLS + j + 1;

                if (index <= total && fadePanel.childCount >= index)
                {
                    fadePanel.GetChild(index - 1).gameObject.SetActive(true);
                    yield return waitTime;
                }

                count++;
                if (count > total) break;
            }
            topRow++;
            if (count > total) break;


            for (int i = topRow; i <= bottomRow; i++)
            {
                int index = i * COLS + rightCol + 1;

                if (index <= total && fadePanel.childCount >= index)
                {
                    fadePanel.GetChild(index - 1).gameObject.SetActive(true);
                    yield return waitTime;
                }

                count++;
                if (count > total) break;
            }
            rightCol--;
            if (count > total) break;


            if (topRow <= bottomRow)
            {
                for (int j = rightCol; j >= leftCol; j--)
                {
                    int index = bottomRow * COLS + j + 1;

                    if (index <= total && fadePanel.childCount >= index)
                    {
                        fadePanel.GetChild(index - 1).gameObject.SetActive(true);
                        yield return waitTime;
                    }

                    count++;
                    if (count > total) break;
                }
                bottomRow--;
            }
            if (count > total) break;


            if (leftCol <= rightCol)
            {
                for (int i = bottomRow; i >= topRow; i--)
                {
                    int index = i * COLS + leftCol + 1;

                    if (index <= total && fadePanel.childCount >= index)
                    {
                        fadePanel.GetChild(index - 1).gameObject.SetActive(true);
                        yield return waitTime;
                    }

                    count++;
                    if (count > total) break;
                }
                leftCol++;
            }
        }

        end = true;
    }
    public IEnumerator FadeIn()
    {
        gameObject.SetActive(true);
        for (int i = 0; i < transform.childCount; i++)
        {
            transform.GetChild(i).gameObject.SetActive(true);
        }
        float a = 1.0f;
        while (a > 0.0f)
        {
            c.alpha = a;
            a -= Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
    public void OnFadeIn() => StartCoroutine(FadeIn());
}