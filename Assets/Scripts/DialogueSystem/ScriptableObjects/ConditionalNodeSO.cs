using DialogueSystem.Node;
using UnityEngine;

namespace DialogueSystem {
    public class ConditionalNodeSO : NodeBaseSO {
        /// <summary>
        ///     This Node is followed in the case of successful conditional check.
        /// </summary>
        [field: SerializeField]
        public NodeBaseSO NextDialogueSuccess { get; set; }

        /// <summary>
        ///     This Node is followed in the case of failed conditional check.
        /// </summary>
        [field: SerializeField]
        public NodeBaseSO NextDialogueFailure { get; set; }
        
        [field: SerializeField] public ConditionalNodeData Conditionals{ get; set; }

        bool EvaluateCondition(int input) {
            // TODO implement evaluation
            return false;
        }

        public void Initialize
        (
            string dialogueID,
            string dialogueGroup,
            string dialogueName,
            ChoiceType choiceType,
            bool isStartingNode,
            ConditionalNodeData conditionals
        ) {
            DialogueID = dialogueID;
            DialogueName = dialogueName;
            DialogueType = choiceType;
            DialogueGroup = dialogueGroup;
            IsStartingNode = isStartingNode;

            if (conditionals == null)
                return;

            Conditionals = conditionals;
        }
    }
}