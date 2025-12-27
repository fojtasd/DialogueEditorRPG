using System;

namespace DialogueSystem.Node {
    [Serializable]
    public enum ConsequenceKind {
        GiveMoneyToPlayer = 0,
        GiveItemToPlayer = 1,
        GiveQuestToPlayer = 2,
        GiveIntelToPlayer = 3,
        InvokeEmotion = 4,
        InvokeHostility = 5,
        StartTrade = 6
    }
}