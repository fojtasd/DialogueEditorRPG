using System;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DialogueSystem.Node {
    [Serializable]
    public class ConsequenceSetting : INodeSetting {
        [EnumToggleButtons] [OnValueChanged(nameof(EnsurePayload))]
        public ConsequenceKind kind;

        [SerializeReference] [InlineProperty] [HideReferenceObjectPicker] [HideLabel] [ShowIf(nameof(HasPayloadUI))]
        public ConsequencePayload payload;

        public ConsequenceSetting() {
            EnsurePayload();
        }

        bool HasPayloadUI =>
            kind is ConsequenceKind.GiveMoneyToPlayer
                or ConsequenceKind.GiveItemToPlayer
                or ConsequenceKind.GiveQuestToPlayer
                or ConsequenceKind.GiveIntelToPlayer
                or ConsequenceKind.InvokeEmotion;

        public NodeSettingType Type => NodeSettingType.Consequence;

        string INodeSetting.Title =>
            payload switch {
                GiveMoneyConsequencePayload money => $"Player will receive: {money.amount}$",
                GiveItemConsequencePayload item => $"Player will receive: {item.itemName}: {item.quantity} pcs",
                GiveQuestConsequencePayload quest => $"Player will receive quest: {quest.questReference}",
                GiveIntelConsequencePayload intel => $"Player will receive intel: {intel.intelReference}",
                InvokeEmotionPayload emotionPayload => $"{emotionPayload.emotion} emotion will be invoked",
                InvokeHostilityConsequencePayload => "Will start hostility",
                StartTradeConsequencePayload => "Will start trade",
                _ => $"Consequence: {kind}"
            };

        public INodeSetting DeepClone() {
            var clone = new ConsequenceSetting { kind = kind, payload = ClonePayload(payload) };

            if (clone.payload == null)
                clone.EnsurePayload();

            return clone;
        }

        public void EnsurePayload() {
            payload ??= KindToPayload(kind);

            if (payload.Kind != kind)
                payload = KindToPayload(kind);
        }

        static ConsequencePayload KindToPayload(ConsequenceKind kind) {
            return kind switch {
                ConsequenceKind.GiveMoneyToPlayer => new GiveMoneyConsequencePayload(),
                ConsequenceKind.GiveItemToPlayer => new GiveItemConsequencePayload(),
                ConsequenceKind.GiveQuestToPlayer => new GiveQuestConsequencePayload(),
                ConsequenceKind.GiveIntelToPlayer => new GiveIntelConsequencePayload(),
                ConsequenceKind.InvokeEmotion => new InvokeEmotionPayload(),
                ConsequenceKind.InvokeHostility => new InvokeHostilityConsequencePayload(),
                ConsequenceKind.StartTrade => new StartTradeConsequencePayload(),

                _ => new GiveMoneyConsequencePayload()
            };
        }

        static ConsequencePayload ClonePayload(ConsequencePayload source) {
            if (source == null)
                return null;

            return source switch {
                GiveMoneyConsequencePayload payload => new GiveMoneyConsequencePayload { amount = payload.amount },
                GiveItemConsequencePayload payload => new GiveItemConsequencePayload { itemName = payload.itemName, quantity = payload.quantity },
                GiveQuestConsequencePayload payload => new GiveQuestConsequencePayload { questReference = payload.questReference },
                GiveIntelConsequencePayload payload => new GiveIntelConsequencePayload { intelReference = payload.intelReference },
                InvokeEmotionPayload payload => new InvokeEmotionPayload { emotion = payload.emotion },
                InvokeHostilityConsequencePayload => new InvokeHostilityConsequencePayload(),
                StartTradeConsequencePayload => new StartTradeConsequencePayload(),
                _ => (ConsequencePayload)JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType())
            };
        }

        public bool TryGetPayload<TPayload>(out TPayload requestedPayload) where TPayload : class {
            if (payload is TPayload typedPayload) {
                requestedPayload = typedPayload;
                return true;
            }

            requestedPayload = null;
            return false;
        }
    }
}