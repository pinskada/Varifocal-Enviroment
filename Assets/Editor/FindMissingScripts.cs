#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FindMissingScriptsInProject
{
    [MenuItem("Tools/Diagnostics/Find Missing Scripts in Project (Prefabs & ScriptableObjects)")]
    public static void FindInProject()
    {
        int missingCount = 0;

        // Prefabs
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) continue;

            var comps = prefab.GetComponentsInChildren<Component>(true);
            foreach (var c in comps)
            {
                if (c == null)
                {
                    missingCount++;
                    Debug.LogWarning($"Missing script in Prefab: {path}", prefab);
                    break; // usually enough to report once per prefab
                }
            }
        }

        // ScriptableObjects (optional but useful)
        string[] soGuids = AssetDatabase.FindAssets("t:ScriptableObject");
        foreach (string guid in soGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var obj = AssetDatabase.LoadMainAssetAtPath(path);
            if (obj == null) continue;

            var so = obj as ScriptableObject;
            if (so == null) continue;

            // Missing scripts on ScriptableObjects are rarer, but can happen via custom editors/serialized refs.
            // We can’t easily enumerate “components” here, but keeping the scan hook is useful if you expand later.
        }

        Debug.Log($"Done. Missing scripts found in Project Prefabs: {missingCount}");
    }
}
#endif
