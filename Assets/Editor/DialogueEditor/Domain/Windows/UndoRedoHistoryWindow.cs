#if UNITY_EDITOR
using System.Collections.Generic;
using DialogueEditor.Domain.Undo;
using DialogueEditor.Elements;
using UnityEditor;
using UnityEngine;

namespace DialogueEditor.Windows {
    public class UndoRedoHistoryWindow : EditorWindow {
        DialogueEditorWindow _ownerWindow;
        GraphViewElement _graphView;
        Vector2 _undoScroll;
        Vector2 _redoScroll;

        public static void Open(DialogueEditorWindow ownerWindow, GraphViewElement graphView) {
            if (ownerWindow == null)
                throw new System.ArgumentNullException(nameof(ownerWindow));

            var window = GetWindow<UndoRedoHistoryWindow>("Undo / Redo History");
            window.Init(ownerWindow, graphView);
            window.Show();
            window.Focus();
        }

        void Init(DialogueEditorWindow ownerWindow, GraphViewElement graphView) {
            _ownerWindow = ownerWindow;
            _graphView = graphView;
            minSize = new Vector2(420f, 260f);
        }

        void OnEnable() {
            minSize = new Vector2(420f, 260f);
        }

        void OnGUI() {
            EnsureGraphBound();

            if (_graphView == null) {
                EditorGUILayout.HelpBox("Graph view unavailable. Open the dialogue editor first.", MessageType.Warning);
                using (new EditorGUILayout.HorizontalScope()) {
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("Retry", GUILayout.Width(100f)))
                        EnsureGraphBound(force: true);
                    GUILayout.FlexibleSpace();
                }

                return;
            }

            GraphUndoManager undoManager = _graphView.UndoManager;
            if (undoManager == null) {
                EditorGUILayout.HelpBox("Undo manager unavailable for the current graph.", MessageType.Warning);
                return;
            }

            EditorGUILayout.LabelField("Undo / Redo Controls", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope()) {
                EditorGUI.BeginDisabledGroup(!undoManager.CanUndo);
                if (GUILayout.Button("Undo", GUILayout.Width(80f)))
                    undoManager.Undo();
                EditorGUI.EndDisabledGroup();

                EditorGUI.BeginDisabledGroup(!undoManager.CanRedo);
                if (GUILayout.Button("Redo", GUILayout.Width(80f)))
                    undoManager.Redo();
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
            }

            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope()) {
                DrawStack("Undo stack (next to undo shown first)", undoManager.GetUndoHistory(), ref _undoScroll, "No actions to undo.");
                GUILayout.Space(10f);
                DrawStack("Redo stack (next to redo shown first)", undoManager.GetRedoHistory(), ref _redoScroll, "No actions to redo.");
            }
        }

        void OnInspectorUpdate() {
            Repaint();
        }

        void DrawStack(string title, IReadOnlyList<string> entries, ref Vector2 scrollPosition, string emptyMessage) {
            using (new EditorGUILayout.VerticalScope(GUILayout.MinWidth(180f))) {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);

                if (entries == null || entries.Count == 0) {
                    EditorGUILayout.HelpBox(emptyMessage, MessageType.Info);
                    return;
                }

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
                for (int index = 0; index < entries.Count; ++index) {
                    string label = entries[index];
                    EditorGUILayout.LabelField($"{index + 1}. {label}", EditorStyles.wordWrappedLabel);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        void EnsureGraphBound(bool force = false) {
            if (_graphView != null && !force)
                return;

            if (_ownerWindow == null)
                return;

            GraphViewElement latest = _ownerWindow.GetGraphViewElement();
            if (latest != null)
                _graphView = latest;
        }
    }
}
#endif

