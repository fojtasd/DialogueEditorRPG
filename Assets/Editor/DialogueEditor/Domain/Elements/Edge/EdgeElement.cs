#if UNITY_EDITOR
using DialogueEditor.Data.Save;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueEditor.Elements {
    public class EdgeElement : Edge {
        static readonly Color[] EdgeColors = { new(0.9f, 0.1f, 0.1f), new(1.0f, 0.5f, 0.0f), new(1.0f, 0.84f, 0.0f), new(0.2f, 0.8f, 0.2f), new(0.1f, 0.5f, 1.0f), new(0.5f, 0.1f, 1.0f), new(1f, 1f, 1f) };

        static int _colorIndex;
        readonly Color _assignedColor;

        public EdgeElement() {
            _assignedColor = EdgeColors[_colorIndex % EdgeColors.Length];
            _colorIndex++;
        }

        public override bool UpdateEdgeControl() {
            bool result = base.UpdateEdgeControl();

            ApplyCustomStyle();

            return result;
        }

        public void ApplyCustomStyle() {
            if (edgeControl == null) return;

            edgeControl.inputColor = _assignedColor;
            edgeControl.outputColor = _assignedColor;

            edgeControl.MarkDirtyRepaint();
        }

        public Color GetAssignedColor() {
            return _assignedColor;
        }

        public static void HandleEdgeChange(GraphViewChange change) {
            if (change.edgesToCreate != null) {
                foreach (Edge edge1 in change.edgesToCreate) {
                    if (edge1.output?.node is not BaseNode fromNode || edge1.input?.node is not BaseNode toNode) {
                        continue;
                    }

                    switch (edge1.output.userData) {
                        case DialogueNodeSaveData choiceData:
                            choiceData.NodeID = toNode.ID;
                            break;
                        case ConditionalNodeSaveData conditionalData: {
                            conditionalData.NodeID = toNode.ID;

                            break;
                        }
                        case RelayNodeSaveData relayData:
                            relayData.NodeID = toNode.ID;
                            break;
                    }

                    fromNode.schedule.Execute(fromNode.RefreshUI).ExecuteLater(50);
                    fromNode.RefreshUI();

                    if (edge1 is EdgeElement dsEdge) {
                        dsEdge.ApplyCustomStyle();
                    }

                    toNode.schedule.Execute(() => { toNode.RefreshUI(); }).ExecuteLater(50);
                }
            }

            if (change.elementsToRemove == null) return;
            {
                foreach (GraphElement element in change.elementsToRemove)
                    if (element is EdgeElement edge) {
                        if (edge.output is { userData: DialogueNodeSaveData choiceData, node: DialogueNode dFromNode }) {
                            choiceData.NodeID = null;
                            dFromNode.RefreshUI();
                        }

                        if (edge.output is { userData: ConditionalNodeSaveData conditionSaveData, node: ConditionalNode cFromNode }) {
                            conditionSaveData.NodeID = null;
                            cFromNode.RefreshUI();
                        }

                        if (edge.output is { userData: RelayNodeSaveData relayData, node: RelayNode relayNode }) {
                            relayData.NodeID = null;
                            relayNode.RefreshUI();
                        }

                        if (edge.input.node is DialogueNode targetNode)
                            targetNode.schedule.Execute(() => { targetNode.RefreshUI(); }).ExecuteLater(50);
                    }
            }
        }
    }
}
#endif