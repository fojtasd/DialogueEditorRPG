#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DialogueEditor.Elements;
using DialogueSystem.Node;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Contracts.Contracts;

namespace DialogueEditor.Windows {
    public class GraphInspectorWindow : OdinEditorWindow {
        enum InspectorPanel {
            Overview
        }

        const string PlayerStatsPrefsKey = "DialogueEditor.GraphInspector.PlayerStatsState";

        DialogueEditorWindow _ownerWindow;
        GraphViewElement _graphViewElement;
        GraphViewElement _subscribedGraph;
        InspectorPanel _activePanel = InspectorPanel.Overview;
        readonly List<AccessibilityEntry> _accessibilityEntries = new();
        Vector2 _overviewScroll;
        readonly Dictionary<AttributeType, int> _playerAttributes = new();
        readonly Dictionary<SkillType, int> _playerSkills = new();
        bool _statsFoldoutExpanded = true;

        public static GraphInspectorWindow Open(DialogueEditorWindow ownerWindow, GraphViewElement graphViewElement) {
            if (graphViewElement == null)
                throw new ArgumentNullException(nameof(graphViewElement));

            var window = GetWindow<GraphInspectorWindow>("Graph Inspector");
            window.Init(ownerWindow, graphViewElement);
            window.Show();
            window.Focus();
            return window;
        }

        void Init(DialogueEditorWindow ownerWindow, GraphViewElement graphViewElement) {
            _ownerWindow = ownerWindow;
            _graphViewElement = graphViewElement;
            titleContent = new GUIContent("Graph Inspector");
            minSize = new Vector2(320, 200);
            BindToGraph(_graphViewElement);
            EditorApplication.update -= HandleEditorUpdate;
            EditorApplication.update += HandleEditorUpdate;
        }

        new void OnEnable() {
            EditorApplication.update += HandleEditorUpdate;
            LoadPlayerStatsState();
        }

        new void OnDisable() {
            EditorApplication.update -= HandleEditorUpdate;
            SavePlayerStatsState();
            BindToGraph(null);
        }

        void HandleEditorUpdate() {
            if (_graphViewElement == null) {
                TrySyncWithOwner();
            }

            Repaint();
        }

        void TrySyncWithOwner() {
            if (_ownerWindow == null)
                return;

            GraphViewElement latest = _ownerWindow.GetGraphViewElement();
            if (ReferenceEquals(_graphViewElement, latest))
                return;

            _graphViewElement = latest;
            BindToGraph(_graphViewElement);
        }

        void BindToGraph(GraphViewElement graph) {
            if (ReferenceEquals(_subscribedGraph, graph))
                return;

            if (_subscribedGraph != null)
                _subscribedGraph.OnGraphChanged -= HandleGraphChanged;

            _subscribedGraph = graph;

            if (_subscribedGraph != null)
                _subscribedGraph.OnGraphChanged += HandleGraphChanged;
        }

        void HandleGraphChanged() {
            Repaint();
        }
        
        protected override void OnImGUI() {
            DrawToolbar();
            EditorGUILayout.Space();
            DrawActivePanel();
        }

