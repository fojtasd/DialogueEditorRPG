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
    }
}
#endif

