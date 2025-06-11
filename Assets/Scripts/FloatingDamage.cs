using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class FloatingDamage : MonoBehaviour
{
    public bool isCritical = false;
    private Material runtimeMaterial;

    private TextMeshPro tmp;

    void Awake()
    {
        Color color1 = new Color(1.00f, 0.45f, 0.00f, 1.00f); // #FF7200FF
        Color color2 = new Color(1.00f, 0.92f, 0.32f, 1.00f); // #FFEB51FF
        Color color3 = new Color(1.00f, 0.00f, 0.03f, 1.00f); // #FF0008

        Color color4 = new Color(1.00f, 0.00f, 0.19f, 1.00f); // #FF002FFF
        Color color5 = new Color(1.00f, 0.51f, 0.32f, 1.00f); // #FF8151FF
        Color color6 = new Color(1.000f, 0.7136299f, 0.000f, 1.000f);


        tmp = GetComponent<TextMeshPro>();
        runtimeMaterial = new Material(tmp.fontSharedMaterial);
        tmp.fontMaterial = runtimeMaterial;
        if (!isCritical)
        {
            VertexGradient gradient = new VertexGradient();

            gradient.topLeft = color1;
            gradient.topRight = color1;
            gradient.bottomLeft = color2;
            gradient.bottomRight = color2;
            tmp.outlineColor = color3;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = gradient;
        }
        else
        {
            VertexGradient gradient = new VertexGradient();

            gradient.topLeft = color4;
            gradient.topRight = color4;
            gradient.bottomLeft = color5;
            gradient.bottomRight = color5;
            tmp.outlineColor = color6;
            tmp.enableVertexGradient = true;
            tmp.colorGradient = gradient;
            tmp.transform.localScale = new(1.5f, 1.5f);
        }
        tmp.outlineWidth = 0.01f;
    }
    private void Update()
    {
        tmp.color = new(1, 1, 1, tmp.color.a - Time.deltaTime);
        transform.Translate(new(0, Time.deltaTime * 0.5f, 0));
        if (tmp.color.a < 0.1f)
            Destroy(this.gameObject);
    }
}