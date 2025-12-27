using System;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    [Serializable]
    public class RelayNodeSaveData {
        [field: SerializeField] public string NodeID { get; set; }
    }
}