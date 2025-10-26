using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


[CreateAssetMenu(menuName="NPCProfile")]
public sealed class NPCProfile : ScriptableObject
{
    public string id;
    public bool isAggressive;
    public NonPlayableCharacterData data;
    public List<AbilityConfig> abilityConfigs = new();
}

/*
[CustomEditor(typeof(NPCProfile))]
public class NPCProfileEditor : Editor
{
    // 스크립트의 모든 직렬화된 프로퍼티를 담을 변수들
    SerializedProperty idProp;
    SerializedProperty isAggressiveProp;
    SerializedProperty dataProp;

    SerializedProperty useMeleeComboProp;
    SerializedProperty meleeParamsProp;

    SerializedProperty useDashChargeProp;
    SerializedProperty dashParamsProp;

    // 에디터가 활성화될 때 프로퍼티들을 찾아서 연결합니다.
    private void OnEnable()
    {
        idProp = serializedObject.FindProperty("id");
        isAggressiveProp = serializedObject.FindProperty("isAggressive");
        dataProp = serializedObject.FindProperty("data");

        useMeleeComboProp = serializedObject.FindProperty("useMeleeCombo");
        meleeParamsProp = serializedObject.FindProperty("meleeParams");

        useDashChargeProp = serializedObject.FindProperty("useDashCharge");
        dashParamsProp = serializedObject.FindProperty("dashParams");
    }

    // 인스펙터 GUI를 다시 그립니다.
    public override void OnInspectorGUI()
    {
        // SerializedObject의 상태를 업데이트합니다. (필수)
        serializedObject.Update();

        // 항상 표시될 기본 필드들
        EditorGUILayout.PropertyField(idProp);
        EditorGUILayout.PropertyField(isAggressiveProp);

        EditorGUILayout.Space(10); // 보기 좋게 공백 추가

        // --- Melee Combo 섹션 ---
        // useMeleeCombo 체크박스를 그립니다.
        EditorGUILayout.PropertyField(useMeleeComboProp);

        // useMeleeComboProp의 bool 값이 true일 때만 meleeParamsProp를 그립니다.
        if (useMeleeComboProp.boolValue)
        {
            // 들여쓰기를 한 단계 추가해서 계층 구조를 보여줍니다.
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(meleeParamsProp);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(10); // 보기 좋게 공백 추가

        // --- Dash Charge 섹션 ---
        // useDashCharge 체크박스를 그립니다.
        EditorGUILayout.PropertyField(useDashChargeProp);

        // useDashChargeProp의 bool 값이 true일 때만 dashParamsProp를 그립니다.
        if (useDashChargeProp.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(dashParamsProp);
            EditorGUI.indentLevel--;
        }

        // 변경된 프로퍼티 값들을 실제 객체에 적용합니다. (Undo/Redo 지원을 위해 필수)
        serializedObject.ApplyModifiedProperties();
    }
}
*/