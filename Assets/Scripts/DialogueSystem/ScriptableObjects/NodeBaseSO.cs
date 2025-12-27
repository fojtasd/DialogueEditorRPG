using DialogueSystem.Node;
using UnityEngine;

namespace DialogueSystem {
    public abstract class NodeBaseSO : ScriptableObject {
        [Header("Dialogue Information")]
        [field: SerializeField] public string DialogueID { get; set; }
        [field: SerializeField] public string DialogueGroup { get; set; }
        [field: SerializeField] public string DialogueName { get; set; }

        [Header("Dialogue Type & Flow")]
        [field: SerializeField] public ChoiceType DialogueType { get; set; }
        [field: SerializeField] public bool IsStartingNode { get; set; }
    }
}