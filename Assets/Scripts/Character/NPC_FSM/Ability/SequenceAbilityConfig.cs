using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(SequenceAbilityConfig))]
public class SequenceAbilityConfigEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var so = (SequenceAbilityConfig)target;

        // steps와 옵션만 그리기
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("steps"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("linkMaxWait"));
        serializedObject.ApplyModifiedProperties();

        // 파생 값 표시(읽기 전용)
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Derived (read-only)", EditorStyles.boldLabel);

        EditorGUI.BeginDisabledGroup(true);
        EditorGUILayout.FloatField("Enter (from steps[0])", so.DerivedEnter);
        EditorGUILayout.FloatField("Exit  (from steps[0])", so.DerivedExit);
        EditorGUILayout.FloatField("Cooldown (from last)", so.DerivedCooldown);
        EditorGUI.BeginDisabledGroup(false);
    }
}
#endif

[CreateAssetMenu(menuName = "Ability/Sequence")]
public sealed class SequenceAbilityConfig : AbilityConfig
{
    public List<AbilityConfig> steps = new();
    public float linkMaxWait = 0.25f;

    public float DerivedEnter => steps != null && steps.Count > 0 ? GetEnter(steps[0]) : 0f;
    public float DerivedExit => steps != null && steps.Count > 0 ? GetExit(steps[0]) : 0f;
    public float DerivedCooldown => steps != null && steps.Count > 0 ? GetCooldown(steps[^1]) : 0f;

    public override IAbility Build(NonPlayableCharacter npc)
    {
        var built = steps.Where(s => s != null).Select(s => s.Build(npc)).ToList();
        return new SequenceAbility(built);
    }
    static float GetEnter(AbilityConfig abilityConfig) => abilityConfig.enter;
    static float GetExit(AbilityConfig abilityConfig) => abilityConfig.exit;
    static float GetCooldown(AbilityConfig abilityConfig) => abilityConfig.cooldown;
}