        void DrawToolbar() {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            bool selectOverview = GUILayout.Toggle(_activePanel == InspectorPanel.Overview,
                                                   "Overview",
                                                   EditorStyles.toolbarButton);
            if (selectOverview && _activePanel != InspectorPanel.Overview)
                SetActivePanel(InspectorPanel.Overview);
            
            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        void SetActivePanel(InspectorPanel panel) {
            _activePanel = panel;
        }

        void DrawActivePanel() {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            switch (_activePanel) {
                case InspectorPanel.Overview:
                    DrawOverviewPanel();
                    break;
            }
            EditorGUILayout.EndVertical();
        }

        void DrawOverviewPanel() {
            if (_graphViewElement == null) {
                EditorGUILayout.HelpBox("Graph view unavailable. Open the dialogue editor first.", MessageType.Warning);
                return;
            }

            BuildAccessibilityOverview();

            EditorGUILayout.LabelField("Accessibility Overview", EditorStyles.boldLabel);

            if (_accessibilityEntries.Count == 0) {
                EditorGUILayout.HelpBox("No accessibility rules assigned to nodes in this graph.", MessageType.Info);
                return;
            }

            int nodesWithRules = _accessibilityEntries.Select(entry => entry.NodeId).Distinct().Count();
            EditorGUILayout.LabelField($"Nodes with accessibility rules: {nodesWithRules}");
            EditorGUILayout.LabelField($"Total accessibility rules: {_accessibilityEntries.Count}");
            EditorGUILayout.Space();

            _overviewScroll = EditorGUILayout.BeginScrollView(_overviewScroll);

            foreach (IGrouping<string, AccessibilityEntry> group in _accessibilityEntries.GroupBy(entry => entry.NodeId)) {
                AccessibilityEntry sample = group.First();
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox)) {
                    EditorGUILayout.LabelField(sample.NodeDisplayName, EditorStyles.boldLabel);
                    if (!string.IsNullOrEmpty(sample.GroupName))
                        EditorGUILayout.LabelField($"Group: {sample.GroupName}");

                    foreach (AccessibilityEntry entry in group) {
                        using (new EditorGUILayout.VerticalScope()) {
                            using (new EditorGUILayout.HorizontalScope()) {
                                EditorGUILayout.LabelField("• " + entry.SettingTitle, EditorStyles.wordWrappedLabel);
                                if (GUILayout.Button("Focus", EditorStyles.miniButton, GUILayout.Width(60f)))
                                    FocusEntry(entry);
                            }
                            if (!string.IsNullOrEmpty(entry.KindLabel))
                                EditorGUILayout.LabelField($"Kind: {entry.KindLabel}", EditorStyles.miniLabel);
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }

        void BuildAccessibilityOverview() {
            _accessibilityEntries.Clear();

            TrySyncWithOwner();

            if (_graphViewElement?.nodes == null)
                return;

            foreach (DialogueNode node in _graphViewElement.nodes.OfType<DialogueNode>()) {
                IReadOnlyList<INodeSetting> settings = node.Model?.NodeSettings;
                if (settings == null || settings.Count == 0)
                    continue;

                foreach (INodeSetting setting in settings) {
                    if (setting == null || setting.Type != NodeSettingType.Accessibility)
                        continue;

                    var entry = new AccessibilityEntry {
                        NodeId = node.ID ?? string.Empty,
                        NodeDisplayName = string.IsNullOrWhiteSpace(node.NodeName) ? "(unnamed node)" : node.NodeName,
                        GroupName = node.GroupElement != null && !string.IsNullOrWhiteSpace(node.GroupElement.title)
                            ? node.GroupElement.title
                            : string.Empty,
                        SettingTitle = string.IsNullOrWhiteSpace(setting.Title) ? "(untitled rule)" : setting.Title,
                        KindLabel = setting is AccessibilitySetting accessibility ? accessibility.kind.ToString() : string.Empty
                    };

                    _accessibilityEntries.Add(entry);
                }
            }

            _accessibilityEntries.Sort((a, b) => string.Compare(a.NodeDisplayName, b.NodeDisplayName, StringComparison.OrdinalIgnoreCase));
        }
        
        void FocusEntry(AccessibilityEntry entry) {
            if (_graphViewElement == null || entry == null || string.IsNullOrEmpty(entry.NodeId))
                return;

            bool focused = _graphViewElement.FocusNodeById(entry.NodeId);

            if (focused)
                _ownerWindow?.Focus();
        }

        sealed class AccessibilityEntry {
            public string NodeId;
            public string NodeDisplayName;
            public string GroupName;
            public string SettingTitle;
            public string KindLabel;
        }

        [Serializable]
        class PlayerStatsState {
            public List<AttributeEntry> attributes = new();
            public List<SkillEntry> skills = new();
            public bool statsFoldoutExpanded = true;
        }

        [Serializable]
        class AttributeEntry {
            public AttributeType type;
            public int value;
        }

        [Serializable]
        class SkillEntry {
            public SkillType type;
            public int value;
        }
        
        void SavePlayerStatsState() {
            var state = new PlayerStatsState {
                statsFoldoutExpanded = _statsFoldoutExpanded
            };

            foreach (AttributeType attribute in Enum.GetValues(typeof(AttributeType))) {
                int value = _playerAttributes.GetValueOrDefault(attribute, 0);
                state.attributes.Add(new AttributeEntry { type = attribute, value = value });
            }

            foreach (SkillType skill in Enum.GetValues(typeof(SkillType))) {
                int value = _playerSkills.GetValueOrDefault(skill, 0);
                state.skills.Add(new SkillEntry { type = skill, value = value });
            }

            string json = JsonUtility.ToJson(state);
            EditorPrefs.SetString(PlayerStatsPrefsKey, json);
        }

        void LoadPlayerStatsState() {
            _playerAttributes.Clear();
            _playerSkills.Clear();

            string json = EditorPrefs.GetString(PlayerStatsPrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) {
                _statsFoldoutExpanded = true;
                return;
            }

            try {
                PlayerStatsState state = JsonUtility.FromJson<PlayerStatsState>(json);

                if (state != null) {
                    _statsFoldoutExpanded = state.statsFoldoutExpanded;

                    if (state.attributes != null) {
                        foreach (AttributeEntry entry in state.attributes)
                            _playerAttributes[entry.type] = entry.value;
                    }

                    if (state.skills != null) {
                        foreach (SkillEntry entry in state.skills)
                            _playerSkills[entry.type] = entry.value;
                    }
                }
            } catch (Exception) {
                _statsFoldoutExpanded = true;
            }
        }
    }
}
#endif

