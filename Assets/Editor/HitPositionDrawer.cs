using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(HitPosition))]
public class HitPositionDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // EnumProperty가 아니라 IntProperty로 저장되어 있기 때문에 intValue 접근해야 함
        var current = (HitPosition)property.intValue;
        var next = (HitPosition)EditorGUI.EnumFlagsField(position, label, current);

        if (current != next)
        {
            property.intValue = (int)next;
        }

        EditorGUI.EndProperty();
    }
}
#endif