#if UNITY_EDITOR
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogueEditor.Data.Error {
    public class GraphValidationSummary {
        readonly List<GraphValidationIssue> _issues = new();

        public bool HasErrors => _issues.Count > 0;

        public ReadOnlyCollection<GraphValidationIssue> Issues => _issues.AsReadOnly();

        public GraphValidationIssue AddIssue(string message) {
            if (string.IsNullOrWhiteSpace(message))
                return null;

            var issue = new GraphValidationIssue(message);
            _issues.Add(issue);
            return issue;
        }

        public IEnumerable<string> BuildMessages() {
            foreach (GraphValidationIssue issue in _issues)
                yield return issue.Message;
        }
    }
}
#endif

