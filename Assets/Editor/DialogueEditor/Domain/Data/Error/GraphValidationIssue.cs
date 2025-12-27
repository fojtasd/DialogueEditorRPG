#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogueEditor.Data.Error {
    public class GraphValidationIssue {
        readonly List<GraphValidationIssueAction> _actions = new();

        public GraphValidationIssue(string message) {
            Message = message;
        }

        public string Message { get; }

        public ReadOnlyCollection<GraphValidationIssueAction> Actions => _actions.AsReadOnly();

        public void AddAction(string label, Action action) {
            if (string.IsNullOrWhiteSpace(label))
                return;

            var issueAction = new GraphValidationIssueAction(label, action);
            _actions.Add(issueAction);
        }
    }
}
#endif