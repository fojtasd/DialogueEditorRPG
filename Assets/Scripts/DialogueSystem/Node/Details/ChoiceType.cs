using System;

namespace DialogueSystem.Node {
    [Serializable]
    public enum ChoiceType {
        SingleChoice = 0,
        MultipleChoice = 1,
        Conditional = 2
    }
}