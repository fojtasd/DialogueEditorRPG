#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Contracts.Dialogues.Nodes;
using DialogueEditor.Data.Save;
using DialogueEditor.Elements;
using DialogueEditor.Windows;
using DialogueSystem;
using DialogueSystem.Node;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DialogueEditor.Utilities {
    public static class IOUtility {
        static GraphViewElement _graphViewElement;
        static string _graphFileName;
        static string _containerFolderPath;
        static List<BaseNode> _nodes;
        static List<GroupElement> _groups;
        static List<StickyNoteElement> _stickyNotes;
        static Dictionary<string, GroupSO> _createdDialogueGroups;
        static Dictionary<string, NodeBaseSO> _createdDialogues;
        static Dictionary<string, GroupElement> _loadedGroups;
        static Dictionary<string, BaseNode> _loadedNodes;

        public static void Initialize(GraphViewElement graphViewElement, string graphName) {
            _graphViewElement = graphViewElement;

            _graphFileName = graphName;

            _containerFolderPath = $"Assets/ScriptableObjects/Dialogues/{graphName}";

            _nodes = new List<BaseNode>();
            _groups = new List<GroupElement>();
            _stickyNotes = new List<StickyNoteElement>();

            _createdDialogueGroups = new Dictionary<string, GroupSO>();
            _createdDialogues = new Dictionary<string, NodeBaseSO>();

            _loadedGroups = new Dictionary<string, GroupElement>();
            _loadedNodes = new Dictionary<string, BaseNode>();
        }

        public static void Save(bool saveGraphOnly) {
            if (!saveGraphOnly) {
                CreateDefaultFolders();
            }

            GetElementsFromGraphView();

            var graphData = CreateAsset<GraphSaveDataSO>("Assets/Editor/DialogueEditor/Graphs", $"{_graphFileName}_Graph");

            graphData.Initialize(_graphFileName);

            DialogueContainerSO dialogueContainer = null;
            if (!saveGraphOnly) {
                dialogueContainer = CreateAsset<DialogueContainerSO>(_containerFolderPath, _graphFileName);
                dialogueContainer.Initialize(_graphFileName);
            }

            SaveGroups(graphData, dialogueContainer);
            SaveNodes(graphData, dialogueContainer, saveGraphOnly);
            SaveStickyNotes(graphData);
            
            foreach (var node in _createdDialogues) {
                if (!node.Value.IsStartingNode)
                    continue;
                if (dialogueContainer != null)
                    dialogueContainer.StartingNode = node.Value;
            }

            SaveAsset(graphData);

            if (!saveGraphOnly) {
                SaveAsset(dialogueContainer);
            }
        }

        public static void SaveGraphAs() {
            string defaultName = string.IsNullOrWhiteSpace(_graphFileName) ? "NewDialogue" : _graphFileName;

            string assetPath = EditorUtility.SaveFilePanelInProject(
                                                                    "Save Dialogue Graph As...",
                                                                    $"{defaultName}_Graph",
                                                                    "asset",
                                                                    "Choose file name and location for the graph asset.",
                                                                    "Assets/Editor/DialogueEditor/Graphs"
                                                                   );

            if (string.IsNullOrWhiteSpace(assetPath))
                return;

            // assetPath = "Assets/.../Foo_Graph.asset";
            string fileNameNoExt = Path.GetFileNameWithoutExtension(assetPath);

            // Tvoje CreateAsset<GraphSaveDataSO>(folder, name) očekává folder bez názvu souboru
            // a jméno bez extension
            // Navíc ty skládáš name jako $"{_graphFileName}_Graph"
            // -> takže z "Foo_Graph" uděláme _graphFileName = "Foo"
            var graphSuffix = "_Graph";
            _graphFileName = fileNameNoExt.EndsWith(graphSuffix)
                ? fileNameNoExt[..^graphSuffix.Length]
                : fileNameNoExt;

            Save(true);
        }

        static void SaveGroups(GraphSaveDataSO graphData, DialogueContainerSO dialogueContainer) {
            List<string> groupNames = new();

            foreach (GroupElement group in _groups) {
                SaveGroupToGraph(group, graphData);
                SaveGroupToScriptableObject(group, dialogueContainer);

                groupNames.Add(group.title);
            }

            UpdateOldGroups(groupNames, graphData);
        }

        static void SaveGroupToGraph(GroupElement groupElement, GraphSaveDataSO graphData) {
            GroupSaveData groupData = new() { ID = groupElement.ID, Name = groupElement.title, Position = groupElement.GetPosition().position };

            graphData.Groups.Add(groupData);
        }

        static void SaveGroupToScriptableObject(GroupElement groupElement, DialogueContainerSO dialogueContainer) {
            if (dialogueContainer == null) return;
            string groupName = groupElement.title;

            CreateFolder($"{_containerFolderPath}/Groups", groupName);
            CreateFolder($"{_containerFolderPath}/Groups/{groupName}", "Dialogues");

            var dialogueGroup =
                CreateAsset<GroupSO>($"{_containerFolderPath}/Groups/{groupName}", groupName);

            dialogueGroup.Initialize(groupName);

            _createdDialogueGroups.Add(groupElement.ID, dialogueGroup);

            dialogueContainer.DialogueGroups.Add(dialogueGroup, new List<NodeBaseSO>());

            SaveAsset(dialogueGroup);
        }

        static void UpdateOldGroups(List<string> currentGroupNames, GraphSaveDataSO graphData) {
            if (graphData.OldGroupNames != null && graphData.OldGroupNames.Count != 0) {
                List<string> groupsToRemove = graphData.OldGroupNames.Except(currentGroupNames).ToList();

                foreach (string groupToRemove in groupsToRemove)
                    RemoveFolder($"{_containerFolderPath}/Groups/{groupToRemove}");
            }

            graphData.OldGroupNames = new List<string>(currentGroupNames);
        }

        static void SaveNodes(GraphSaveDataSO graphData, DialogueContainerSO dialogueContainer, bool saveGraphOnly) {
            var groupedNodeNames =
                new SerializableDictionary<string, List<string>>();
            var ungroupedNodeNames = new List<string>();

            List<ConditionalNode> conditionalNodes = new();
            foreach (BaseNode node in _nodes) {
                if (node is ConditionalNode conditional) {
                    conditionalNodes.Add(conditional);
                    continue;
                }

                SaveNodeToGraph(node, graphData);
                if (node.ShouldPersistToAssets && !saveGraphOnly)
                    SaveNodeToScriptableObject(node, dialogueContainer);

                if (!node.ShouldTrackName)
                    continue;

                if (node.GroupElement != null) {
                    groupedNodeNames.AddItem(node.GroupElement.title, node.NodeName);

                    continue;
                }

                ungroupedNodeNames.Add(node.NodeName);
            }

            foreach (ConditionalNode node in conditionalNodes) {
                SaveNodeToGraph(node, graphData);
                if (node.ShouldPersistToAssets && !saveGraphOnly)
                    SaveNodeToScriptableObject(node, dialogueContainer);

                if (!node.ShouldTrackName)
                    continue;

                if (node.GroupElement != null) {
                    groupedNodeNames.AddItem(node.GroupElement.title, node.NodeName);

                    continue;
                }

                ungroupedNodeNames.Add(node.NodeName);
            }

            UpdateDialoguesChoicesConnections();

            UpdateOldGroupedNodes(groupedNodeNames, graphData);
            UpdateOldUngroupedNodes(ungroupedNodeNames, graphData);
        }

        static void SaveStickyNotes(GraphSaveDataSO graphData) {
            if (graphData.StickyNotes == null)
                graphData.StickyNotes = new List<StickyNoteSaveData>();
            else
                graphData.StickyNotes.Clear();

            foreach (StickyNoteElement sticky in _stickyNotes) {
                Rect stickyRect = sticky.GetPosition();
                var stickyData = new StickyNoteSaveData {
                    Text = sticky.GetText(),
                    Position = stickyRect.position,
                    Size = stickyRect.size,
                    ColorClass = sticky.GetColorClassName(),
                    IsLocked = sticky.IsLocked,
                    IsBold = sticky.IsBold,
                    FontSize = sticky.FontSize
                };

                graphData.StickyNotes.Add(stickyData);
            }
        }

        static void SaveNodeToGraph(BaseNode node, GraphSaveDataSO graphData) {
            NodeSaveData nodeData = new(node);
            graphData.Nodes.Add(nodeData);
        }

        static void SaveNodeToScriptableObject(BaseNode node, DialogueContainerSO dialogueContainer) {
            if (node.ChoiceType == ChoiceType.Conditional) {
                ConditionalNodeSO conditional;

                if (node is not ConditionalNode cNode) {
                    Debug.LogError("Ara");
                    return;
                }

                if (cNode.GroupElement != null) {
                    conditional = CreateAsset<ConditionalNodeSO>($"{_containerFolderPath}/Groups/{cNode.GroupElement.title}/Dialogues",
                                                                 cNode.NodeName);

                    dialogueContainer.DialogueGroups.AddItem(_createdDialogueGroups[cNode.GroupElement.ID], conditional);
                } else {
                    conditional = CreateAsset<ConditionalNodeSO>($"{_containerFolderPath}/Global/Dialogues", node.NodeName);

                    dialogueContainer.UngroupedDialogues.Add(conditional);
                }

                string groupIdSanitized = null;


                TryGetDialogue(cNode.SuccessNodeData?.NodeID, out NodeSO dialogueSuccess);
                TryGetDialogue(cNode.FailureNodeData?.NodeID, out NodeSO dialogueFail);
     
                conditional.NextDialogueSuccess = dialogueSuccess;
                conditional.NextDialogueFailure = dialogueFail;
                
                if (cNode.GroupElement != null) groupIdSanitized = cNode.GroupElement.ID;
                conditional.Initialize(
                                       cNode.ID,
                                       groupIdSanitized,
                                       cNode.NodeName,
                                       cNode.ChoiceType,
                                       GraphViewElement.IsStartingNode(cNode),
                                       ConvertNodeConditionalToDialogueConditional(cNode)
                                      );
                _createdDialogues.Add(node.ID, conditional);

                SaveAsset(conditional);
            } else {
                NodeSO dialogue;

                if (node.GroupElement != null) {
                    dialogue = CreateAsset<NodeSO>($"{_containerFolderPath}/Groups/{node.GroupElement.title}/Dialogues",
                                                   node.NodeName);

                    dialogueContainer.DialogueGroups.AddItem(_createdDialogueGroups[node.GroupElement.ID], dialogue);
                } else {
                    dialogue = CreateAsset<NodeSO>($"{_containerFolderPath}/Global/Dialogues", node.NodeName);

                    dialogueContainer.UngroupedDialogues.Add(dialogue);
                }

                string groupIdSanitized = null;
                if (node.GroupElement != null) groupIdSanitized = node.GroupElement.ID;

                Model dialogueNodeModel = null;
                if (node is DialogueNode dialogueNode) {
                    dialogueNodeModel = dialogueNode.Model;
                }

                dialogue.Initialize(
                                    node.ID,
                                    node.NodeName,
                                    groupIdSanitized,
                                    node.ChoiceType,
                                    GraphViewElement.IsStartingNode(node),
                                    dialogueNodeModel
                                   );

                _createdDialogues.Add(node.ID, dialogue);

                SaveAsset(dialogue);
            }
        }

        static List<DialogueChoiceData> ConvertNodeChoicesToDialogueChoices(List<DialogueNodeSaveData> nodeChoices) {
            List<DialogueChoiceData> dialogueChoices = new();

            foreach (DialogueNodeSaveData nodeChoice in nodeChoices) {
                DialogueChoiceData choiceData = new();

                dialogueChoices.Add(choiceData);
            }

            return dialogueChoices;
        }

        static ConditionalNodeData ConvertNodeConditionalToDialogueConditional(ConditionalNode cNode, ConditionalNodeData conditionalData = null) {
            conditionalData ??= new ConditionalNodeData();
            conditionalData.ExpectedValue = cNode.ExpectedValue;
            conditionalData.AttributeType = cNode.AttributeType;
            conditionalData.SkillType = cNode.SkillType;
            conditionalData.Kind = cNode.ConditionTargetType;

            return conditionalData;
        }

        static void UpdateDialoguesChoicesConnections() {
            foreach (BaseNode nodeBase in _nodes) {
                switch (nodeBase) {
                    case DialogueNode dNode: {
                        if (string.IsNullOrEmpty(nodeBase.ID) || !TryGetDialogue(nodeBase.ID, out NodeSO dialogue)) {
                            continue;
                        }

                        dialogue.Choices = ConvertNodeChoicesToDialogueChoices(dNode.Choices);

                        for (var choiceIndex = 0; choiceIndex < dNode.Choices.Count; ++choiceIndex) {
                            DialogueNodeSaveData nodeChoice = dNode.Choices[choiceIndex];

                            if (string.IsNullOrEmpty(nodeChoice.NodeID)) {
                                continue;
                            }

                            string resolvedNodeId = ResolvePersistedNodeId(nodeChoice.NodeID);
                            if (string.IsNullOrEmpty(resolvedNodeId)) {
                                continue;
                            }

                            if (!TryGetDialogue(resolvedNodeId, out NodeBaseSO nextDialogue)) {
                                continue;
                            }

                            dialogue.Choices[choiceIndex].NextDialogue = nextDialogue;

                            SaveAsset(dialogue);
                        }

                        break;
                    }
                    case ConditionalNode cNode: {
                        if (string.IsNullOrEmpty(nodeBase.ID) || !TryGetDialogue(nodeBase.ID, out ConditionalNodeSO conditional)) {
                            continue;
                        }

                        TryGetDialogue(ResolvePersistedNodeId(cNode.SuccessNodeData.NodeID), out NodeBaseSO dialogueOptionSuccess);
                        TryGetDialogue(ResolvePersistedNodeId(cNode.FailureNodeData.NodeID), out NodeBaseSO dialogueOptionFail);

                        conditional.Conditionals??= new ConditionalNodeData();
                        conditional.NextDialogueSuccess = dialogueOptionSuccess;
                        conditional.NextDialogueFailure = dialogueOptionFail;
                        conditional.Conditionals.ExpectedValue = cNode.ExpectedValue;
                        conditional.Conditionals.SkillType = cNode.SkillType;
                        conditional.Conditionals.AttributeType = cNode.AttributeType;

                        SaveAsset(conditional);
                        return;
                    }
                }
            }
        }

        static string ResolvePersistedNodeId(string nodeID, HashSet<string> visited = null) {
            if (string.IsNullOrEmpty(nodeID))
                return null;

            if (_createdDialogues.TryGetValue(nodeID, out _))
                return nodeID;

            BaseNode relayCandidate = _nodes.FirstOrDefault(n => n.ID == nodeID);
            if (relayCandidate is not RelayNode relayNode)
                return nodeID;

            visited ??= new HashSet<string>();
            if (!visited.Add(nodeID))
                return null;

            return ResolvePersistedNodeId(relayNode.ConnectionData?.NodeID, visited);
        }

        static bool TryGetDialogue<T>(string nodeID, out T dialogue, Object context = null) where T : NodeBaseSO {
            dialogue = null;
            if (nodeID == null) {
                return false;
            }

            if (nodeID == "") {
                return false;
            }

            if (!_createdDialogues.TryGetValue(nodeID, out NodeBaseSO baseDialogue)) {
                return false;
            }

            return baseDialogue.TryAs(out dialogue, context);
        }

        static void UpdateOldGroupedNodes(SerializableDictionary<string, List<string>> currentGroupedNodeNames,
                                          GraphSaveDataSO graphData) {
            if (graphData.OldGroupedNodeNames != null && graphData.OldGroupedNodeNames.Count != 0)
                foreach (KeyValuePair<string, List<string>> oldGroupedNode in graphData.OldGroupedNodeNames) {
                    var nodesToRemove = new List<string>();

                    if (currentGroupedNodeNames.TryGetValue(oldGroupedNode.Key, out List<string> name))
                        nodesToRemove = oldGroupedNode.Value.Except(name)
                                                      .ToList();

                    foreach (string nodeToRemove in nodesToRemove)
                        RemoveAsset($"{_containerFolderPath}/Groups/{oldGroupedNode.Key}/Dialogues", nodeToRemove);
                }

            graphData.OldGroupedNodeNames = new SerializableDictionary<string, List<string>>(currentGroupedNodeNames);
        }

        static void UpdateOldUngroupedNodes(List<string> currentUngroupedNodeNames, GraphSaveDataSO graphData) {
            if (graphData.OldUngroupedNodeNames != null && graphData.OldUngroupedNodeNames.Count != 0) {
                List<string> nodesToRemove = graphData.OldUngroupedNodeNames.Except(currentUngroupedNodeNames).ToList();

                foreach (string nodeToRemove in nodesToRemove)
                    RemoveAsset($"{_containerFolderPath}/Global/Dialogues", nodeToRemove);
            }

            graphData.OldUngroupedNodeNames = new List<string>(currentUngroupedNodeNames);
        }

        public static void Load() {
            var graphData =
                LoadAsset<GraphSaveDataSO>("Assets/Editor/DialogueEditor/Graphs", _graphFileName);

            if (graphData == null) {
                EditorUtility.DisplayDialog(
                                            "Could not find the file!",
                                            "The file at the following path could not be found:\n\n" +
                                            $"\"Assets/Editor/DialogueSystem/Graphs/{_graphFileName}\".\n\n" +
                                            "Make sure you chose the right file and it's placed at the folder path mentioned above.",
                                            "Thanks!"
                                           );

                return;
            }

            DialogueEditorWindow.UpdateFileName(graphData.FileName);
            // Notes are loaded based on the File Name field in the editor window.
            LoadGroups(graphData.Groups);
            LoadNodes(graphData.Nodes);
            LoadStickyNotes(graphData.StickyNotes);
            LoadNodesConnections();
        }

        static void LoadGroups(List<GroupSaveData> groups) {
            foreach (GroupSaveData groupData in groups) {
                GroupElement group = _graphViewElement.CreateGroup(groupData.Name, groupData.Position);

                group.ID = groupData.ID;

                _loadedGroups.Add(group.ID, group);
            }
        }

        static void LoadNodes(List<NodeSaveData> nodes) {
            foreach (NodeSaveData nodeData in nodes) {
                if (nodeData.NodeType == GraphNodeType.Relay) {
                    LoadRelayNode(nodeData);
                    continue;
                }

                if (nodeData.ChoiceType == ChoiceType.Conditional || nodeData.NodeType == GraphNodeType.Conditional) {
                    ConditionalNodeSaveData dialogueNodeData1 = new() { Text = nodeData.Conditionals.failure.Text, NodeID = nodeData.Conditionals.failure.NodeID, OutputType = nodeData.Conditionals.failure.OutputType };
                    ConditionalNodeSaveData dialogueNodeData2 = new() { Text = nodeData.Conditionals.success.Text, NodeID = nodeData.Conditionals.success.NodeID, OutputType = nodeData.Conditionals.success.OutputType };
                    LoadConditionalNodes(nodeData, new List<ConditionalNodeSaveData> { dialogueNodeData1, dialogueNodeData2 });
                    continue;
                }

                LoadDialogueNodes(nodeData, CloneNodeChoices(nodeData.Choices));
            }
        }

        static void LoadRelayNode(NodeSaveData nodeData) {
            RelayNode relayNode = _graphViewElement.CreateRelayNode(nodeData.Position, false);

            relayNode.ID = nodeData.ID;
            relayNode.NodeName = nodeData.NodeName;

            RelayNodeSaveData relayConnection = nodeData.RelayConnection != null
                ? new RelayNodeSaveData { NodeID = nodeData.RelayConnection.NodeID }
                : new RelayNodeSaveData();

            relayNode.SetConnectionData(relayConnection);

            if (!string.IsNullOrEmpty(nodeData.GroupID)) {
                GroupElement group = _loadedGroups[nodeData.GroupID];
                relayNode.GroupElement = group;
                group.AddElement(relayNode);
            }

            _graphViewElement.AddElement(relayNode);
            _loadedNodes.Add(relayNode.ID, relayNode);
            relayNode.Draw();
        }

        static void LoadConditionalNodes(NodeSaveData nodeData, List<ConditionalNodeSaveData> conditionals) {
            ConditionalNode conditionalNode = _graphViewElement.CreateConditionalNode(nodeData.Position, nodeData.NodeName, false);

            conditionalNode.ChoiceType = nodeData.ChoiceType;
            conditionalNode.ID = nodeData.ID;
            conditionalNode.NodeName = nodeData.NodeName;
            if (!string.IsNullOrEmpty(nodeData.GroupID)) {
                GroupElement group = _loadedGroups[nodeData.GroupID];
                conditionalNode.GroupElement = group;
                group.AddElement(conditionalNode);
            }

            conditionalNode.FailureNodeData = conditionals.FirstOrDefault(a => a.OutputType == ConditionalOutputType.Failure);
            conditionalNode.SuccessNodeData = conditionals.FirstOrDefault(a => a.OutputType == ConditionalOutputType.Success);
            conditionalNode.AttributeType = nodeData.Conditionals.attributeType;
            conditionalNode.SkillType = nodeData.Conditionals.skillType;
            conditionalNode.ConditionTargetType = nodeData.Conditionals.kind;
            conditionalNode.ExpectedValue = nodeData.Conditionals.expectedValue;

            _graphViewElement.AddElement(conditionalNode);
            _loadedNodes.Add(conditionalNode.ID, conditionalNode);
            conditionalNode.Draw();
        }

        static void LoadDialogueNodes(NodeSaveData nodeData, List<DialogueNodeSaveData> choices) {
            DialogueNode dialogueNode = _graphViewElement.CreateDialogueNode(nodeData.Position, nodeData.NodeName, false);

            dialogueNode.ChoiceType = nodeData.ChoiceType;
            dialogueNode.ID = nodeData.ID;
            dialogueNode.NodeName = nodeData.NodeName;
            dialogueNode.Choices = choices;

            dialogueNode.Model.Text = nodeData.Text;
            dialogueNode.Model.SpeakerType = nodeData.SpeakerType;
            dialogueNode.SetNodeColorBasedOnSpeakerType();

            dialogueNode.ReplaceNodeSettings(nodeData.NodeSettings);

            _graphViewElement.AddElement(dialogueNode);
            _loadedNodes.Add(dialogueNode.ID, dialogueNode);
            dialogueNode.Draw();

            if (string.IsNullOrEmpty(nodeData.GroupID)) return;

            GroupElement group = _loadedGroups[nodeData.GroupID];
            dialogueNode.GroupElement = group;
            group.AddElement(dialogueNode);
        }

        static void LoadStickyNotes(List<StickyNoteSaveData> stickyNotes) {
            if (stickyNotes == null || stickyNotes.Count == 0)
                return;

            foreach (StickyNoteSaveData stickyData in stickyNotes)
                _graphViewElement.AddSticky(stickyData.Position,
                                            stickyData.Size,
                                            stickyData.Text,
                                            stickyData.ColorClass,
                                            stickyData.IsLocked,
                                            stickyData.IsBold,
                                            stickyData.FontSize);
        }

        static void LoadNodesConnections() {
            foreach (KeyValuePair<string, BaseNode> loadedNodeKvp in _loadedNodes) {
                BaseNode node = loadedNodeKvp.Value;

                foreach (Port choicePort in node.outputContainer.Children().OfType<Port>()) {
                    if (choicePort.userData is DialogueNodeSaveData choiceData) {
                        if (string.IsNullOrEmpty(choiceData.NodeID))
                            continue;

                        if (!_loadedNodes.TryGetValue(choiceData.NodeID, out BaseNode nextNode)) {
                            Debug.LogWarning($"Node with ID {choiceData.NodeID} not found. Skipping connection.");
                            continue;
                        }

                        Port nextNodeInputPort = nextNode.inputContainer.Children().OfType<Port>().FirstOrDefault();
                        if (nextNodeInputPort == null) {
                            Debug.LogWarning($"Node {choiceData.NodeID} has no input Port. Skipping connection.");
                            continue;
                        }

                        var edge = new EdgeElement { output = choicePort, input = nextNodeInputPort };

                        choicePort.Connect(edge);
                        nextNodeInputPort.Connect(edge);
                        _graphViewElement.AddElement(edge);

                        node.RefreshUI();
                        nextNode.RefreshUI();

                        Color c = edge.GetAssignedColor();
                        choicePort.portColor = c;
                        nextNodeInputPort.portColor = c;
                        continue;
                    }

                    if (choicePort.userData is ConditionalNodeSaveData conditionalData) {
                        if (string.IsNullOrEmpty(conditionalData.NodeID))
                            continue;

                        if (!_loadedNodes.TryGetValue(conditionalData.NodeID, out BaseNode nextNode)) {
                            Debug.LogWarning($"Node {conditionalData.NodeID} not found. Skipping connection.");
                            continue;
                        }

                        Port nextNodeInputPort = nextNode.inputContainer.Children().OfType<Port>().FirstOrDefault();
                        if (nextNodeInputPort == null) {
                            Debug.LogWarning($"Node {conditionalData.NodeID} has no input Port. Skipping connection.");
                            continue;
                        }

                        var edge = new EdgeElement { output = choicePort, input = nextNodeInputPort };

                        choicePort.Connect(edge);
                        nextNodeInputPort.Connect(edge);
                        _graphViewElement.AddElement(edge);

                        node.RefreshUI();
                        nextNode.RefreshUI();

                        continue;
                    }

                    if (choicePort.userData is RelayNodeSaveData relayData) {
                        if (string.IsNullOrEmpty(relayData.NodeID))
                            continue;

                        if (!_loadedNodes.TryGetValue(relayData.NodeID, out BaseNode nextNode)) {
                            Debug.LogWarning($"Node {relayData.NodeID} not found. Skipping connection.");
                            continue;
                        }

                        Port nextNodeInputPort = nextNode.inputContainer.Children().OfType<Port>().FirstOrDefault();
                        if (nextNodeInputPort == null) {
                            Debug.LogWarning($"Node {relayData.NodeID} has no input Port. Skipping connection.");
                            continue;
                        }

                        var edge = new EdgeElement { output = choicePort, input = nextNodeInputPort };

                        choicePort.Connect(edge);
                        nextNodeInputPort.Connect(edge);
                        _graphViewElement.AddElement(edge);

                        node.RefreshUI();
                        nextNode.RefreshUI();

                        Color c = edge.GetAssignedColor();
                        choicePort.portColor = c;
                        nextNodeInputPort.portColor = c;
                    }
                }

                node.RefreshPorts();
            }
        }

        static void CreateDefaultFolders() {
            CreateFolder("Assets/ScriptableObjects/Dialogues", _graphFileName);
            CreateFolder(_containerFolderPath, "Global");
            CreateFolder(_containerFolderPath, "Groups");
            CreateFolder($"{_containerFolderPath}/Global", "Dialogues");
        }

        static void GetElementsFromGraphView() {
            Type groupType = typeof(GroupElement);

            _nodes.Clear();
            _groups.Clear();
            _stickyNotes.Clear();

            _graphViewElement.graphElements.ForEach(graphElement => {
                if (graphElement is BaseNode node) {
                    _nodes.Add(node);

                    return;
                }

                if (graphElement is StickyNoteElement sticky) {
                    _stickyNotes.Add(sticky);

                    return;
                }

                if (graphElement.GetType() != groupType)
                    return;
                var group = (GroupElement)graphElement;
                _groups.Add(group);
            });
        }

        static void CreateFolder(string parentFolderPath, string newFolderName) {
            if (AssetDatabase.IsValidFolder($"{parentFolderPath}/{newFolderName}")) return;

            AssetDatabase.CreateFolder(parentFolderPath, newFolderName);
        }

        static void RemoveFolder(string path) {
            FileUtil.DeleteFileOrDirectory($"{path}.meta");
            FileUtil.DeleteFileOrDirectory($"{path}/");
        }

        static T CreateAsset<T>(string path, string assetName) where T : ScriptableObject {
            var fullPath = $"{path}/{assetName}.asset";

            var asset = LoadAsset<T>(path, assetName);

            if (asset != null)
                return asset;
            asset = ScriptableObject.CreateInstance<T>();

            AssetDatabase.CreateAsset(asset, fullPath);

            return asset;
        }

        static T LoadAsset<T>(string path, string assetName) where T : ScriptableObject {
            var fullPath = $"{path}/{assetName}.asset";

            return AssetDatabase.LoadAssetAtPath<T>(fullPath);
        }

        static void SaveAsset(Object asset) {
            EditorUtility.SetDirty(asset);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void RemoveAsset(string path, string assetName) {
            AssetDatabase.DeleteAsset($"{path}/{assetName}.asset");
        }

        public static List<DialogueNodeSaveData> CloneNodeChoices(List<DialogueNodeSaveData> nodeChoices) {
            return nodeChoices.Select(choice => new DialogueNodeSaveData { Text = choice.Text, NodeID = choice.NodeID }).ToList();
        }
    }
}

#endif