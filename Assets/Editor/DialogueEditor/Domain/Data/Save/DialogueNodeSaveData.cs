using System;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    [Serializable]
    public class DialogueNodeSaveData {
        [field: SerializeField] public string Text { get; set; } = "New Choice";
        [field: SerializeField] public string NodeID { get; set; }
    }
}