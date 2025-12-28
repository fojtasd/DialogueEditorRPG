#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Dialogues;
using Contracts.Dialogues.Nodes;
using DialogueEditor.Data.Save;
using DialogueEditor.Utilities;
using DialogueSystem.Node;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public sealed class DialogueNode : BaseNode {
        readonly Dictionary<ElementID, VisualElement> _elementLibrary = new();
        readonly List<INodeSetting> _nodeSettings = new();

        List<DialogueNodeSaveData> _choices = new();
        IDisposable _nodeNameEditScope;
        IDisposable _dialogueTextEditScope;

        public DialogueNode() {
            Model.NodeSettings = _nodeSettings;
            Choices.Add(new DialogueNodeSaveData());
        }

        public List<DialogueNodeSaveData> Choices {
            get => _choices;
            set => _choices = value ?? new List<DialogueNodeSaveData>();
        }

        public Model Model { get; } = new();

        public override void Initialize(string nodeName, GraphViewElement graphViewElement, Vector2 position) {
            base.Initialize(nodeName, graphViewElement, position);
            SubscribeToGraphEvents();
        }

        public void Draw() {
            titleContainer.Clear();
            BuíldElements();
            AddElementsToContainers();
            SetStyles();

            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
        }

        void AddElementsToContainers() {
            titleContainer.Add(_elementLibrary[ElementID.NodeNameLabel]);
            titleContainer.Add(_elementLibrary[ElementID.NodeNameTextField]);
            titleContainer.Add(_elementLibrary[ElementID.NodeIndicator]);
            titleContainer.Add(_elementLibrary[ElementID.SpeakerTypeDropdown]);
            titleContainer.Add(_elementLibrary[ElementID.CenterButton]);

            inputContainer.Add(_elementLibrary[ElementID.InputPort]);
            
            extensionContainer.Add(_elementLibrary[ElementID.DialogueTextField]);
            extensionContainer.Add(_elementLibrary[ElementID.SettingsFoldout]);

            topContainer.Add(inputContainer);
            topContainer.Add(outputContainer);

            mainContainer.Add(extensionContainer);
        }

        void SetStyles() {
            mainContainer.AddToClassList("ds-node__main-container");
            extensionContainer.AddToClassList("ds-node__extension-container");
            inputContainer.AddToClassList("node__input-container");
            outputContainer.AddToClassList("node__output-container");
            topContainer.AddToClassList("node__top-container");
            _elementLibrary[ElementID.DialogueTextField].AddClasses(
                                                                    "ds-node__text-field",
                                                                    "ds-node__text-field__hidden",
                                                                    "ds-node__filename-text-field",
                                                                    "node__dialogue-text-input-field"
                                                                   );
        }

        void BuíldElements() {
            // NODE NAME
            var nodeNameLabel = new Label("Node Name") { style = { fontSize = 13 } };
            TextField nodeName = ElementUtility.CreateTextField(NodeName, null, callback => {
                if (_nodeNameEditScope == null)
                    _nodeNameEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Rename dialogue node");

                var target = (TextField)callback.target;

                target.value = callback.newValue.RemoveWhitespaces().RemoveSpecialCharacters();

                if (string.IsNullOrEmpty(target.value)) {
                    if (!string.IsNullOrEmpty(NodeName)) {
                        ++GraphViewElement.NameErrorsAmount;
                    }
                } else {
                    if (string.IsNullOrEmpty(NodeName)) {
                        --GraphViewElement.NameErrorsAmount;
                    }
                }

                if (GroupElement == null) {
                    GraphViewElement.RemoveUngroupedNode(this);
                    NodeName = target.value;
                    GraphViewElement.AddUngroupedNode(this);
                    return;
                }

                GroupElement currentGroupElement = GroupElement;
                GraphViewElement.RemoveGroupedNode(this, GroupElement);
                NodeName = target.value;
                GraphViewElement.AddGroupedNode(this, currentGroupElement);
            });
            nodeName.RegisterCallback<FocusInEvent>(_ => {
                _nodeNameEditScope?.Dispose();
                _nodeNameEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Rename dialogue node");
            });
            nodeName.RegisterCallback<FocusOutEvent>(_ => {
                _nodeNameEditScope?.Dispose();
                _nodeNameEditScope = null;
            });
            nodeName.RegisterCallback<DetachFromPanelEvent>(_ => {
                _nodeNameEditScope?.Dispose();
                _nodeNameEditScope = null;
            });
            nodeName.style.fontSize = 13;
            nodeName.tooltip = Constants.NodeNameTooltip;
            nodeNameLabel.tooltip = Constants.NodeNameTooltip;
            nodeName.AddToClassList("node__title-text-field");
            nodeNameLabel.AddToClassList("node__title-text-label");
            _elementLibrary.Add(ElementID.NodeNameLabel, nodeNameLabel);
            _elementLibrary.Add(ElementID.NodeNameTextField, nodeName);

            // INDICATOR OF RULES
            var indicator = new NodeIndicator(_nodeSettings);
            _elementLibrary.Add(ElementID.NodeIndicator, indicator);

            // SPEAKER TYPE
            EnumField speakerTypeDropdown = new("", Model.SpeakerType) { tooltip = Constants.SpeakerTooltip };
            SetNodeColorBasedOnSpeakerType();
            speakerTypeDropdown.RegisterValueChangedCallback(evt => {
                GraphViewElement.UndoManager.RecordNodeChange(this, "Change speaker type", () => {
                    Model.SpeakerType = (SpeakerType)evt.newValue;
                    SetNodeColorBasedOnSpeakerType();
                });
            });
            speakerTypeDropdown.AddToClassList("node_title-speaker-dropdown");
            _elementLibrary.Add(ElementID.SpeakerTypeDropdown, speakerTypeDropdown);

            // FOCUS ON BUTTON
            Button centerButton = new(() => { FocusOn(this); }) { text = "Focus On", tooltip = Constants.FocusOnButtonTooltip };
            centerButton.AddToClassList("ds-node__title-center-button");
            _elementLibrary.Add(ElementID.CenterButton, centerButton);

            // INPUT PORT
            Port inputPort = CreatePort("", Orientation.Horizontal,
                                        Direction.Input, Port.Capacity.Multi);
            _elementLibrary.Add(ElementID.InputPort, inputPort);

            // ADD CHOICE BUTTON
            Button addChoiceButton = ElementUtility.CreateButton("✚ New Choice", () => {
                GraphViewElement.UndoManager.RecordNodeChange(this, "Add dialogue choice", () => {
                    DialogueNodeSaveData dialogueNodeData = new();
                    Choices.Add(dialogueNodeData);

                    if (Choices.Count > 1) ChoiceType = ChoiceType.MultipleChoice;

                    Port choicePort = CreateChoiceOptionPort(GraphViewElement, dialogueNodeData);

                    outputContainer.Add(choicePort);
                });
            });
            addChoiceButton.tooltip = Constants.AddChoiceButtonTooltip;
            addChoiceButton.style.fontSize = 13;

            outputContainer.Add(addChoiceButton);

            // OUTPUT PORT
            if (Choices is { Count: > 0 }) {
                foreach (DialogueNodeSaveData choice in Choices) {
                    Port outputPort = CreateChoiceOptionPort(GraphViewElement, choice);
                    outputContainer.Add(outputPort);
                }
            } else {
                var choiceData = new DialogueNodeSaveData();
                Choices.Add(choiceData);
                Port outputPort = CreateChoiceOptionPort(GraphViewElement, choiceData);
                outputContainer.Add(outputPort);
            }

            // TEXT FIELD
            VisualElement nodeNameField = new();
            TextField dialogueTextField = ElementUtility.CreateTextArea(Model.Text, null, callback => {
                if (_dialogueTextEditScope == null)
                    _dialogueTextEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Edit dialogue text");

                Model.Text = callback.newValue;
                RefreshPreviousNodeButtons();
            });
            dialogueTextField.RegisterCallback<FocusInEvent>(_ => {
                _dialogueTextEditScope?.Dispose();
                _dialogueTextEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Edit dialogue text");
            });
            dialogueTextField.RegisterCallback<FocusOutEvent>(_ => {
                _dialogueTextEditScope?.Dispose();
                _dialogueTextEditScope = null;
            });
            dialogueTextField.RegisterCallback<DetachFromPanelEvent>(_ => {
                _dialogueTextEditScope?.Dispose();
                _dialogueTextEditScope = null;
            });
            dialogueTextField.style.maxHeight = 500;
            nodeNameField.Add(dialogueTextField);
            _elementLibrary.Add(ElementID.DialogueTextField, dialogueTextField);

            // NODE SETTINGS
            Foldout foldout = ElementUtility.CreateFoldout("Visibility & Consequences Rules", _nodeSettings.Count == 0);
            foldout.style.paddingTop = 8;
            var settingsListContainer = new VisualElement { name = "Settings List Container" };
            foldout.Add(settingsListContainer);
            _elementLibrary.Add(ElementID.SettingsFoldout, foldout);
            
            NodeSettingsListElement nodeSettingsListElement = new(_nodeSettings,
                                                                  GraphViewElement?.EditorWindow,
                                                                  GraphViewElement?.UndoManager,
                                                                  this);
            nodeSettingsListElement.ListChanged += () => { indicator.RefreshUI(_nodeSettings.Count > 0); };
            settingsListContainer.Add(nodeSettingsListElement);
            _elementLibrary.Add(ElementID.SettingsList, settingsListContainer);
        }
        
        void SubscribeToGraphEvents() {
            GraphViewElement.OnNodeDeletion += node => { ClearDeletedNodeFromChoices(node.ID); };

            GraphViewElement.graphViewChanged = change => {
                EdgeElement.HandleEdgeChange(change);
                if (change.edgesToCreate is { Count: > 0 }) {
                    List<Edge> filteredEdges = change.edgesToCreate
                                                     .Where(edge => edge is { parent: null, output: not null, input: not null })
                                                     .GroupBy(edge => (edge.output, edge.input))
                                                     .Select(group => group.First())
                                                     .Where(edge => !edge.output.connections
                                                                         .Any(existingEdge => existingEdge != null &&
                                                                                              existingEdge != edge &&
                                                                                              existingEdge.input == edge.input))
                                                     .ToList();

                    change.edgesToCreate = filteredEdges;
                }

                return change;
            };
        }

        void ClearDeletedNodeFromChoices(string deletedNodeId) {
            foreach (DialogueNodeSaveData choice in Choices.Where(choice => choice.NodeID == deletedNodeId)) {
                choice.NodeID = null;
                return;
            }
        }

        public void ReplaceNodeSettings(IEnumerable<INodeSetting> settings) {
            _nodeSettings.Clear();

            if (settings != null) {
                foreach (INodeSetting setting in settings) {
                    if (setting == null)
                        continue;

                    _nodeSettings.Add(setting.DeepClone());
                }
            }

            Model.NodeSettings = _nodeSettings;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Ports/Disconnect Input Ports", _ => GraphViewElement.DisconnectInputPorts(this));
            evt.menu.AppendAction("Ports/Disconnect Output Ports", _ => GraphViewElement.DisconnectOutputPorts(this));
        }

        static Port CreatePort(
            string portName = "",
            Orientation orientation = Orientation.Horizontal,
            Direction direction = Direction.Output,
            Port.Capacity capacity = Port.Capacity.Single
        ) {
            var port = Port.Create<EdgeElement>(orientation, direction, capacity, typeof(float));
            port.portName = portName;
            var connectorListener = new ConnectorListener();
            var edgeConnector = new EdgeConnector<EdgeElement>(connectorListener);
            port.AddManipulator(edgeConnector);
            return port;
        }

        Port CreateChoiceOptionPort(
            GraphViewElement graphViewElement,
            DialogueNodeSaveData dialogueNodeData
        ) {
            Port choicePort = CreatePort();
            choicePort.userData = dialogueNodeData;
            IDisposable choiceTextEditScope = null;
            Button deleteChoiceButton = ElementUtility.CreateButton("X", () => {
                GraphViewElement.UndoManager.RecordNodeChange(this, "Remove dialogue choice", () => {
                    if (Choices.Count == 1) {
                        ChoiceType = ChoiceType.SingleChoice;
                        return;
                    }

                    if (choicePort.connected) graphViewElement.DeleteElements(choicePort.connections);
                    Choices.Remove(dialogueNodeData);
                    graphViewElement.RemoveElement(choicePort);
                });
            });
            deleteChoiceButton.tooltip = Constants.DeleteChoiceButtonTooltip;
            
            TextField choiceTextField = ElementUtility.CreateTextField(dialogueNodeData.Text);
            choiceTextField.RegisterValueChangedCallback(evt => {
                if (choiceTextEditScope == null)
                    choiceTextEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Edit choice text");

                dialogueNodeData.Text = evt.newValue;
            });
            choiceTextField.RegisterCallback<FocusInEvent>(_ => {
                choiceTextEditScope?.Dispose();
                choiceTextEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Edit choice text");
            });
            choiceTextField.RegisterCallback<FocusOutEvent>(_ => {
                choiceTextEditScope?.Dispose();
                choiceTextEditScope = null;
            });
            choiceTextField.RegisterCallback<DetachFromPanelEvent>(_ => {
                choiceTextEditScope?.Dispose();
                choiceTextEditScope = null;
            });

            Button followChoiceButton = new(() => { FollowNode(choicePort); }) { text = "Follow", tooltip = Constants.FollowNextNodeButtonTooltip };

            followChoiceButton.schedule.Execute(() => {
                var edge = (EdgeElement)choicePort.connections.FirstOrDefault();
                if (edge is not { input: { node: DialogueNode nextNode } }) return;
                followChoiceButton.tooltip = $"{nextNode.Model.Text}";
                followChoiceButton.style.backgroundColor = edge.GetAssignedColor();
                followChoiceButton.style.color = Color.black;
            }).ExecuteLater(300);

            choiceTextField.AddClasses(
                                       "ds-node__text-field",
                                       "ds-node__text-field__hidden",
                                       "ds-node__choice-text-field"
                                      );
            
            choicePort.Add(followChoiceButton);
            choicePort.Add(choiceTextField);
            choicePort.Add(deleteChoiceButton);
            return choicePort;
        }

        public void SetNodeColorBasedOnSpeakerType() {
            mainContainer.style.borderTopWidth = 5;
            titleContainer.style.backgroundColor = new StyleColor(Constants.GetSpeakerBorderColor(Model.SpeakerType));
        }
        
        void FollowNode(Port outputPort) {
            Edge edge = outputPort.connections.FirstOrDefault();

            if (edge == null) {
                Debug.Log("End of dialogue path.");
                return;
            }

            var nextNode = (BaseNode)edge.input.node;
            HighlightNode(nextNode);

            FocusOn(nextNode);
        }

        void FocusOn(BaseNode dialogueNode) {
            GraphViewElement.ClearSelection();
            GraphViewElement.AddToSelection(dialogueNode);
            GraphViewElement.FrameSelection();
        }

        void FollowPreviousNode(BaseNode previousNode) {
            FocusOn(previousNode);
            HighlightNode(previousNode);
        }

        void RefreshPreviousNodeButtons() {
            var inputPort = (Port)inputContainer.Children().First();
            
            foreach (Button button in inputContainer.Children().OfType<Button>().ToList())
                inputContainer.Remove(button);

            List<DialogueNode> previousNodes = inputPort.connections
                                                        .Select(edge => edge.output.node as DialogueNode)
                                                        .Where(node => node != null)
                                                        .OrderBy(n => n.NodeName)
                                                        .ToList();

            foreach (DialogueNode node in previousNodes) {
                Button previousNodeButton = new(() => { FollowPreviousNode(node); }) {
                    name = "previousNodeButton", 
                    text = "Node " + node.NodeName, 
                    tooltip = node.Model.Text.Substring(0, Mathf.Min(25, node.Model.Text.Length))
                };
                previousNodeButton.AddToClassList("ds-node__previous_node_button");

                Edge matchingEdge = inputPort.connections.FirstOrDefault(edge => edge.output?.node == node);
                if (matchingEdge is EdgeElement edgeElement) {
                    Color assignedColor = edgeElement.GetAssignedColor();
                    previousNodeButton.style.backgroundColor = assignedColor;
                    previousNodeButton.style.color = Color.black;
                    previousNodeButton.style.borderBottomColor = assignedColor;
                    previousNodeButton.style.borderTopColor = assignedColor;
                    previousNodeButton.style.borderLeftColor = assignedColor;
                    previousNodeButton.style.borderRightColor = assignedColor;
                }

                inputContainer.Add(previousNodeButton);
            }
        }

        void RefreshNextNodeButtons() {
            foreach (VisualElement element in outputContainer.Children().ToList())
                if (element is Port port) {
                    Button button = port.Children().OfType<Button>().FirstOrDefault();

                    var edge = (EdgeElement)port.connections.FirstOrDefault();

                    if (edge?.input?.node is not DialogueNode nextNode) {
                        continue;
                    }

                    if (button == null) continue;
                    button.style.color = edge.GetAssignedColor();
                    button.tooltip = $"{nextNode.Model.Text}";
                    button.style.backgroundColor = edge.GetAssignedColor();
                    button.style.color = Color.black;
                    button.style.maxWidth = 60;
                }
        }

        protected override void RefreshUIInternal() {
            RefreshNextNodeButtons();
            RefreshPreviousNodeButtons();
        }
    }
}

#endif