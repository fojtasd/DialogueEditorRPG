using System;
using UnityEngine;

namespace DialogueSystem.Node {
    [Serializable]
    public abstract class ConsequencePayload {
        public abstract ConsequenceKind Kind { get; }
    }

    [Serializable]
    public sealed class GiveMoneyConsequencePayload : ConsequencePayload {
        [Min(0)] public float amount;

        public override ConsequenceKind Kind => ConsequenceKind.GiveMoneyToPlayer;
    }

    [Serializable]
    public sealed class GiveItemConsequencePayload : ConsequencePayload {
        [Min(1)] public float quantity = 1;

        public string itemName;
        public override ConsequenceKind Kind => ConsequenceKind.GiveItemToPlayer;
    }

    [Serializable]
    public sealed class GiveQuestConsequencePayload : ConsequencePayload {
        public string questReference;
        public override ConsequenceKind Kind => ConsequenceKind.GiveQuestToPlayer;
    }

    [Serializable]
    public sealed class GiveIntelConsequencePayload : ConsequencePayload {
        public string intelReference;
        public override ConsequenceKind Kind => ConsequenceKind.GiveIntelToPlayer;
    }

    [Serializable]
    public sealed class InvokeEmotionPayload : ConsequencePayload {
        public string emotion;

        public override ConsequenceKind Kind => ConsequenceKind.InvokeEmotion;
    }

    [Serializable]
    public sealed class InvokeHostilityConsequencePayload : ConsequencePayload {
        public override ConsequenceKind Kind => ConsequenceKind.InvokeHostility;
    }

    [Serializable]
    public sealed class StartTradeConsequencePayload : ConsequencePayload {
        public override ConsequenceKind Kind => ConsequenceKind.StartTrade;
    }
}