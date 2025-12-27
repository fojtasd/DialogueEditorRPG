#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using Contracts.Contracts;
using Contracts.Dialogues;
using DialogueEditor.Elements;
using DialogueEditor.Utilities;
using DialogueSystem.Node;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    public enum GraphNodeType {
        Dialogue,
        Conditional,
        Relay
    }

    [Serializable]
    public class NodeSaveData {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string NodeName { get; set; }
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public SpeakerType SpeakerType { get; set; }
        [field: SerializeField] public List<DialogueNodeSaveData> Choices { get; set; }
        [field: SerializeField] public ConditionalsWrapper Conditionals { get; set; }
        [field: SerializeField] public string GroupID { get; set; }
        [field: SerializeField] public ChoiceType ChoiceType { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
        [field: SerializeReference] public List<INodeSetting> NodeSettings { get; set; }
        [field: SerializeField] public GraphNodeType NodeType { get; set; }
        [field: SerializeField] public RelayNodeSaveData RelayConnection { get; set; }

        public NodeSaveData(BaseNode node) {
            List<DialogueNodeSaveData> choices = new();
            ConditionalsWrapper conditionals = new();
            RelayNodeSaveData relayConnection = null;
            NodeType = GraphNodeType.Dialogue;

            switch (node) {
                case DialogueNode dNode:
                    choices = IOUtility.CloneNodeChoices(dNode.Choices);
                    break;
                case ConditionalNode cNode:
                    NodeType = GraphNodeType.Conditional;
                    conditionals.failure = cNode.FailureNodeData;
                    conditionals.success = cNode.SuccessNodeData;
                    conditionals.expectedValue = cNode.ExpectedValue;
                    conditionals.attributeType = cNode.AttributeType;
                    conditionals.skillType = cNode.SkillType;
                    conditionals.kind = cNode.ConditionTargetType;

                    break;
                case RelayNode relayNode:
                    NodeType = GraphNodeType.Relay;
                    relayConnection = new RelayNodeSaveData { NodeID = relayNode.ConnectionData?.NodeID };
                    break;
            }

            ID = node.ID;
            NodeName = node.NodeName;
            GroupID = node.GroupElement?.ID;
            Choices = choices;
            Conditionals = conditionals;
            Position = node.GetPosition().position;
            ChoiceType = node.ChoiceType;
            RelayConnection = relayConnection;

            if (node is not DialogueNode dialogueNode)
                return;
            if (dialogueNode.Model == null)
                return;
            SpeakerType = dialogueNode.Model.SpeakerType;

            Text = dialogueNode.Model.Text;
            NodeSettings = dialogueNode.Model.NodeSettings?.Select(s => s.DeepClone()).ToList();
        }

        [Serializable]
        public class ConditionalsWrapper {
            public ConditionalNodeSaveData failure;
            public ConditionalNodeSaveData success;

            public int expectedValue;
            public ConditionTargetType kind;
            public AttributeType attributeType;
            public SkillType skillType;
        }
    }
}
#endif