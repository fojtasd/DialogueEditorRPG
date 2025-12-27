using System.Collections.Generic;
using DialogueSystem.Node;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public class NodeIndicator : VisualElement {
        readonly IntegerField _countField;
        readonly List<INodeSetting> _nodeSettings;

        public NodeIndicator(List<INodeSetting> nodeSettings) {
            _countField = new IntegerField { value = nodeSettings.Count, isReadOnly = true };
            _countField.tooltip = Constants.NodeIndicatorTooltip;
            _countField.SetEnabled(false);
            
            _nodeSettings = nodeSettings;
            
            Add(_countField);
            UpdateStyles(nodeSettings.Count > 0);
        }

        void UpdateStyles(bool isHighlighted) {
            AddToClassList("node-indicators--container");
            ApplyFlagStyle(isHighlighted);
            _countField.AddToClassList("node-indicators--integer-fields");
        }

        void ApplyFlagStyle(bool isHighlighted) {
            _countField.EnableInClassList("node-indicators--integer-fields_highlight_on", isHighlighted);
            _countField.EnableInClassList("node-indicators--integer-fields_highlight_off", !isHighlighted);
        }

        public void RefreshUI(bool isHighlighted) {
            ApplyFlagStyle(isHighlighted);
            _countField.value = _nodeSettings.Count;
        }
    }
}