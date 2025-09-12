#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using Unity.EditorCoroutines.Editor;
using System.Data.Common;

[CustomEditor(typeof(ColorChanger))]
public class ColorChangerEditor : Editor
{
    private EditorCoroutine co = null;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        SerializedProperty sp = serializedObject.FindProperty("target");
        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.PropertyField(sp);
        EditorGUI.EndDisabledGroup();
        EditorGUILayout.Space();
        ColorChanger colorChanger = (ColorChanger)target;
        EditorGUILayout.BeginHorizontal();
        {
            EditorGUILayout.LabelField("Color Timer", EditorStyles.boldLabel);
            if (GUILayout.Button("+", GUILayout.Width(20))) colorChanger.colorTimers.Add(new ColorTimer(Color.white, 0));
        }
        EditorGUILayout.EndHorizontal();
        List<ColorTimer> forRemove = new();
        foreach (ColorTimer colorTimer in colorChanger.colorTimers)
        {
            EditorGUILayout.BeginHorizontal();
            {
                colorTimer.color = EditorGUILayout.ColorField(colorTimer.color);
                colorTimer.timer = EditorGUILayout.FloatField(colorTimer.timer, GUILayout.Width(50));

                if (GUILayout.Button("▶", GUILayout.Width(20)))
                {
                    if (Application.isPlaying)
                        colorChanger.Change(colorTimer);
                    else
                    {
                        Debug.Log(colorChanger.IsRunningColorChange());
                        if (co != null)
                        {
                            EditorCoroutineUtility.StopCoroutine(co);
                            co = EditorCoroutineUtility.StartCoroutineOwnerless(colorChanger.ColorChange(colorTimer));
                        }
                        else
                        {
                            co = null;
                            co = EditorCoroutineUtility.StartCoroutineOwnerless(colorChanger.ColorChange(colorTimer));
                            Debug.Log("조건분기 입장");
                        }
                    }
                }

                if (GUILayout.Button("-", GUILayout.Width(20)))
                {
                    forRemove.Add(colorTimer);
                }
            }
            EditorGUILayout.EndHorizontal();

        }
        foreach (ColorTimer item in forRemove)
        {
            colorChanger.colorTimers.Remove(item);
        }
        serializedObject.ApplyModifiedProperties();
    }
}
#endif