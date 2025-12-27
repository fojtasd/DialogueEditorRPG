#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Common;
using Contracts.Dialogues;
using DialogueEditor.Data.Error;
using DialogueEditor.Data.Save;
using DialogueEditor.Utilities;
using DialogueEditor.Windows;
using DialogueSystem.Utilities;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using GraphGroup = UnityEditor.Experimental.GraphView.Group;

namespace DialogueEditor.Elements {
    public class GraphViewElement : GraphView {
        readonly SerializableDictionary<GraphGroup, SerializableDictionary<string, NodeErrorData>> _groupedNodes;

        readonly SerializableDictionary<string, GroupErrorData> _groups;

        readonly SerializableDictionary<string, NodeErrorData> _ungroupedNodes;

        /// <summary>
        ///     Node Names already used - for checking new names
        /// </summary>
        readonly HashSet<string> _usedNodeNames = new();

        List<BaseNode> _lastInvalidStartingNodes = new();


        public BaseNode HighlightedNode;

        public GraphViewElement(DialogueEditorWindow dialogueEditorWindow) {
            EditorWindow = dialogueEditorWindow;

            _ungroupedNodes = new SerializableDictionary<string, NodeErrorData>();
            _groups = new SerializableDictionary<string, GroupErrorData>();
            _groupedNodes =
                new SerializableDictionary<GraphGroup,
                    SerializableDictionary<string, NodeErrorData>>();

            AddManipulators();

            AddGridBackground();

            OnElementsDeleted();
            OnGroupElementsAdded();
            OnGroupElementsRemoved();
            OnGroupRenamed();

            AddStyles();
            schedule.Execute(ValidateGraph).Every(2000);
        }

        public DialogueEditorWindow EditorWindow { get; }

        public int NameErrorsAmount { get; set; }

        public event Action<BaseNode> OnNodeDeletion;

        void RebuildUsedNodeNames() {
            _usedNodeNames.Clear();

            foreach (Node node1 in nodes) {
                var node = (BaseNode)node1;
                if (!node.ShouldTrackName)
                    continue;
                if (!string.IsNullOrWhiteSpace(node.NodeName))
                    _usedNodeNames.Add(node.NodeName);
            }
        }

        void ValidateGraph() {
            if (HasMultipleStartingNodes(out List<BaseNode> startNodes)) {
                foreach (BaseNode node in startNodes) node.SetErrorStyle(Color.red);

                // Clear old nodes that were previously invalid but now fixed
                foreach (BaseNode oldNode in _lastInvalidStartingNodes.Except(startNodes)) oldNode.ResetStyleAfterError();

                _lastInvalidStartingNodes = startNodes;
            } else {
                // Reset any previously flagged nodes
                foreach (BaseNode node in _lastInvalidStartingNodes) node.ResetStyleAfterError();

                _lastInvalidStartingNodes.Clear();
            }
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
            var compatiblePorts = new List<Port>();

            ports.ForEach(port => {
                if (startPort == port) return;

                if (startPort.node == port.node) return;

                if (startPort.direction == port.direction) return;

                if (startPort.node is RelayNode && port.node is RelayNode) return;

                compatiblePorts.Add(port);
            });

            return compatiblePorts;
        }

        void AddManipulators() {
            SetupZoom(0.1f, ContentZoomer.DefaultMaxScale+0.5f);

            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());

            nodeCreationRequest = context => {
                Vector2 localMousePosition = GetLocalMousePosition(context.screenMousePosition, true);
                DialogueNode multipleChoiceDialogueNode = CreateDialogueNode(localMousePosition);
                AddElement(multipleChoiceDialogueNode);
            };
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            Vector2 mousePosition = GetLocalMousePosition(evt.mousePosition);
            evt.menu.AppendAction("💬 Add Dialogue Node", _ => {
                DialogueNode newNode = CreateDialogueNode(mousePosition);
                AddElement(newNode);
                ClearSelection();
                AddToSelection(newNode);
                FrameSelection();
            });
            evt.menu.AppendAction("❓ Add Conditional Node", _ => {
                ConditionalNode newNode = CreateConditionalNode(mousePosition);
                AddElement(newNode);
                ClearSelection();
                AddToSelection(newNode);
                FrameSelection();
            });
            evt.menu.AppendAction("🔀 Add Relay Node", _ => {
                RelayNode newNode = CreateRelayNode(mousePosition);
                AddElement(newNode);
                ClearSelection();
                AddToSelection(newNode);
                FrameSelection();
            });
            evt.menu.AppendAction("✍️ Add Sticky Note", _ => AddSticky(mousePosition));
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("🗂️ Add Group", _ => CreateGroup("DialogueGroup", mousePosition));
        }

