using UnityEngine;
using TMPro;

public class FloatingDamage : MonoBehaviour
{
    private TextMeshPro tmp;

    void Awake()
    {
        tmp = GetComponent<TextMeshPro>();
    }
    private float alpha = 2;
    private void Update()
    {
        alpha -= Time.deltaTime;
        tmp.color = new(1, 1, 1, alpha);
        transform.Translate(new(0, Time.deltaTime * 0.5f, 0));
        if (tmp.color.a < 0.1f)
            Destroy(this.gameObject);
    }
}