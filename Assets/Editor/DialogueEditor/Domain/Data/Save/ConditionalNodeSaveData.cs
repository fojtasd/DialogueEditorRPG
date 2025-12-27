using System;
using DialogueSystem.Node;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    [Serializable]
    public class ConditionalNodeSaveData {
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public string NodeID { get; set; }
        [field: SerializeField] public ConditionalOutputType OutputType { get; set; }
    }
}