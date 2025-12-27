using System.Collections.Generic;
using Common;
using UnityEngine;

namespace DialogueSystem {
    public class DialogueContainerSO : ScriptableObject {
        [field: SerializeField] public string FileName { get; set; }
        [field: SerializeField] public NodeBaseSO StartingNode { get; set; }
        [field: SerializeField] public SerializableDictionary<GroupSO, List<NodeBaseSO>> DialogueGroups { get; set; }
        [field: SerializeField] public List<NodeBaseSO> UngroupedDialogues { get; set; }
        
        public void Initialize(string fileName) {
            FileName = fileName;

            DialogueGroups = new SerializableDictionary<GroupSO, List<NodeBaseSO>>();
            UngroupedDialogues = new List<NodeBaseSO>();
        }
    }
}