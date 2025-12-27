#if UNITY_EDITOR
using System.Collections.Generic;
using DialogueEditor.Elements;

namespace DialogueEditor.Data.Error {
    public class GroupErrorData {
        public ErrorData ErrorData { get; } = new();
        public List<GroupElement> Groups { get; } = new();
    }
}
#endif