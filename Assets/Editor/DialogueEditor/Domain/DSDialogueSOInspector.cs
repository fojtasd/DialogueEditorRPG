#if UNITY_EDITOR
using System.Collections.Generic;
using DialogueSystem;
using DialogueSystem.Node;
using UnityEditor;
using UnityEngine;

namespace DialogueEditor.Inspectors {
    [CustomEditor(typeof(NodeSO))]
    public class DSDialogueSOInspector : Editor {
        public override void OnInspectorGUI() {
            serializedObject.Update();

            var nodeSettingsBackingField = $"<{nameof(NodeSO.Model.NodeSettings)}>k__BackingField";
            DrawPropertiesExcluding(serializedObject, nodeSettingsBackingField);

            DrawNodeSettingsPreview((NodeSO)target);

            serializedObject.ApplyModifiedProperties();
        }

        static void DrawNodeSettingsPreview(NodeSO dialogue) {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Node Settings", EditorStyles.boldLabel);

            EditorGUI.indentLevel++;

            IReadOnlyList<INodeSetting> settings = dialogue.Model.NodeSettings;

            if (settings == null || settings.Count == 0) {
                EditorGUILayout.HelpBox("No node settings saved for this dialogue.", MessageType.Info);
                EditorGUI.indentLevel--;
                return;
            }

            foreach (INodeSetting setting in settings) {
                EditorGUILayout.LabelField(setting.Type.ToString(), EditorStyles.boldLabel);
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField(setting.Title, EditorStyles.wordWrappedLabel);
                EditorGUI.indentLevel--;
                GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);
            }

            EditorGUI.indentLevel--;
        }
    }
}
#endif