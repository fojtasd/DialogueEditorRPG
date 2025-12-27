#if UNITY_EDITOR
using DialogueEditor.Data.Save;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public sealed class RelayNode : BaseNode {
        Port _inputPort;
        Port _outputPort;

        public RelayNodeSaveData ConnectionData { get; private set; } = new();

        public override bool ShouldPersistToAssets => false;
        public override bool ShouldTrackName => false;
        public override bool ShouldIgnoreStartingNodeValidation => true;

        public void Draw() {
            title = "Relay";

            titleContainer.Clear();
            inputContainer.Clear();
            outputContainer.Clear();
            extensionContainer.Clear();
            topContainer.Clear();

            titleContainer.style.height = 0;

            BuildInputPort();
            BuildOutputPort();

            topContainer.Add(inputContainer);
            topContainer.Add(outputContainer);

            mainContainer.Add(extensionContainer);

            RefreshPorts();
            RefreshExpandedState();
        }

        public void SetConnectionData(RelayNodeSaveData data) {
            ConnectionData = data ?? new RelayNodeSaveData();
            if (_outputPort != null)
                _outputPort.userData = ConnectionData;
        }

        protected override void RefreshUIInternal() {
            if (_outputPort?.userData is RelayNodeSaveData data)
                ConnectionData = data;
        }

        void BuildInputPort() {
            _inputPort = Port.Create<EdgeElement>(Orientation.Horizontal,
                                                  Direction.Input,
                                                  Port.Capacity.Single,
                                                  typeof(float));
            _inputPort.portName = null;
            _inputPort.AddManipulator(CreateEdgeConnector());
            inputContainer.Add(_inputPort);
        }

        void BuildOutputPort() {
            _outputPort = Port.Create<EdgeElement>(Orientation.Horizontal,
                                                   Direction.Output,
                                                   Port.Capacity.Single,
                                                   typeof(float));
            _outputPort.portName = null;
            _outputPort.userData = ConnectionData;
            _outputPort.AddManipulator(CreateEdgeConnector());
            outputContainer.Add(_outputPort);
        }

        static EdgeConnector<EdgeElement> CreateEdgeConnector() {
            return new EdgeConnector<EdgeElement>(new ConnectorListener());
        }
    }
}
#endif