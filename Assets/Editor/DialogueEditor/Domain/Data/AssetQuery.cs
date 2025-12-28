#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

static class AssetQuery {
    public static List<T> LoadAllFromFolders<T>(params string[] folders) where T : ScriptableObject {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", folders);
        var list = new List<T>(guids.Length);

        foreach (var guid in guids) {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
                list.Add(asset);
        }

        // stabilní řazení (ať to neskáče)
        return list.OrderBy(a => a.name).ToList();
    }
}
#endif
