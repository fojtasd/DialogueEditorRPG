using System.Collections.Generic;
using System.Linq;
using Contracts.Dialogues.Nodes;
using DialogueSystem.Node;
using UnityEngine;

namespace DialogueSystem {
    public class NodeSO : NodeBaseSO {
        [field: SerializeField] public Model Model { get; set; }
        [field: SerializeField] public List<DialogueChoiceData> Choices { get; set; }
        
        public void Initialize
        (
            string dialogueID,
            string dialogueName,
            string dialogueGroup,
            ChoiceType choiceType,
            bool isStartingNode,
            Model model
        ) {
            DialogueID = dialogueID;
            DialogueName = dialogueName;
            DialogueType = choiceType;
            DialogueGroup = dialogueGroup;
            IsStartingNode = isStartingNode;

            if (model is null) return;
            Model = model;
        }
    }
}