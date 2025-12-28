#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using DialogueEditor.Data.Save;
using DialogueEditor.Elements;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace DialogueEditor.Domain.Undo {
    interface IGraphUndoAction {
        void Undo();
        void Redo();
        string Description { get; }
    }

    public sealed class GraphUndoManager {
        readonly GraphViewElement _graphView;
        readonly Stack<IGraphUndoAction> _undoStack = new();
        readonly Stack<IGraphUndoAction> _redoStack = new();

        int _recordingSuppressionDepth;
        int _executionDepth;

        public GraphUndoManager(GraphViewElement graphView) {
            _graphView = graphView ?? throw new ArgumentNullException(nameof(graphView));
        }

        public bool CanUndo => _undoStack.Count > 0;
        public bool CanRedo => _redoStack.Count > 0;

        internal bool IsRecordingEnabled => _recordingSuppressionDepth == 0 && _executionDepth == 0;

        public IReadOnlyList<string> GetUndoHistory() {
            return _undoStack.Select(action => action.Description).ToList();
        }

        public IReadOnlyList<string> GetRedoHistory() {
            return _redoStack.Select(action => action.Description).ToList();
        }

        public void RecordNodeChange(BaseNode node, string description, Action mutation) {
            if (mutation == null)
                return;

            if (node == null || !IsRecordingEnabled) {
                mutation();
                return;
            }

            using (new NodeEditScope(this, node, description)) {
                mutation();
            }
        }

        public IDisposable BeginNodeEdit(BaseNode node, string description) {
            if (node == null)
                return Disposable.Empty;

            return new NodeEditScope(this, node, description);
        }

        public IDisposable SuppressRecording() {
            return new RecordingScope(this);
        }

        public void RunWithoutRecording(Action callback) {
            if (callback == null)
                return;

            using (SuppressRecording()) {
                callback();
            }
        }

        public void ClearHistory() {
            _undoStack.Clear();
            _redoStack.Clear();
        }

        public void RecordNodeCreation(BaseNode node) {
            if (node == null)
                return;

            if (!IsRecordingEnabled)
                return;

            var action = new NodeCreationAction(_graphView, node);
            _undoStack.Push(action);
            _redoStack.Clear();
        }

        public void RecordConnectionsCreated(IEnumerable<Edge> edges) {
            if (!IsRecordingEnabled)
                return;

            List<ConnectionActionBase.ConnectionRecord> records = BuildConnectionRecords(edges);

            if (records.Count == 0)
                return;

            PushAndClear(new ConnectionCreationAction(_graphView, records));
        }

        public void RecordConnectionsDeleted(IEnumerable<Edge> edges) {
            if (!IsRecordingEnabled)
                return;

            List<ConnectionActionBase.ConnectionRecord> records = BuildConnectionRecords(edges);

            if (records.Count == 0)
                return;

            PushAndClear(new ConnectionDeletionAction(_graphView, records));
        }

        public void RecordElementsMoved(IEnumerable<GraphElementsMoveRecord> moveRecords) {
            if (!IsRecordingEnabled)
                return;

            List<GraphElementsMoveRecord> records = moveRecords?
                                                    .Where(record => record.HasMovement)
                                                    .ToList();

            if (records == null || records.Count == 0)
                return;

            PushAndClear(new GraphElementsMoveAction(_graphView, records));
        }

        public bool TryHandleNodeDeletion(BaseNode node) {
            if (node == null)
                return false;

            if (!IsRecordingEnabled)
                return false;

            var action = new NodeDeletionAction(_graphView, node);

            using (new ExecutionScope(this))
            using (SuppressRecording()) {
                action.Execute();
            }

            _undoStack.Push(action);
            _redoStack.Clear();

            return true;
        }

        public void Undo() {
            if (!CanUndo)
                return;

            IGraphUndoAction action = _undoStack.Pop();

            using (new ExecutionScope(this))
            using (SuppressRecording()) {
                action.Undo();
            }

            _redoStack.Push(action);
        }

        public void Redo() {
            if (!CanRedo)
                return;

            IGraphUndoAction action = _redoStack.Pop();

            using (new ExecutionScope(this))
            using (SuppressRecording()) {
                action.Redo();
            }

            _undoStack.Push(action);
        }

        List<ConnectionActionBase.ConnectionRecord> BuildConnectionRecords(IEnumerable<Edge> edges) {
            var connectionRecords = new List<ConnectionActionBase.ConnectionRecord>();

            if (edges == null)
                return connectionRecords;

            foreach (Edge edge in edges) {
                if (edge?.output?.node is not BaseNode sourceNode)
                    continue;

                if (edge.input?.node is not BaseNode targetNode)
                    continue;

                if (edge.output.userData == null)
                    continue;

                connectionRecords.Add(new ConnectionActionBase.ConnectionRecord(sourceNode.ID,
                                                                                sourceNode.NodeName,
                                                                                targetNode.ID,
                                                                                targetNode.NodeName,
                                                                                edge.output.userData));
            }

            return connectionRecords;
        }

        void PushAndClear(IGraphUndoAction action) {
            if (action == null)
                return;

            _undoStack.Push(action);
            _redoStack.Clear();
        }

        sealed class RecordingScope : IDisposable {
            readonly GraphUndoManager _manager;
            bool _disposed;

            public RecordingScope(GraphUndoManager manager) {
                _manager = manager;
                _manager._recordingSuppressionDepth++;
            }

            public void Dispose() {
                if (_disposed)
                    return;

                _disposed = true;
                _manager._recordingSuppressionDepth = Math.Max(0, _manager._recordingSuppressionDepth - 1);
            }
        }

        sealed class ExecutionScope : IDisposable {
            readonly GraphUndoManager _manager;
            bool _disposed;

            public ExecutionScope(GraphUndoManager manager) {
                _manager = manager;
                _manager._executionDepth++;
            }

            public void Dispose() {
                if (_disposed)
                    return;

                _disposed = true;
                _manager._executionDepth = Math.Max(0, _manager._executionDepth - 1);
            }
        }

        sealed class NodeCreationAction : IGraphUndoAction {
            readonly GraphViewElement _graphView;
            readonly NodeSaveData _nodeData;
            BaseNode _currentInstance;

            public NodeCreationAction(GraphViewElement graphView, BaseNode node) {
                _graphView = graphView;
                _nodeData = new NodeSaveData(node);
                _currentInstance = node;
            }

            public string Description => $"Create {DescribeNode(_nodeData)}";

            public void Undo() {
                BaseNode node = _graphView.FindNodeById(_nodeData.ID) ?? _currentInstance;
                if (node == null)
                    return;

                _graphView.RemoveNodeInternal(node);
                if (ReferenceEquals(_currentInstance, node))
                    _currentInstance = null;
            }

            public void Redo() {
                BaseNode restored = _graphView.RestoreNode(_nodeData);
                _graphView.RestoreOutgoingConnections(restored, _nodeData);
                _currentInstance = restored;
            }
        }

        sealed class NodeDeletionAction : IGraphUndoAction {
            readonly GraphViewElement _graphView;
            readonly NodeSaveData _nodeData;
            readonly List<ConnectionSnapshot> _incomingConnections;

            public NodeDeletionAction(GraphViewElement graphView, BaseNode node) {
                _graphView = graphView;
                _nodeData = new NodeSaveData(node);
                _incomingConnections = ConnectionSnapshot.Capture(graphView, node);
            }

            public string Description => $"Delete {DescribeNode(_nodeData)}";

            public void Execute() {
                BaseNode node = _graphView.FindNodeById(_nodeData.ID);
                if (node == null)
                    return;

                _graphView.RemoveNodeInternal(node);
            }

            public void Undo() {
                BaseNode restored = _graphView.RestoreNode(_nodeData);
                if (restored == null)
                    return;

                foreach (ConnectionSnapshot snapshot in _incomingConnections)
                    snapshot.Restore(_graphView, restored);

                _graphView.RestoreOutgoingConnections(restored, _nodeData);
            }

            public void Redo() {
                Execute();
            }
        }

        sealed class NodeModificationAction : IGraphUndoAction {
            readonly GraphViewElement _graphView;
            readonly NodeSaveData _before;
            readonly NodeSaveData _after;
            readonly List<ConnectionSnapshot> _incomingConnections;

            public NodeModificationAction(GraphViewElement graphView,
                                          NodeSaveData before,
                                          NodeSaveData after,
                                          List<ConnectionSnapshot> incomingConnections,
                                          string description) {
                _graphView = graphView;
                _before = before;
                _after = after;
                _incomingConnections = incomingConnections ?? new List<ConnectionSnapshot>();
                Description = description;
            }

            public string Description { get; }

            public void Undo() {
                ApplySnapshot(_before);
            }

            public void Redo() {
                ApplySnapshot(_after);
            }

            void ApplySnapshot(NodeSaveData data) {
                if (data == null)
                    return;

                BaseNode existing = _graphView.FindNodeById(data.ID);
                if (existing != null)
                    _graphView.RemoveNodeInternal(existing);

                BaseNode restored = _graphView.RestoreNode(data);
                if (restored == null)
                    return;

                foreach (ConnectionSnapshot snapshot in _incomingConnections)
                    snapshot.Restore(_graphView, restored);

                _graphView.RestoreOutgoingConnections(restored, data);
                restored.RefreshUI();
            }
        }

        sealed class NodeEditScope : IDisposable {
            readonly GraphUndoManager _manager;
            readonly BaseNode _node;
            readonly string _description;
            readonly bool _shouldRecord;
            readonly NodeSaveData _before;
            readonly string _beforeJson;
            readonly List<ConnectionSnapshot> _incomingConnections;
            bool _disposed;

            public NodeEditScope(GraphUndoManager manager, BaseNode node, string description) {
                _manager = manager;
                _node = node;
                _description = description;
                _shouldRecord = manager.IsRecordingEnabled && node != null;

                if (!_shouldRecord)
                    return;

                _incomingConnections = ConnectionSnapshot.Capture(manager._graphView, node);
                _before = new NodeSaveData(node);
                _beforeJson = JsonUtility.ToJson(_before);
            }

            public void Dispose() {
                if (_disposed)
                    return;

                _disposed = true;

                if (!_shouldRecord)
                    return;

                var after = new NodeSaveData(_node);
                string afterJson = JsonUtility.ToJson(after);

                if (string.Equals(_beforeJson, afterJson, StringComparison.Ordinal))
                    return;

                string label = string.IsNullOrWhiteSpace(_description)
                    ? $"Edit {DescribeNode(after)}"
                    : _description;

                var action = new NodeModificationAction(_manager._graphView,
                                                        _before,
                                                        after,
                                                        _incomingConnections,
                                                        label);
                _manager._undoStack.Push(action);
                _manager._redoStack.Clear();
            }
        }

        static class Disposable {
            public static readonly IDisposable Empty = new EmptyDisposable();

            sealed class EmptyDisposable : IDisposable {
                public void Dispose() { }
            }
        }

        sealed class ConnectionSnapshot {
            readonly string _sourceNodeId;
            readonly object _portUserData;
            readonly string _targetNodeId;

            ConnectionSnapshot(string sourceNodeId, object portUserData, string targetNodeId) {
                _sourceNodeId = sourceNodeId;
                _portUserData = portUserData;
                _targetNodeId = targetNodeId;
            }

            public static List<ConnectionSnapshot> Capture(GraphViewElement graphView, BaseNode node) {
                var snapshots = new List<ConnectionSnapshot>();
                if (graphView == null || node == null)
                    return snapshots;

                foreach (Port inputPort in graphView.GetInputPorts(node)) {
                    foreach (Edge connection in inputPort.connections.ToList()) {
                        if (connection.output?.node is not BaseNode sourceNode)
                            continue;

                        object userData = connection.output.userData;
                        if (userData == null)
                            continue;

                        snapshots.Add(new ConnectionSnapshot(sourceNode.ID, userData, node.ID));
                    }
                }

                return snapshots;
            }

            public void Restore(GraphViewElement graphView, BaseNode restoredTarget) {
                if (graphView == null || restoredTarget == null)
                    return;

                BaseNode sourceNode = graphView.FindNodeById(_sourceNodeId);
                if (sourceNode == null)
                    return;

                Port outputPort = graphView.FindOutputPort(sourceNode, _portUserData);
                if (outputPort == null)
                    return;

                Port inputPort = graphView.GetPrimaryInputPort(restoredTarget);
                if (inputPort == null)
                    return;

                switch (_portUserData) {
                    case DialogueNodeSaveData dialogueChoice:
                        dialogueChoice.NodeID = restoredTarget.ID;
                        break;
                    case ConditionalNodeSaveData conditionalData:
                        conditionalData.NodeID = restoredTarget.ID;
                        break;
                    case RelayNodeSaveData relayData:
                        relayData.NodeID = restoredTarget.ID;
                        break;
                }

                graphView.ConnectPorts(outputPort, inputPort);
            }
        }

        static string DescribeNode(NodeSaveData data) {
            if (data == null)
                return "node";

            string typeLabel = data.NodeType switch {
                GraphNodeType.Dialogue => "dialogue",
                GraphNodeType.Conditional => "conditional",
                GraphNodeType.Relay => "relay",
                _ => "node"
            };

            string name = string.IsNullOrWhiteSpace(data.NodeName) ? "(unnamed)" : data.NodeName;
            return $"{typeLabel} \"{name}\"";
        }

        abstract class ConnectionActionBase : IGraphUndoAction {
            protected readonly GraphViewElement GraphView;
            readonly List<ConnectionRecord> _connections;

            protected ConnectionActionBase(GraphViewElement graphView,
                                           IEnumerable<ConnectionRecord> connections,
                                           string singleDescription,
                                           string pluralDescription) {
                GraphView = graphView;
                _connections = connections?.ToList() ?? new List<ConnectionRecord>();

                Description = _connections.Count switch {
                    0 => pluralDescription,
                    1 => $"{singleDescription} {_connections[0].GetLabel()}",
                    _ => $"{pluralDescription} ({_connections.Count})"
                };
            }

            protected IReadOnlyList<ConnectionRecord> Connections => _connections;

            public string Description { get; }

            public abstract void Undo();
            public abstract void Redo();

            protected bool Disconnect(ConnectionRecord record) {
                BaseNode sourceNode = GraphView.FindNodeById(record.SourceNodeId);
                BaseNode targetNode = GraphView.FindNodeById(record.TargetNodeId);

                if (sourceNode == null)
                    return false;

                Port outputPort = GraphView.FindOutputPort(sourceNode, record.PortUserData);

                if (outputPort == null)
                    return false;

                bool disconnected = false;

                foreach (Edge connection in outputPort.connections.ToList()) {
                    if (connection?.input?.node is not BaseNode inputNode)
                        continue;

                    if (!string.Equals(inputNode.ID, record.TargetNodeId, StringComparison.Ordinal))
                        continue;

                    outputPort.Disconnect(connection);
                    connection.input?.Disconnect(connection);
                    GraphView.RemoveElement(connection);
                    disconnected = true;
                }

                if (!disconnected)
                    return false;

                ClearConnectionMetadata(record.PortUserData);
                RefreshNode(sourceNode);
                if (targetNode != null)
                    RefreshNode(targetNode);

                return true;
            }

            protected bool Connect(ConnectionRecord record) {
                BaseNode sourceNode = GraphView.FindNodeById(record.SourceNodeId);
                BaseNode targetNode = GraphView.FindNodeById(record.TargetNodeId);

                if (sourceNode == null || targetNode == null)
                    return false;

                Port outputPort = GraphView.FindOutputPort(sourceNode, record.PortUserData);
                Port inputPort = GraphView.GetPrimaryInputPort(targetNode);

                if (outputPort == null || inputPort == null)
                    return false;

                if (outputPort.connections.Any(edge => edge != null && edge.input == inputPort))
                    return false;

                EdgeElement edge = GraphView.ConnectPorts(outputPort, inputPort);

                if (edge == null)
                    return false;

                ApplyConnectionMetadata(record.PortUserData, targetNode.ID);
                edge.ApplyCustomStyle();
                RefreshNode(sourceNode);
                RefreshNode(targetNode);

                return true;
            }

            protected static void RefreshNode(BaseNode node) {
                if (node == null)
                    return;

                node.schedule.Execute(node.RefreshUI).ExecuteLater(50);
                node.RefreshUI();
            }

            protected static void ClearConnectionMetadata(object portUserData) {
                switch (portUserData) {
                    case DialogueNodeSaveData dialogueData:
                        dialogueData.NodeID = null;
                        break;
                    case ConditionalNodeSaveData conditionalData:
                        conditionalData.NodeID = null;
                        break;
                    case RelayNodeSaveData relayData:
                        relayData.NodeID = null;
                        break;
                }
            }

            protected static void ApplyConnectionMetadata(object portUserData, string targetNodeId) {
                switch (portUserData) {
                    case DialogueNodeSaveData dialogueData:
                        dialogueData.NodeID = targetNodeId;
                        break;
                    case ConditionalNodeSaveData conditionalData:
                        conditionalData.NodeID = targetNodeId;
                        break;
                    case RelayNodeSaveData relayData:
                        relayData.NodeID = targetNodeId;
                        break;
                }
            }

            public sealed class ConnectionRecord {
                public ConnectionRecord(string sourceNodeId,
                                        string sourceName,
                                        string targetNodeId,
                                        string targetName,
                                        object portUserData) {
                    SourceNodeId = sourceNodeId;
                    SourceName = sourceName;
                    TargetNodeId = targetNodeId;
                    TargetName = targetName;
                    PortUserData = portUserData;
                }

                public string SourceNodeId { get; }
                public string SourceName { get; }
                public string TargetNodeId { get; }
                public string TargetName { get; }
                public object PortUserData { get; }

                public string GetLabel() {
                    string source = string.IsNullOrWhiteSpace(SourceName) ? "node" : $"\"{SourceName}\"";
                    string target = string.IsNullOrWhiteSpace(TargetName) ? "node" : $"\"{TargetName}\"";
                    return $"{source} → {target}";
                }
            }
        }

        sealed class ConnectionCreationAction : ConnectionActionBase {
            public ConnectionCreationAction(GraphViewElement graphView, IEnumerable<ConnectionRecord> connections)
                : base(graphView, connections, "Connect", "Connect links") { }

            public override void Undo() {
                bool changed = false;

                foreach (ConnectionRecord record in Connections) {
                    if (Disconnect(record))
                        changed = true;
                }

                if (changed)
                    GraphView.NotifyGraphChanged();
            }

            public override void Redo() {
                bool changed = false;

                foreach (ConnectionRecord record in Connections) {
                    if (Connect(record))
                        changed = true;
                }

                if (changed)
                    GraphView.NotifyGraphChanged();
            }
        }

        sealed class ConnectionDeletionAction : ConnectionActionBase {
            public ConnectionDeletionAction(GraphViewElement graphView, IEnumerable<ConnectionRecord> connections)
                : base(graphView, connections, "Disconnect", "Disconnect links") { }

            public override void Undo() {
                bool changed = false;

                foreach (ConnectionRecord record in Connections) {
                    if (Connect(record))
                        changed = true;
                }

                if (changed)
                    GraphView.NotifyGraphChanged();
            }

            public override void Redo() {
                bool changed = false;

                foreach (ConnectionRecord record in Connections) {
                    if (Disconnect(record))
                        changed = true;
                }

                if (changed)
                    GraphView.NotifyGraphChanged();
            }
        }

        sealed class GraphElementsMoveAction : IGraphUndoAction {
            readonly GraphViewElement _graphView;
            readonly List<GraphElementsMoveRecord> _records;

            public GraphElementsMoveAction(GraphViewElement graphView, IEnumerable<GraphElementsMoveRecord> records) {
                _graphView = graphView;
                _records = records?.ToList() ?? new List<GraphElementsMoveRecord>();

                Description = _records.Count switch {
                    0 => "Move elements",
                    1 => $"Move {_records[0].GetLabel()}",
                    _ => $"Move {_records.Count} elements"
                };
            }

            public string Description { get; }

            public void Undo() {
                Apply(record => record.OldPosition);
            }

            public void Redo() {
                Apply(record => record.NewPosition);
            }

            void Apply(Func<GraphElementsMoveRecord, Rect> selector) {
                bool changed = false;

                foreach (GraphElementsMoveRecord record in _records) {
                    if (!record.TryResolve(_graphView, out GraphElement element))
                        continue;

                    Rect targetRect = selector(record);

                    switch (element) {
                        case BaseNode node:
                            node.SetPosition(targetRect);
                            node.RefreshUI();
                            changed = true;
                            break;
                        case GroupElement group:
                            group.SetPosition(targetRect);
                            changed = true;
                            break;
                    }
                }

                if (changed)
                    _graphView.NotifyGraphChanged();
            }
        }

        public readonly struct GraphElementsMoveRecord {
            public GraphElementsMoveRecord(string nodeId,
                                           string nodeName,
                                           string groupId,
                                           string groupName,
                                           Rect oldPosition,
                                           Rect newPosition) {
                NodeId = nodeId;
                NodeName = nodeName;
                GroupId = groupId;
                GroupName = groupName;
                OldPosition = oldPosition;
                NewPosition = newPosition;
            }

            public string NodeId { get; }
            public string NodeName { get; }
            public string GroupId { get; }
            public string GroupName { get; }
            public Rect OldPosition { get; }
            public Rect NewPosition { get; }

            public bool HasMovement => !Approximately(OldPosition.position, NewPosition.position);

            public string GetLabel() {
                if (!string.IsNullOrEmpty(NodeId))
                    return string.IsNullOrWhiteSpace(NodeName) ? "node" : $"node \"{NodeName}\"";

                if (!string.IsNullOrEmpty(GroupId))
                    return string.IsNullOrWhiteSpace(GroupName) ? "group" : $"group \"{GroupName}\"";

                return "element";
            }

            public bool TryResolve(GraphViewElement graphView, out GraphElement element) {
                element = null;

                if (!string.IsNullOrEmpty(NodeId)) {
                    element = graphView.FindNodeById(NodeId);
                    return element != null;
                }

                if (!string.IsNullOrEmpty(GroupId)) {
                    element = graphView.FindGroupById(GroupId);
                    return element != null;
                }

                return false;
            }

            public static GraphElementsMoveRecord ForNode(BaseNode node, Rect oldPosition, Rect newPosition) {
                if (node == null)
                    return default;

                return new GraphElementsMoveRecord(node.ID,
                                                   node.NodeName,
                                                   null,
                                                   null,
                                                   oldPosition,
                                                   newPosition);
            }

            public static GraphElementsMoveRecord ForGroup(GroupElement group, Rect oldPosition, Rect newPosition) {
                if (group == null)
                    return default;

                return new GraphElementsMoveRecord(null,
                                                   null,
                                                   group.ID,
                                                   group.title,
                                                   oldPosition,
                                                   newPosition);
            }

            static bool Approximately(Vector2 a, Vector2 b) {
                return Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
            }
        }
    }
}
#endif