        public GroupElement CreateGroup(string title, Vector2 position) {
            var group = new GroupElement(title, position);

            AddGroup(group);

            AddElement(group);

            foreach (GraphElement selectedElement in selection.Cast<GraphElement>()) {
                if (selectedElement is not BaseNode node) continue;

                group.AddElement(node);
            }

            return group;
        }

        public DialogueNode CreateDialogueNode(Vector2 position, string nodeName = null, bool shouldDraw = true) {
            var node = new DialogueNode();

            if (nodeName == null || _usedNodeNames.Contains(nodeName)) {
                string id = NodeUtility.GetNextNodeName(_usedNodeNames);
                node.Initialize(id, this, position);
                _usedNodeNames.Add(id);
            } else {
                node.Initialize(nodeName, this, position);
                _usedNodeNames.Add(nodeName);
            }

            if (shouldDraw) node.Draw();

            AddUngroupedNode(node);

            return node;
        }

        public ConditionalNode CreateConditionalNode(Vector2 position, string nodeName = null, bool shouldDraw = true) {
            var node = new ConditionalNode();

            if (nodeName == null || _usedNodeNames.Contains(nodeName)) {
                string id = NodeUtility.GetNextNodeName(_usedNodeNames);
                node.Initialize(id, this, position);
                _usedNodeNames.Add(id);
            } else {
                node.Initialize(nodeName, this, position);
                _usedNodeNames.Add(nodeName);
            }

            if (shouldDraw) node.Draw();

            AddUngroupedNode(node);

            return node;
        }

        public RelayNode CreateRelayNode(Vector2 position, bool shouldDraw = true) {
            var node = new RelayNode();

            node.Initialize("Relay", this, position);

            if (shouldDraw) node.Draw();

            AddUngroupedNode(node);

            return node;
        }

        void OnElementsDeleted() {
            deleteSelection = (_, _) => {
                Type groupType = typeof(GroupElement);
                Type edgeType = typeof(EdgeElement);

                List<GroupElement> groupsToDelete = new();
                List<BaseNode> nodesToDelete = new();
                List<EdgeElement> edgesToDelete = new();

                foreach (ISelectable selectable in selection) {
                    var selectedElement = (GraphElement)selectable;
                    if (selectedElement is BaseNode node) {
                        nodesToDelete.Add(node);
                        continue;
                    }

                    if (selectedElement.GetType() == edgeType) {
                        var edge = (EdgeElement)selectedElement;
                        edgesToDelete.Add(edge);
                        continue;
                    }

                    if (selectedElement.GetType() != groupType) continue;
                    var group = (GroupElement)selectedElement;
                    groupsToDelete.Add(group);
                }

                foreach (GroupElement groupToDelete in groupsToDelete) {
                    var groupNodes = new List<DialogueNode>();

                    foreach (GraphElement groupElement in groupToDelete.containedElements) {
                        if (groupElement is not DialogueNode groupNode) continue;

                        groupNodes.Add(groupNode);
                    }

                    groupToDelete.RemoveElements(groupNodes);

                    RemoveGroup(groupToDelete);

                    RemoveElement(groupToDelete);
                }

                DeleteElements(edgesToDelete);

                foreach (BaseNode nodeToDelete in nodesToDelete) DeleteNode(nodeToDelete);
            };
        }

        void DeleteNode(BaseNode nodeToDelete) {
            if (nodeToDelete == null)
                return;

            _usedNodeNames.Remove(nodeToDelete.NodeName);
            nodeToDelete.GroupElement?.RemoveElement(nodeToDelete);

            RemoveUngroupedNode(nodeToDelete);

            DisconnectAllPorts(nodeToDelete);
            OnNodeDeletion?.Invoke(nodeToDelete);

            if (HighlightedNode == nodeToDelete) {
                HighlightedNode.RemoveHighlight();
                HighlightedNode = null;
            }

            RemoveElement(nodeToDelete);
        }

