#if UNITY_EDITOR
using System.Collections.Generic;
using DialogueEditor.Elements;

namespace DialogueEditor.Data.Error {
    public class NodeErrorData {
        public ErrorData ErrorData { get; set; } = new();
        public List<BaseNode> Nodes { get; set; } = new();
    }
}
#endif