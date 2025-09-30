using UnityEditor;
using UnityEngine;

public class PrefabReplacer : EditorWindow
{
    private GameObject prefabToReplaceWith;

    [MenuItem("Tools/Prefab Replacer")]
    public static void ShowWindow()
    {
        GetWindow<PrefabReplacer>("Prefab Replacer");
    }

    private void OnGUI()
    {
        GUILayout.Label("교체할 프리팹을 아래에 등록하세요", EditorStyles.boldLabel);
        prefabToReplaceWith = (GameObject)EditorGUILayout.ObjectField("New Prefab", prefabToReplaceWith, typeof(GameObject), false);

        if (GUILayout.Button("선택한 오브젝트들을 프리팹으로 교체"))
        {
            ReplaceSelectedObjects();
        }
    }

    private void ReplaceSelectedObjects()
    {
        if (prefabToReplaceWith == null)
        {
            EditorUtility.DisplayDialog("경고", "교체할 프리팹을 먼저 등록해주세요.", "확인");
            return;
        }

        GameObject[] selectedObjects = Selection.gameObjects;

        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("경고", "하이어라키에서 교체할 오브젝트를 먼저 선택해주세요.", "확인");
            return;
        }

        foreach (GameObject obj in selectedObjects)
        {
            Transform parent = obj.transform.parent;

            // 새 프리팹 인스턴스 생성
            GameObject newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefabToReplaceWith, parent);
            
            // Undo(Ctrl+Z) 기능을 위해 생성 등록
            Undo.RegisterCreatedObjectUndo(newObject, "Replace with prefab");

            // --- 여기가 핵심! RectTransform인지 일반 Transform인지 확인 ---
            if (obj.transform is RectTransform originalRectTransform)
            {
                // UI 오브젝트일 경우 (RectTransform 처리)
                RectTransform newRectTransform = newObject.GetComponent<RectTransform>();
                if (newRectTransform != null)
                {
                    // RectTransform의 모든 주요 속성 복사
                    newRectTransform.anchorMin = originalRectTransform.anchorMin;
                    newRectTransform.anchorMax = originalRectTransform.anchorMax;
                    newRectTransform.pivot = originalRectTransform.pivot;
                    newRectTransform.anchoredPosition = originalRectTransform.anchoredPosition;
                    newRectTransform.sizeDelta = originalRectTransform.sizeDelta;
                    
                    // Z축 위치와 같은 추가 정보도 복사
                    newRectTransform.anchoredPosition3D = originalRectTransform.anchoredPosition3D;
                    newRectTransform.SetSiblingIndex(originalRectTransform.GetSiblingIndex());
                }
            }
            else
            {
                // 일반 오브젝트일 경우 (Transform 처리)
                newObject.transform.position = obj.transform.position;
                newObject.transform.SetSiblingIndex(obj.transform.GetSiblingIndex());
            }

            // 공통 속성인 회전과 스케일은 항상 복사
            newObject.transform.rotation = obj.transform.rotation;
            newObject.transform.localScale = obj.transform.localScale;

            // 기존 오브젝트 즉시 파괴
            Undo.DestroyObjectImmediate(obj);
        }

        Debug.Log(selectedObjects.Length + "개의 오브젝트를 성공적으로 교체했습니다.");
    }
}