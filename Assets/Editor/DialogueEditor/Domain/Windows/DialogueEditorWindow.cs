#if UNITY_EDITOR
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DialogueEditor.Data.Error;
using DialogueEditor.Elements;
using DialogueEditor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Windows {
    public class DialogueEditorWindow : EditorWindow {
        const string ToolName = "Tools/Editors/Dialogue Editor/Editor";
        const string DefaultFileName = "DialoguesFileName";
        const int MinimumWindowWidth = 900;
        const int MinimumWindowHeight = 400;

        static TextField _fileNameTextField;
        ScrollView _autoFixListView;

        Button _errorIndicatorButton;
        IVisualElementScheduledItem _errorIndicatorSchedule;
        VisualElement _errorInfoContainer;
        ScrollView _errorListView;
        VisualElement _graphDimOverlay;
        GraphViewElement _graphViewElement;
        List<string> _lastIssueSnapshot = new();

        void OnEnable() {
            minSize = new Vector2(MinimumWindowWidth, MinimumWindowHeight);
            AddGraphView();
            AddToolbar();
            AddStyles();
            CreateErrorInfoWindows();
            StartErrorIndicatorUpdates();
        }

        void OnDisable() {
            _errorIndicatorSchedule?.Pause();
            _errorIndicatorSchedule = null;
            SetGraphDimmed(false);
        }

        [MenuItem(ToolName)]
        public static void Open() {
            GetWindow<DialogueEditorWindow>("💬 Dialogue Editor");
        }

        void AddGraphView() {
            _graphViewElement = new GraphViewElement(this);
            _graphViewElement.StretchToParentSize();
            rootVisualElement.Add(_graphViewElement);

            _graphDimOverlay = new VisualElement {
                name = "ds-graph-dim-overlay",
                style = {
                    position = Position.Absolute,
                    left = 0,
                    top = 0,
                    right = 0,
                    bottom = 0,
                    backgroundColor = new Color(0f, 0f, 0f, 0.35f),
                    display = DisplayStyle.None
                },
                pickingMode = PickingMode.Ignore
            };
            _graphViewElement.Add(_graphDimOverlay);
            _graphDimOverlay.BringToFront();
        }

        void AddToolbar() {
            var toolbar = new DialogueEditorToolbar(DefaultFileName,
                                                    _graphViewElement,
                                                    Save,
                                                    SaveGraphAs,
                                                    Load,
                                                    OnInspection,
                                                    OnUndoRedoHistory,
                                                    Clear,
                                                    ResetGraph,
                                                    ToggleErrorInfo);

            _fileNameTextField = toolbar.FileNameTextField;
            _errorIndicatorButton = toolbar.ErrorIndicatorButton;
            _errorIndicatorButton.tooltip = "Graph validation summary";

            rootVisualElement.Add(toolbar);
        }

        public void SetGraphDimmed(bool enabled) {
            if (_graphDimOverlay == null)
                return;

            _graphDimOverlay.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void AddStyles() {
            rootVisualElement.AddStyleSheets("DSVariables");
            rootVisualElement.AddStyleSheets("DSToolbarStyles");
        }

        void Save(bool graphOnly) {
            if (string.IsNullOrEmpty(_fileNameTextField.value)) {
                EditorUtility.DisplayDialog("Invalid file name.",
                                            "Please ensure the file name you've typed in is valid.", "Roger!");

                return;
            }

            GraphValidationSummary validationSummary = _graphViewElement.GetValidationSummary();

            if (validationSummary.HasErrors) {
                string[] messages = validationSummary.BuildMessages().ToArray();
                string issues = string.Join("\n- ", messages);
                string body = messages.Length > 0
                    ? "Please resolve the following issues before saving:\n\n- " + issues
                    : "Please resolve the outstanding validation issues before saving.";
                EditorUtility.DisplayDialog("Graph validation issues", body, "Understood");
                return;
            }

            IOUtility.Initialize(_graphViewElement, _fileNameTextField.value);
            IOUtility.Save(graphOnly);
        }

        void SaveGraphAs() {
            if (string.IsNullOrEmpty(_fileNameTextField.value)) {
                EditorUtility.DisplayDialog("Invalid file name.",
                                            "Please ensure the file name you've typed in is valid.", "Roger!");

                return;
            }

            GraphValidationSummary validationSummary = _graphViewElement.GetValidationSummary();

            if (validationSummary.HasErrors) {
                string[] messages = validationSummary.BuildMessages().ToArray();
                string issues = string.Join("\n- ", messages);
                string body = messages.Length > 0
                    ? "Please resolve the following issues before saving:\n\n- " + issues
                    : "Please resolve the outstanding validation issues before saving.";
                EditorUtility.DisplayDialog("Graph validation issues", body, "Understood");
                return;
            }

            IOUtility.Initialize(_graphViewElement, _fileNameTextField.value);
            IOUtility.SaveGraphAs();
        }

        void Load() {
            string filePath =
                EditorUtility.OpenFilePanel("Dialogue Graphs", "Assets/Editor/DialogueEditor/Graphs", "asset");

            if (string.IsNullOrEmpty(filePath)) {
                return;
            }

            Clear();

            IOUtility.Initialize(_graphViewElement, Path.GetFileNameWithoutExtension(filePath));
            IOUtility.Load();
        }
        
        void OnInspection() {
            GraphInspectorWindow.Open(this, _graphViewElement);
        }
        
        void OnUndoRedoHistory() {
            UndoRedoHistoryWindow.Open(this, _graphViewElement);
        }

        void Clear() {
            _graphViewElement.ClearGraph();
        }

        void ResetGraph() {
            Clear();

            UpdateFileName(DefaultFileName);
        }

        void ToggleErrorInfo() {
            if (_errorInfoContainer == null)
                return;

            bool currentlyVisible = _errorInfoContainer.style.display != DisplayStyle.None;
            _errorInfoContainer.style.display = currentlyVisible ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public static void UpdateFileName(string newFileName) {
            _fileNameTextField.value = newFileName;
        }

        public GraphViewElement GetGraphViewElement() {
            return _graphViewElement;
        }

        void CreateErrorInfoWindows() {
            _errorInfoContainer = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Row,
                    marginTop = 6,
                    display = DisplayStyle.None,
                    minHeight = 350,
                    maxHeight = 800
                }
            };
            _errorInfoContainer.AddToClassList("ds-error-info-container");

            VisualElement autoFixWindow = CreateInfoWindow("Quick actions & auto-fix", out _autoFixListView);
            VisualElement issuesWindow = CreateInfoWindow("Current problems", out _errorListView);

            _errorInfoContainer.Add(autoFixWindow);
            _errorInfoContainer.Add(issuesWindow);

            rootVisualElement.Add(_errorInfoContainer);
        }

        static VisualElement CreateInfoWindow(string title, out ScrollView contentContainer) {
            var window = new VisualElement {
                style = {
                    width = 220,
                    backgroundColor = new Color(179, 157, 219, .35f),
                    borderLeftWidth = 1,
                    borderTopWidth = 1,
                    borderRightWidth = 1,
                    borderBottomWidth = 1,
                    borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                    borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                    borderRightColor = new Color(0.25f, 0.25f, 0.25f),
                    borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                    paddingLeft = 8,
                    paddingRight = 8,
                    paddingTop = 8,
                    paddingBottom = 8,
                    flexDirection = FlexDirection.Column
                }
            };
            window.AddToClassList("ds-error-info-window");

            var titleLabel = new Label(title) { style = { unityFontStyleAndWeight = FontStyle.Bold, whiteSpace = WhiteSpace.Normal } };
            titleLabel.AddToClassList("ds-error-info-window__title");

            contentContainer = new ScrollView { style = { flexGrow = 1, paddingTop = 2 } };
            contentContainer.AddToClassList("ds-error-info-window__content");

            window.Add(titleLabel);
            window.Add(contentContainer);
            return window;
        }

        void UpdateIssueWindows(IReadOnlyList<GraphValidationIssue> issues) {
            if (_errorListView == null || _autoFixListView == null)
                return;

            _errorListView.Clear();
            _autoFixListView.Clear();

            if (issues == null || issues.Count == 0) {
                _errorListView.Add(new Label("No validation issues detected.") { style = { whiteSpace = WhiteSpace.Normal } });
                _autoFixListView.Add(new Label("Nothing to fix right now.") { style = { whiteSpace = WhiteSpace.Normal } });
                return;
            }

            foreach (GraphValidationIssue issue in issues) {
                var issueLabel = new Label($"• {issue.Message}") { style = { whiteSpace = WhiteSpace.Normal, marginBottom = 4 } };
                _errorListView.Add(issueLabel);
            }

            var anyActions = false;

            foreach (GraphValidationIssue issue in issues) {
                if (issue.Actions == null || issue.Actions.Count == 0)
                    continue;

                anyActions = true;
                var entry = new VisualElement { style = { flexDirection = FlexDirection.Column, marginBottom = 6 } };

                var description = new Label(issue.Message) { style = { whiteSpace = WhiteSpace.Normal } };

                entry.Add(description);

                VisualElement buttonContainer = new() { style = { flexDirection = FlexDirection.Column } };

                foreach (GraphValidationIssueAction action in issue.Actions) {
                    if (!action.CanExecute)
                        continue;

                    GraphValidationIssueAction capturedAction = action;
                    Button actionButton = ElementUtility.CreateButton(capturedAction.Label, () => {
                        capturedAction.Invoke();
                        UpdateErrorIndicator();
                    });
                    actionButton.AddToClassList("ds-error-info__fix-button");
                    actionButton.style.alignSelf = Align.FlexStart;

                    buttonContainer.Add(actionButton);
                }

                entry.Add(buttonContainer);
                _autoFixListView.Add(entry);
            }

            if (!anyActions)
                _autoFixListView.Add(new Label("No quick actions available.") { style = { whiteSpace = WhiteSpace.Normal } });
        }

        void StartErrorIndicatorUpdates() {
            UpdateErrorIndicator(true);
            _errorIndicatorSchedule = rootVisualElement.schedule.Execute(_ => UpdateErrorIndicator()).Every(2000);
        }

        void UpdateErrorIndicator(bool forceRefresh = false) {
            if (_errorIndicatorButton == null || _graphViewElement == null)
                return;

            GraphValidationSummary summary = _graphViewElement.GetValidationSummary();
            IReadOnlyList<GraphValidationIssue> issues = summary.Issues;
            int errorCount = issues.Count;

            List<string> snapshot = BuildIssueSnapshot(issues);
            bool hasChanges = forceRefresh || !_lastIssueSnapshot.SequenceEqual(snapshot);

            if (!hasChanges)
                return;

            string label = errorCount == 1 ? "Errors: 1" : $"Errors: {errorCount}";
            Color colorProblem = new Color32(187, 71, 54, 255);
            Color colorNoProblem = new Color32(39, 41, 44, 255);
            _errorIndicatorButton.text = label;
            _errorIndicatorButton.tooltip = errorCount > 0
                ? "Current graph issues:\n- " + string.Join("\n- ", issues.Select(issue => issue.Message))
                : "No validation issues detected.";
            _errorIndicatorButton.style.color = errorCount > 0 ? Color.papayaWhip : colorProblem;
            _errorIndicatorButton.style.backgroundColor = errorCount > 0 ? colorProblem : colorNoProblem;
            _errorIndicatorButton.EnableInClassList("ds-toolbar__button__error-present", errorCount > 0);
            UpdateIssueWindows(issues);
            _lastIssueSnapshot = snapshot;
        }

        static List<string> BuildIssueSnapshot(IReadOnlyList<GraphValidationIssue> issues) {
            var snapshot = new List<string>();

            if (issues == null || issues.Count == 0)
                return snapshot;

            foreach (GraphValidationIssue issue in issues) {
                string message = issue?.Message ?? string.Empty;
                string actions = issue?.Actions is { Count: > 0 }
                    ? string.Join("|", issue.Actions.Select(action => action?.Label ?? string.Empty))
                    : string.Empty;

                snapshot.Add($"{message}::{actions}");
            }

            return snapshot;
        }
        
        [MenuItem("Tools/Editors/Dialogue Editor/Manual")]
        public static void OpenManual() {
            var path = Path.Combine(
                                    Application.dataPath,
                                    "Editor/DialogueEditor/Documentation/DialogueEditorManual.md"
                                   );
            
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
#endif