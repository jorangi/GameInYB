using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;


[RequireComponent(typeof(SpriteRenderer))]
[ExecuteInEditMode]
public class ColorChanger : MonoBehaviour
{
    private Coroutine co;
    [SerializeField]
    private SpriteRenderer target;
    public List<ColorTimer> colorTimers = new();
    private void OnValidate()
    {
        if (target == null) target = GetComponent<SpriteRenderer>();
    }
    public IEnumerator ColorChange(ColorTimer colorTimer)
    {
        Debug.Log($"코루틴 시작{target.color} -> {colorTimer.color} : {colorTimer.timer}");
        float timer = colorTimer.timer;
        while (timer > .0f)
        {
            Debug.Log($"{colorTimer.color} 타이머 진행중 {Mathf.Round(timer * 10) * 0.1}");
            timer -= Time.deltaTime;
            target.color = Color.Lerp(target.color, colorTimer.color, colorTimer.timer - timer);
            yield return null;
        }
    }
    public bool IsRunningColorChange() => co != null;
    public void StopColorChange()
    {
        StopCoroutine(co);
        Debug.Log("코루틴 정지");
        co = null;
    }
    public void Change(ColorTimer colorTimer)
    {
        if (IsRunningColorChange()) StopColorChange();
        co = StartCoroutine(ColorChange(colorTimer));
    }
}
[Serializable]
public class ColorTimer
{
    public Color color = Color.white;
    public float timer = 0.0f;
    public ColorTimer(Color color, float timer)
    {
        this.color = color;
        this.timer = timer;
    }
}
