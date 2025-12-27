#if UNITY_EDITOR
using System;

namespace DialogueEditor.Data.Error {
    public class GraphValidationIssueAction {
        readonly Action _handler;

        public GraphValidationIssueAction(string label, Action handler) {
            Label = label;
            _handler = handler;
        }

        public string Label { get; }

        public bool CanExecute => _handler != null;

        public void Invoke() {
            _handler?.Invoke();
        }
    }
}
#endif

