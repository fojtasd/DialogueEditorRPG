#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Utilities {
    public static class StyleUtility {
        const string StylesRoot = "Assets/Editor/DialogueEditor/Domain/Styles/";

        public static void AddClasses(this VisualElement element,
                                      params string[] classNames) {
            foreach (string className in classNames) {
                element.AddToClassList(className);
            }
        }

        public static void AddStyleSheets(this VisualElement element,
                                          params string[] styleSheetNames) {
            foreach (string name in styleSheetNames) {
                var path = $"{StylesRoot}{name}.uss";
                var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(path);

                if (styleSheet == null) {
                    Debug.LogError($"USS not found at path: {path}");
                    continue;
                }

                element.styleSheets.Add(styleSheet);
            }
        }
    }
}
#endif