#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DialogueEditor.Data.Save;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;


namespace DialogueEditor.Elements {
    public class ConnectorListener : IEdgeConnectorListener {
        public void OnDropOutsidePort(Edge edge, Vector2 position) {
            if (edge?.output is not { } outputPort)
                return;

            if (outputPort.node is not BaseNode originatingNode)
                return;

            GraphViewElement graphView = originatingNode.GraphViewElement;

            if (graphView == null)
                return;

            Vector2 graphPosition = ConvertToGraphPosition(graphView, position);
            GroupElement dropGroup = FindGroupAt(graphView, graphPosition);
            Vector2 dialoguePosition = dropGroup != null ? graphPosition : GetDialogueSpawnPosition(originatingNode, graphPosition);
            Vector2 conditionalPosition = dropGroup != null ? graphPosition : GetConditionalSpawnPosition(originatingNode, graphPosition);
            Vector2 relayPosition = dropGroup != null ? graphPosition : GetRelaySpawnPosition(originatingNode, graphPosition);

            DisconnectTemporaryEdge(edge);

            GenericMenu menu = new();
            bool originatingIsRelay = originatingNode is RelayNode;

            menu.AddItem(new GUIContent("💬 Create Dialogue Node"),
                         false,
                         () => CreateNodeAndConnect(graphView,
                                                    outputPort,
                                                    dialoguePosition,
                                                    pos => graphView.CreateDialogueNode(pos),
                                                    dropGroup));

            menu.AddItem(new GUIContent("❓Create Conditional Node"),
                         false,
                         () => CreateNodeAndConnect(graphView,
                                                    outputPort,
                                                    conditionalPosition,
                                                    pos => graphView.CreateConditionalNode(pos),
                                                    dropGroup));

            if (originatingIsRelay) {
                menu.AddDisabledItem(new GUIContent("🔀 Create Relay Node"));
            } else {
                menu.AddItem(new GUIContent("🔀 Create Relay Node"),
                             false,
                             () => CreateNodeAndConnect(graphView,
                                                        outputPort,
                                                        relayPosition,
                                                        pos => graphView.CreateRelayNode(pos),
                                                        dropGroup));
            }

            menu.ShowAsContext();
        }

        public void OnDrop(GraphView graphView, Edge edge) {
            if (edge.output?.node is not DialogueNode || edge.input?.node is not DialogueNode toNode) {
                return;
            }

            if (edge.output.userData is not DialogueNodeSaveData choiceData)
                return;
            choiceData.NodeID = toNode.ID;
        }

        static void CreateNodeAndConnect(GraphViewElement graphView,
                                         Port originPort,
                                         Vector2 position,
                                         Func<Vector2, BaseNode> factory,
                                         GroupElement targetGroup,
                                         bool frameSelection = false) {
            if (graphView == null || originPort == null || factory == null)
                return;

            BaseNode newNode = factory(position);

            if (newNode == null)
                return;

            graphView.AddElement(newNode);
            if (targetGroup != null) {
                targetGroup.AddElement(newNode);
            }
            graphView.ClearSelection();
            graphView.AddToSelection(newNode);

            var inputPort = newNode.inputContainer.Q<Port>();

            if (inputPort == null)
                return;

            List<Edge> disconnectedEdges = graphView.DisconnectPortConnections(originPort);
            graphView.UndoManager?.RecordConnectionsDeleted(disconnectedEdges);

            var newEdge = new EdgeElement { output = originPort, input = inputPort };

            newEdge.output.Connect(newEdge);
            newEdge.input.Connect(newEdge);

            graphView.AddElement(newEdge);
            UpdateConnectionMetadata(originPort, newNode, newEdge);
            graphView.UndoManager?.RecordConnectionsCreated(new[] { newEdge });
            if (frameSelection)
                graphView.FrameSelection();
        }

        static Vector2 ConvertToGraphPosition(GraphViewElement graphView, Vector2 cursorPosition) {
            if (graphView == null)
                return cursorPosition;

            return graphView.contentViewContainer.WorldToLocal(cursorPosition);
        }

        static Vector2 GetDialogueSpawnPosition(BaseNode originNode, Vector2 fallback) {
            return GetSpawnPosition(originNode, fallback, 0, 0f);
        }

        static Vector2 GetConditionalSpawnPosition(BaseNode originNode, Vector2 fallback) {
            return GetSpawnPosition(originNode, fallback, 0, 0f);
        }

        static Vector2 GetRelaySpawnPosition(BaseNode originNode, Vector2 fallback) {
            return GetSpawnPosition(originNode, fallback, 0, 0f);
        }

        static Vector2 GetSpawnPosition(BaseNode originNode,
                                        Vector2 fallback,
                                        float minHorizontalOffset,
                                        float verticalOffset) {
            if (originNode == null)
                return fallback;

            Rect nodeRect = originNode.GetPosition();
            Vector2 spawnPosition = fallback;

            if (float.IsNaN(spawnPosition.x) || float.IsInfinity(spawnPosition.x))
                spawnPosition.x = nodeRect.xMax + Mathf.Max(minHorizontalOffset, nodeRect.width * 0.6f);

            if (float.IsNaN(spawnPosition.y) || float.IsInfinity(spawnPosition.y))
                spawnPosition.y = nodeRect.yMin + verticalOffset;

            return spawnPosition;
        }

        static void DisconnectTemporaryEdge(Edge edge) {
            if (edge == null)
                return;

            edge.output?.Disconnect(edge);
            edge.input?.Disconnect(edge);

            if (edge.parent != null)
                edge.RemoveFromHierarchy();
        }

        static void UpdateConnectionMetadata(Port originPort, BaseNode newNode, EdgeElement newEdge) {
            if (originPort?.userData is DialogueNodeSaveData dialogueData) {
                dialogueData.NodeID = newNode.ID;
            }

            if (originPort?.userData is ConditionalNodeSaveData conditionalData) {
                conditionalData.NodeID = newNode.ID;
            }

            if (originPort?.userData is RelayNodeSaveData relayData) {
                relayData.NodeID = newNode.ID;
            }

            if (originPort.node is BaseNode fromNode) {
                fromNode.schedule.Execute(fromNode.RefreshUI).ExecuteLater(50);
                fromNode.RefreshUI();
            }

            newNode.schedule.Execute(newNode.RefreshUI).ExecuteLater(50);

            newEdge?.ApplyCustomStyle();
        }

        static GroupElement FindGroupAt(GraphViewElement graphView, Vector2 localDropPosition) {
            if (graphView == null)
                return null;

            List<GroupElement> groups = graphView.Query<GroupElement>().ToList();

            foreach (GroupElement group in groups) {
                Rect groupRect = group.GetPosition();

                if (groupRect.Contains(localDropPosition))
                    return group;
            }

            return null;
        }
    }
}
#endif