        void OnGroupElementsAdded() {
            elementsAddedToGroup = (group, elements) => {
                foreach (GraphElement element in elements) {
                    if (element is not BaseNode node) continue;

                    var dsGroup = (GroupElement)group;

                    RemoveUngroupedNode(node);
                    AddGroupedNode(node, dsGroup);
                }
            };
        }

        void OnGroupElementsRemoved() {
            elementsRemovedFromGroup = (group, elements) => {
                foreach (GraphElement element in elements) {
                    if (element is not BaseNode node) continue;

                    var dsGroup = (GroupElement)group;

                    RemoveGroupedNode(node, dsGroup);
                    AddUngroupedNode(node);
                }
            };
        }

        void OnGroupRenamed() {
            groupTitleChanged = (group, newTitle) => {
                var dsGroup = (GroupElement)group;

                dsGroup.title = newTitle.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(dsGroup.title)) {
                    if (!string.IsNullOrEmpty(dsGroup.OldTitle)) ++NameErrorsAmount;
                } else {
                    if (string.IsNullOrEmpty(dsGroup.OldTitle)) --NameErrorsAmount;
                }

                RemoveGroup(dsGroup);
                dsGroup.OldTitle = dsGroup.title;
                AddGroup(dsGroup);
            };
        }

        public void DisconnectPortConnections(Port port) {
            foreach (Edge connection in port.connections.ToList()) {
                if (connection is EdgeElement { output: { userData: DialogueNodeSaveData choiceData, node: DialogueNode } }) {
                    choiceData.NodeID = null;
                }

                if (connection is EdgeElement { output: { userData: ConditionalNodeSaveData conditionSaveData, node: ConditionalNode } }) {
                    conditionSaveData.NodeID = null;
                }

                // Physically disconnect and remove from UI
                port.Disconnect(connection);
                connection.output?.Disconnect(connection);
                connection.input?.Disconnect(connection);
                RemoveElement(connection);
            }
        }

        public void AddUngroupedNode(BaseNode node) {
            if (!node.ShouldTrackName)
                return;
            string nodeName = node.NodeName.ToLower();

            if (!_ungroupedNodes.TryGetValue(nodeName, out NodeErrorData ungroupedNode)) {
                var nodeErrorData = new NodeErrorData();
                nodeErrorData.Nodes.Add(node);
                _ungroupedNodes.Add(nodeName, nodeErrorData);
                return;
            }

            List<BaseNode> ungroupedNodesList = ungroupedNode.Nodes;
            ungroupedNodesList.Add(node);
            Color errorColor = _ungroupedNodes[nodeName].ErrorData.Color;
            node.SetErrorStyle(errorColor);

            if (ungroupedNodesList.Count != 2)
                return;
            ++NameErrorsAmount;

            ungroupedNodesList[0].SetErrorStyle(errorColor);
        }

        public void RemoveUngroupedNode(BaseNode node) {
            if (!node.ShouldTrackName)
                return;
            string nodeName = node.NodeName.ToLower();
            if (!_ungroupedNodes.TryGetValue(nodeName, out NodeErrorData _))
                return;

            List<BaseNode> ungroupedNodesList = _ungroupedNodes[nodeName].Nodes;
            ungroupedNodesList.Remove(node);
            node.ResetStyleAfterError();

            if (ungroupedNodesList.Count == 1) {
                --NameErrorsAmount;
                ungroupedNodesList[0].ResetStyleAfterError();
                return;
            }

            if (ungroupedNodesList.Count == 0) _ungroupedNodes.Remove(nodeName);
        }

        void AddGroup(GroupElement groupElement) {
            string groupName = groupElement.title.ToLower();

            if (!_groups.TryGetValue(groupName, out GroupErrorData selectedGroup)) {
                var groupErrorData = new GroupErrorData();
                groupErrorData.Groups.Add(groupElement);
                _groups.Add(groupName, groupErrorData);

                return;
            }

            List<GroupElement> groupsList = selectedGroup.Groups;
            groupsList.Add(groupElement);
            groupElement.SetErrorStyle();

            if (groupsList.Count == 2) {
                ++NameErrorsAmount;

                groupsList[0].SetErrorStyle();
            }
        }

        void RemoveGroup(GroupElement groupElement) {
            string oldGroupName = groupElement.OldTitle.ToLower();

            List<GroupElement> groupsList = _groups[oldGroupName].Groups;
            groupsList.Remove(groupElement);
            groupElement.ResetStyle();

            if (groupsList.Count == 1) {
                --NameErrorsAmount;

                groupsList[0].ResetStyle();

                return;
            }

            if (groupsList.Count == 0) _groups.Remove(oldGroupName);
        }

        public void AddGroupedNode(BaseNode node, GroupElement groupElement) {
            if (!node.ShouldTrackName)
                return;
            string nodeName = node.NodeName.ToLower();

            node.GroupElement = groupElement;

            if (!_groupedNodes.ContainsKey(groupElement))
                _groupedNodes.Add(groupElement, new SerializableDictionary<string, NodeErrorData>());

            if (!_groupedNodes[groupElement].ContainsKey(nodeName)) {
                var nodeErrorData = new NodeErrorData();

                nodeErrorData.Nodes.Add(node);

                _groupedNodes[groupElement].Add(nodeName, nodeErrorData);

                return;
            }

            List<BaseNode> groupedNodesList = _groupedNodes[groupElement][nodeName].Nodes;

            groupedNodesList.Add(node);

            Color errorColor = _groupedNodes[groupElement][nodeName].ErrorData.Color;

            node.SetErrorStyle(errorColor);

            if (groupedNodesList.Count == 2) {
                ++NameErrorsAmount;

                groupedNodesList[0].SetErrorStyle(errorColor);
            }
        }

        public void RemoveGroupedNode(BaseNode node, GroupElement groupElement) {
            if (!node.ShouldTrackName)
                return;
            string nodeName = node.NodeName.ToLower();

            node.GroupElement = null;

            List<BaseNode> groupedNodesList = _groupedNodes[groupElement][nodeName].Nodes;

            groupedNodesList.Remove(node);

            node.ResetStyleAfterError();

            if (groupedNodesList.Count == 1) {
                --NameErrorsAmount;

                groupedNodesList[0].ResetStyleAfterError();

                return;
            }

            if (groupedNodesList.Count == 0) {
                _groupedNodes[groupElement].Remove(nodeName);

                if (_groupedNodes[groupElement].Count == 0) _groupedNodes.Remove(groupElement);
            }
        }

        void AddGridBackground() {
            var gridBackground = new GridBackground();

            gridBackground.StretchToParentSize();

            Insert(0, gridBackground);
        }

        void AddStyles() {
            this.AddStyleSheets(
                                "DSGraphViewStyles",
                                "DSNodeStyles"
                               );
        }

        Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false) {
            Vector2 worldMousePosition = mousePosition;

            if (isSearchWindow)
                worldMousePosition = EditorWindow.rootVisualElement.ChangeCoordinatesTo(
                                                                                        EditorWindow.rootVisualElement.parent,
                                                                                        mousePosition - EditorWindow.position.position);

            Vector2 localMousePosition = contentViewContainer.WorldToLocal(worldMousePosition);

            return localMousePosition;
        }

        public void ClearGraph() {
            graphElements.ForEach(RemoveElement);

            _groups.Clear();
            _groupedNodes.Clear();
            _ungroupedNodes.Clear();
            RebuildUsedNodeNames();

            NameErrorsAmount = 0;
        }
        
        public void SearchNodes(string searchTerm) {
            if (string.IsNullOrWhiteSpace(searchTerm)) {
                foreach (Node node1 in nodes) {
                    var node = (BaseNode)node1;
                    node.RemoveHighlight();
                }

                return;
            }

            string term = searchTerm.ToLowerInvariant();
            string pattern = term.Trim('*');

            foreach (Node node1 in nodes) {
                if (node1 is not DialogueNode node)
                    continue;
                string title = node.NodeName.ToLowerInvariant();
                string text = node.Model.Text.ToLowerInvariant();

                bool titleMatch = MatchesPattern(title, pattern);
                bool textMatch = MatchesPattern(text, pattern);

                if (titleMatch || textMatch)
                    node.StartHighlighting();
                else
                    node.RemoveHighlight();
            }
        }

        public DialogueNode SearchNode(string searchTerm) {
            string term = searchTerm.ToLowerInvariant();
            string pattern = term.Trim('*');

            foreach (Node node1 in nodes) {
                var node = (DialogueNode)node1;
                string title = node.NodeName.ToLowerInvariant();
                string text = node.Model.Text.ToLowerInvariant();

                bool titleMatch = MatchesPattern(title, pattern);
                bool textMatch = MatchesPattern(text, pattern);

                if (titleMatch || textMatch) return node;
            }

            return null;
        }

        bool MatchesPattern(string content, string pattern) {
            if (string.IsNullOrEmpty(pattern)) return false;

            return content.Contains(pattern);
        }

        bool HasMultipleStartingNodes(out List<BaseNode> startingNodes) {
            startingNodes = nodes
                           .OfType<BaseNode>()
                           .Where(node => !node.ShouldIgnoreStartingNodeValidation && IsStartingNode(node))
                           .ToList();

            return startingNodes.Count > 1;
        }

        public static bool IsStartingNode(BaseNode node) {
            List<Port> inputPorts = node.inputContainer.Children().OfType<Port>().ToList();
            return inputPorts.Count == 0 || inputPorts.All(p => !p.connected);
        }

        void DisconnectPorts(VisualElement container) {
            foreach (VisualElement child in container.Children())
                if (child is Port { connected: true } port)
                    DisconnectPortConnections(port);
        }

        public void DisconnectAllPorts(BaseNode dialogueNode) {
            if (dialogueNode == null) return;
            DisconnectInputPorts(dialogueNode);
            DisconnectOutputPorts(dialogueNode);
        }

        public void DisconnectInputPorts(BaseNode dialogueNode) {
            DisconnectPorts(dialogueNode.inputContainer);
        }

        public void DisconnectOutputPorts(BaseNode dialogueNode) {
            DisconnectPorts(dialogueNode.outputContainer);
        }

        public void AddSticky(Vector2 position,
                              Vector2? size = null,
                              string text = "",
                              string colorClass = null,
                              bool isLocked = false,
                              bool isBold = false,
                              float? fontSize = null) {
            var sticky = new StickyNoteElement();
            Vector2 resolvedSize = size ?? new Vector2(220, 140);
            sticky.SetPosition(new Rect(position, resolvedSize));
            sticky.SetText(text ?? string.Empty);

            if (!string.IsNullOrEmpty(colorClass))
                sticky.SetColorByClassName(colorClass);

            sticky.SetBoldState(isBold);
            sticky.SetLockState(isLocked);
            if (fontSize is > 0f)
                sticky.SetFontSize(fontSize.Value);

            AddElement(sticky);
        }

        public GraphValidationSummary GetValidationSummary() {
            var summary = new GraphValidationSummary();

            if (HasMultipleStartingNodes(out List<BaseNode> startingNodes) && startingNodes.Count > 1) {
                List<BaseNode> orderedStartingNodes = startingNodes
                                                     .OrderBy(GetDisplayableNodeName, StringComparer.OrdinalIgnoreCase)
                                                     .ThenBy(node => node.ID)
                                                     .ToList();

                string startList = string.Join(", ", orderedStartingNodes.Select(DescribeNode));
                GraphValidationIssue issue = summary.AddIssue($"More than one starting node detected: {startList}");

                if (issue != null) {
                    foreach (BaseNode startingNode in orderedStartingNodes) {
                        BaseNode targetNode = startingNode;
                        string nodeName = GetDisplayableNodeName(targetNode);
                        issue.AddAction($"Focus '{nodeName}'", () => CenterOnNode(targetNode));
                    }
                }
            }

            foreach ((string _, NodeErrorData data) in _ungroupedNodes) {
                if (data.Nodes.Count <= 1)
                    continue;

                BaseNode firstNode = data.Nodes.FirstOrDefault();
                string nodeName = GetDisplayableNodeName(firstNode);
                string nodeList = string.Join(", ", data.Nodes.Select(DescribeNode));
                var message = $"Duplicate node name '{nodeName}' among ungrouped nodes ({data.Nodes.Count}): {nodeList}";
                GraphValidationIssue issue = summary.AddIssue(message);
                if (issue == null)
                    continue;
                foreach (BaseNode duplicate in data.Nodes)
                    issue.AddAction($"Focus {DescribeNodeShort(duplicate)}", () => CenterOnNode(duplicate));
            }

            foreach ((GraphGroup graphGroup, SerializableDictionary<string, NodeErrorData> groupedEntries) in _groupedNodes) {
                if (graphGroup is not GroupElement groupElement)
                    continue;

                foreach ((string _, NodeErrorData data) in groupedEntries) {
                    if (data.Nodes.Count <= 1)
                        continue;

                    BaseNode first = data.Nodes.FirstOrDefault();
                    string nodeName = GetDisplayableNodeName(first);
                    string groupName = DescribeGroup(groupElement);
                    string nodeList = string.Join(", ", data.Nodes.Select(DescribeNode));
                    var message = $"Group '{groupName}' has duplicate node name '{nodeName}' ({data.Nodes.Count}): {nodeList}";
                    GraphValidationIssue issue = summary.AddIssue(message);
                    if (issue != null) {
                        foreach (BaseNode duplicate in data.Nodes)
                            issue.AddAction($"Focus {DescribeNodeShort(duplicate)}", () => CenterOnNode(duplicate));
                    }
                }
            }

            foreach ((string _, GroupErrorData data) in _groups) {
                if (data.Groups.Count <= 1)
                    continue;

                string groupsList = string.Join(", ", data.Groups.Select(DescribeGroup));
                string groupName = GetDisplayableGroupName(data.Groups.FirstOrDefault());
                var message = $"Duplicate group name '{groupName}' ({data.Groups.Count}): {groupsList}";
                GraphValidationIssue issue = summary.AddIssue(message);
                if (issue != null) {
                    foreach (GroupElement group in data.Groups)
                        issue.AddAction($"Focus {DescribeGroupShort(group)}", () => CenterOnGroup(group));
                }
            }

            foreach (DialogueNode dialogueNode in nodes.OfType<DialogueNode>()) {
                int choiceCount = dialogueNode?.Choices?.Count ?? 0;
                if (choiceCount <= 1)
                    continue;
                
                List<DialogueNodeSaveData> unconnectedChoices = dialogueNode.Choices
                                                                            .Where(choice => string.IsNullOrWhiteSpace(choice?.NodeID))
                                                                            .ToList();
                if (unconnectedChoices.Count <= 0)
                    continue;

                string choiceSummary = string.Join(", ",
                    unconnectedChoices.Select(choice => $"'{GetChoiceDisplayLabel(choice)}'"));

                DialogueNodeSaveData choiceToRemove = unconnectedChoices.FirstOrDefault();
                DialogueNode targetWithEmptyChoices = dialogueNode;
                GraphValidationIssue unconnectedIssue =
                    summary.AddIssue($"Node {DescribeNode(dialogueNode)} defines multiple choices but {unconnectedChoices.Count} choice(s) are not connected: {choiceSummary}.");
                unconnectedIssue?.AddAction($"Focus {DescribeNodeShort(targetWithEmptyChoices)}", () => CenterOnNode(targetWithEmptyChoices));
                if (choiceToRemove != null)
                    unconnectedIssue?.AddAction("Remove one unconnected choice", () => RemoveChoice(targetWithEmptyChoices, choiceToRemove));
            }

            foreach (ConditionalNode conditionalNode in nodes.OfType<ConditionalNode>()) {
                bool successMissing = string.IsNullOrWhiteSpace(conditionalNode.SuccessNodeData?.NodeID);
                bool failureMissing = string.IsNullOrWhiteSpace(conditionalNode.FailureNodeData?.NodeID);

                if (!successMissing && !failureMissing)
                    continue;

                List<string> missingOutputs = new();
                if (successMissing)
                    missingOutputs.Add("'SUCCESS'");
                if (failureMissing)
                    missingOutputs.Add("'FAILURE'");

                string outputsLabel = missingOutputs.Count == 1
                    ? $"{missingOutputs[0]} output"
                    : $"{missingOutputs[0]} and {missingOutputs[1]} outputs";

                ConditionalNode target = conditionalNode;
                GraphValidationIssue outputIssue = summary.AddIssue($"Conditional node {DescribeNode(target)} has an unconnected {outputsLabel}.");
                outputIssue?.AddAction($"Focus {DescribeNodeShort(target)}", () => CenterOnNode(target));
            }

            foreach (BaseNode node in nodes.OfType<BaseNode>()) {
                if (!node.ShouldTrackName)
                    continue;

                if (!string.IsNullOrWhiteSpace(node.NodeName))
                    continue;

                string location = node.GroupElement != null ? $" in group '{DescribeGroup(node.GroupElement)}'" : string.Empty;
                BaseNode targetNode = node;
                GraphValidationIssue issue = summary.AddIssue($"Node {DescribeNode(node)}{location} is missing a name.");
                issue?.AddAction($"Focus {DescribeNodeShort(targetNode)}", () => CenterOnNode(targetNode));
            }

            foreach (GroupElement group in graphElements.OfType<GroupElement>()) {
                if (!string.IsNullOrWhiteSpace(group.title))
                    continue;

                GroupElement targetGroup = group;
                GraphValidationIssue issue = summary.AddIssue($"Group {DescribeGroup(group)} is missing a name.");
                issue?.AddAction($"Focus {DescribeGroupShort(targetGroup)}", () => CenterOnGroup(targetGroup));
            }

            return summary;
        }

        static string GetChoiceDisplayLabel(DialogueNodeSaveData choice) {
            if (choice == null)
                return "(unknown)";

            string text = choice.Text;
            if (string.IsNullOrWhiteSpace(text))
                return "(empty)";

            text = text.Trim();
            return text.Length <= 25 ? text : $"{text[..25]}...";
        }

        void RemoveChoice(DialogueNode node, DialogueNodeSaveData choice) {
            if (node == null || choice == null)
                return;

            if (node.Choices == null || node.Choices.Count <= 1)
                return;

            Port portToRemove = node.outputContainer.Children()
                                  .OfType<Port>()
                                  .FirstOrDefault(port => ReferenceEquals(port.userData, choice));
            if (portToRemove == null)
                return;

            List<GraphElement> connections = portToRemove.connections
                                                        .Cast<GraphElement>()
                                                        .ToList();
            if (connections.Count > 0)
                DeleteElements(connections);

            node.Choices.Remove(choice);
            RemoveElement(portToRemove);
            node.RefreshUI();
        }

        static string DescribeNodeShort(BaseNode node) {
            return $"'{GetDisplayableNodeName(node)}'";
        }

        static string DescribeGroupShort(GroupElement group) {
            return $"'{GetDisplayableGroupName(group)}'";
        }

        void CenterOnNode(BaseNode node) {
            if (node == null)
                return;

            ClearSelection();
            AddToSelection(node);
            FrameSelection();
        }

        void CenterOnGroup(GroupElement group) {
            if (group == null)
                return;

            ClearSelection();
            AddToSelection(group);
            FrameSelection();
        }

        static string DescribeNode(BaseNode node) {
            if (node == null)
                return "(unknown node)";

            string name = GetDisplayableNodeName(node);
            string typeName = node.GetType().Name;
            string id = GetShortId(node.ID);

            return $"{name} [{typeName}]{id}";
        }

        static string DescribeGroup(GroupElement group) {
            if (group == null)
                return "(unknown group)";

            string name = GetDisplayableGroupName(group);
            string id = GetShortId(group.ID);

            return $"{name}{id}";
        }

        static string GetShortId(string id) {
            if (string.IsNullOrEmpty(id))
                return string.Empty;

            return $" ({(id.Length <= 8 ? id : id[..8])})";
        }

        static string GetDisplayableNodeName(BaseNode node) {
            if (node == null || string.IsNullOrWhiteSpace(node.NodeName))
                return "(unnamed)";

            return node.NodeName;
        }

        static string GetDisplayableGroupName(GroupElement group) {
            if (group == null || string.IsNullOrWhiteSpace(group.title))
                return "(unnamed)";

            return group.title;
        }
    }
}
#endif