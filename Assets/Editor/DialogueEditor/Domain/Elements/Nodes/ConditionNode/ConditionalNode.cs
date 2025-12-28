#if UNITY_EDITOR
using System;
using Contracts.Contracts;
using DialogueEditor.Data.Save;
using DialogueEditor.Utilities;
using DialogueSystem.Node;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public sealed class ConditionalNode : BaseNode {
        Port _failOutputPort;
        VisualElement _row0;
        VisualElement _row1;
        VisualElement _row2;
        Port _successOutputPort;
        public AttributeType AttributeType;
        public ConditionTargetType ConditionTargetType;

        public int ExpectedValue;

        public ConditionalNodeSaveData FailureNodeData = new() { OutputType = ConditionalOutputType.Failure, Text = "FAILURE" };
        public SkillType SkillType;
        public ConditionalNodeSaveData SuccessNodeData = new() { OutputType = ConditionalOutputType.Success, Text = "SUCCESS" };

        public ConditionalNode() {
            ChoiceType = ChoiceType.Conditional;
        }

        IDisposable _nodeNameEditScope;
        IDisposable _expectedValueEditScope;

        public void Draw() {
            extensionContainer.Clear();
            titleContainer.Clear();

            // HEADER
            VisualElement header = CreateHeader();

            titleContainer.Add(header);
            titleContainer.style.height = 86;
            titleContainer.style.maxWidth = 150;
            // OUTPUT
            _successOutputPort = Port.Create<EdgeElement>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));
            _failOutputPort = Port.Create<EdgeElement>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(float));

            _successOutputPort.portName = "SUCCESS";
            _failOutputPort.portName = "FAILURE";
            _successOutputPort.userData = SuccessNodeData;
            _failOutputPort.userData = FailureNodeData;

            _successOutputPort.AddManipulator(CreateEdgeConnector());
            _failOutputPort.AddManipulator(CreateEdgeConnector());
            outputContainer.Add(_successOutputPort);
            outputContainer.Add(_failOutputPort);


            mainContainer.Add(extensionContainer);

            // INPUT
            var inputPort = Port.Create<EdgeElement>(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(float));
            inputPort.portName = "IN";
            inputPort.AddManipulator(CreateEdgeConnector());
            inputContainer.Add(inputPort);
            inputContainer.style.height = 60;
            inputContainer.style.justifyContent = Justify.Center;

            topContainer.Add(inputContainer);
            topContainer.Add(outputContainer);

            RefreshExpandedState();
            RefreshPorts();
        }

        VisualElement CreateHeader() {
            var container = new VisualElement();
            _row0 = new VisualElement();
            _row1 = new VisualElement();
            _row2 = new VisualElement();

            outputContainer.AddToClassList("node__output-container");
            container.AddToClassList("cnode__header-container");
            _row0.AddToClassList("cnode__header-row0");
            _row1.AddToClassList("cnode__header-row1");
            _row2.AddToClassList("cnode__header-row2");
            
            Label nodeNameLabel = new Label("Node Name") { style = { fontSize = 13 } };
            TextField nodeName = ElementUtility.CreateTextField(NodeName, null, callback => {
                if (_nodeNameEditScope == null)
                    _nodeNameEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Rename conditional node");

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
                _nodeNameEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Rename conditional node");
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
            nodeName.style.maxWidth = 50;
            nodeName.tooltip = Constants.NodeNameTooltip;
            nodeNameLabel.tooltip = Constants.NodeNameTooltip;
            nodeNameLabel.style.alignSelf = Align.Center;
            nodeNameLabel.style.paddingLeft = 5;

            var label = new Label();
            label.style.fontSize = 15;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.alignSelf = Align.Center;
            label.style.paddingLeft = 15;
            label.text = "IF";

            RefreshUIInternal();
            EnumField ifNodeTypeDropdown = new("", ConditionTargetType) { tooltip = Constants.IfNodeTypeTooltip };
            ifNodeTypeDropdown.RegisterValueChangedCallback(evt => {
                GraphViewElement.UndoManager.RecordNodeChange(this, "Change node condition target", () => {
                    ConditionTargetType = (ConditionTargetType)evt.newValue;
                    RefreshUIInternal();
                });
            });
            ifNodeTypeDropdown.AddToClassList("cnode_type-dropdown");

            _row0.Add(nodeNameLabel);
            _row0.Add(nodeName);
            _row1.Add(label);
            _row1.Add(ifNodeTypeDropdown);

            var divider = new VisualElement();
            divider.style.height = 1;
            divider.style.marginBottom = 1;
            divider.style.borderBottomColor = Color.black;
            divider.style.borderBottomWidth = 0.6f;
            divider.style.paddingTop = 4;

            container.Add(_row0);
            container.Add(_row1);
            container.Add(divider);
            container.Add(_row2);
            return container;
        }

        protected override void RefreshUIInternal() {
            if (_row2 == null) return;
            _row2.Clear();
            var attrDropdown = new EnumField("", AttributeType);
            var skillDropdown = new EnumField("", SkillType);

            if (ConditionTargetType == ConditionTargetType.Attribute) {
                attrDropdown.RegisterValueChangedCallback(e => {
                    GraphViewElement.UndoManager.RecordNodeChange(this, "Change required attribute", () => {
                        AttributeType = (AttributeType)e.newValue;
                    });
                });
                attrDropdown.AddToClassList("cnode_attribute-dropdown");
                _row2.Add(attrDropdown);
            }

            if (ConditionTargetType == ConditionTargetType.Skill) {
                skillDropdown.RegisterValueChangedCallback(e => {
                    GraphViewElement.UndoManager.RecordNodeChange(this, "Change required skill", () => {
                        SkillType = (SkillType)e.newValue;
                    });
                });
                skillDropdown.AddToClassList("cnode_skill-dropdown");
                _row2.Add(skillDropdown);
            }

            VisualElement bottomContainer = new();
            bottomContainer.AddToClassList("cnode__bottom-container");

            Label textLabel = new(">=");
            textLabel.style.fontSize = 14;
            textLabel.AddToClassList("cnode__operand_text");

            var inputField = new IntegerField();
            inputField.SetValueWithoutNotify(ExpectedValue);
            inputField.RegisterValueChangedCallback(evt => {
                if (_expectedValueEditScope == null)
                    _expectedValueEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Change condition threshold");

                ExpectedValue = evt.newValue;
            });
            inputField.RegisterCallback<FocusInEvent>(_ => {
                _expectedValueEditScope?.Dispose();
                _expectedValueEditScope = GraphViewElement.UndoManager.BeginNodeEdit(this, "Change condition threshold");
            });
            inputField.RegisterCallback<FocusOutEvent>(_ => {
                _expectedValueEditScope?.Dispose();
                _expectedValueEditScope = null;
            });
            inputField.RegisterCallback<DetachFromPanelEvent>(_ => {
                _expectedValueEditScope?.Dispose();
                _expectedValueEditScope = null;
            });
            inputField.AddToClassList("cnode__input-field");

            bottomContainer.Add(textLabel);
            bottomContainer.Add(inputField);

            _row2.Add(bottomContainer);
        }

        static EdgeConnector<EdgeElement> CreateEdgeConnector() {
            return new EdgeConnector<EdgeElement>(new ConnectorListener());
        }
    }
}
#endif