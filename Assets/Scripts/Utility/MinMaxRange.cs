using UnityEngine;
using System;
using UnityEditor;
[Serializable]
public struct MinMaxFloat
{
    public float min;
    public float max;
    public MinMaxFloat(float min, float max) { this.min = min; this.max = max; }
    public void Sort()
    {
        if (min > max) (max, min) = (min, max);
    }
}
[AttributeUsage(AttributeTargets.Field)]
public class MinMaxRangeAttribute : PropertyAttribute
{
    public readonly float minLimit;
    public readonly float maxLimit;
    public readonly bool roundToInt;
    public MinMaxRangeAttribute(float min, float max, bool round = false)
    {
        this.minLimit = min;
        this.maxLimit = max;
        this.roundToInt = round;
    }
}
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(MinMaxRangeAttribute))]
public class MinMaxRangeDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2f + EditorGUIUtility.standardVerticalSpacing;
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (MinMaxRangeAttribute)attribute;
        var minProp = property.FindPropertyRelative("min");
        var maxProp = property.FindPropertyRelative("max");

        if (minProp == null || maxProp == null)
        {
            EditorGUI.LabelField(position, label.text, "Use with MinMaxFloat(min/max)");
            return;
        }
        EditorGUI.BeginProperty(position, label, property);

        Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        EditorGUI.LabelField(labelRect, label);

        float lineH = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        Rect rowRect = new Rect(position.x, position.y, position.width, lineH);

        float fieldW = 70f;
        float pad = 4f;
        Rect minFieldRect = new Rect(rowRect.x, rowRect.y, fieldW, rowRect.height);
        Rect sliderRect = new Rect(rowRect.x + fieldW + pad, rowRect.y, rowRect.width - (fieldW + pad) * 2, rowRect.height);
        Rect maxFieldRect = new Rect(rowRect.x + rowRect.width - fieldW, rowRect.y, fieldW, rowRect.height);

        float minV = minProp.floatValue;
        float maxV = maxProp.floatValue;

        minV = EditorGUI.FloatField(minFieldRect, minV);
        EditorGUI.MinMaxSlider(sliderRect, ref minV, ref maxV, attr.minLimit, attr.maxLimit);
        maxV = EditorGUI.FloatField(maxFieldRect, maxV);

        minV = Mathf.Clamp(minV, attr.minLimit, attr.maxLimit);
        maxV = Mathf.Clamp(maxV, attr.minLimit, attr.maxLimit);
        if (minV > maxV) minV = maxV;

        if (attr.roundToInt)
        {
            minV = Mathf.Round(minV);
            maxV = Mathf.Round(maxV);
        }

        if (!Mathf.Approximately(minV, minProp.floatValue)) minProp.floatValue = minV;
        if (!Mathf.Approximately(maxV, maxProp.floatValue)) maxProp.floatValue = maxV;

        EditorGUI.EndProperty();
    }
}
#endif