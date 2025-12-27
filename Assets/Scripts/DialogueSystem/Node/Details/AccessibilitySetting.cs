using System;
using Contracts.Contracts;
using Sirenix.OdinInspector;
using UnityEngine;

namespace DialogueSystem.Node {
    [Serializable]
    public class AccessibilitySetting : INodeSetting {
        [EnumToggleButtons] [OnValueChanged(nameof(EnsurePayload))]
        public AccessibilityKind kind;

        [SerializeReference] [InlineProperty] [HideReferenceObjectPicker] [HideLabel] [ShowIf(nameof(HasPayloadUI))]
        public AccessibilityPayload payload;

        public AccessibilitySetting() {
            EnsurePayload();
        }

        bool HasPayloadUI =>
            kind is AccessibilityKind.NotVisitableAfterVisiting
                or AccessibilityKind.VisitableOnlyAfterVisiting
                or AccessibilityKind.AttributeRequired
                or AccessibilityKind.SkillRequired
                or AccessibilityKind.IntelRequired
                or AccessibilityKind.QuestRequired
                or AccessibilityKind.ItemRequired
                or AccessibilityKind.MoneyRequired;

        public NodeSettingType Type => NodeSettingType.Accessibility;

        string INodeSetting.Title =>
            payload switch {
                // TODO add more options
                IsVisitableOnlyOncePayload => "Node is visitable only once.",
                NotVisitableAfterVisitingPayload node => $"Node is not visitable after visiting node: {node.nodeId}",
                VisitableOnlyAfterVisitingPayload node => $"Node is visitable only after visiting node: {node.nodeId}",
                IntelRequiredPayload node => $"Node is not visitable until knowing: {node.intelID}",
                AttributeRequiredPayload node => $"Node is not visitable until having: {node.attributeType} on {node.value}",
                SkillRequiredPayload node => $"Node is not visitable until having: {node.skillType}  on {node.value}",
                QuestRequiredPayload node => $"Node is not visitable until having: {node.questID}",
                MoneyRequiredPayload node => $"Node is not visitable until {node.quantity} $",
                
                _ => "Accessibility more"
            };

        public INodeSetting DeepClone() {
            var clone = new AccessibilitySetting { kind = kind, payload = ClonePayload(payload) };

            if (clone.payload == null)
                clone.EnsurePayload();

            return clone;
        }

        public void EnsurePayload() {
            payload ??= KindToPayload(kind);

            if (payload.Kind != kind)
                payload = KindToPayload(kind);
        }

        static AccessibilityPayload KindToPayload(AccessibilityKind kind) {
            return kind switch {
                AccessibilityKind.IsVisitableOnlyOnce => new IsVisitableOnlyOncePayload(),
                AccessibilityKind.NotVisitableAfterVisiting => new NotVisitableAfterVisitingPayload(),
                AccessibilityKind.VisitableOnlyAfterVisiting => new VisitableOnlyAfterVisitingPayload(),
                AccessibilityKind.AttributeRequired => new AttributeRequiredPayload(),
                AccessibilityKind.SkillRequired => new SkillRequiredPayload(),
                AccessibilityKind.IntelRequired => new IntelRequiredPayload(),
                AccessibilityKind.QuestRequired => new QuestRequiredPayload(),
                AccessibilityKind.ItemRequired => new ItemRequiredPayload(),
                AccessibilityKind.MoneyRequired => new MoneyRequiredPayload(),

                _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
            };
        }

        static AccessibilityPayload ClonePayload(AccessibilityPayload source) {
            if (source == null)
                return null;

            return source switch {
                IsVisitableOnlyOncePayload => new IsVisitableOnlyOncePayload(),
                NotVisitableAfterVisitingPayload payload => new NotVisitableAfterVisitingPayload { nodeId = payload.nodeId },
                VisitableOnlyAfterVisitingPayload payload => new VisitableOnlyAfterVisitingPayload{nodeId =  payload.nodeId},
                IntelRequiredPayload payload => new IntelRequiredPayload { intelID = payload.intelID },
                QuestRequiredPayload payload => new QuestRequiredPayload { questID = payload.questID },
                SkillRequiredPayload payload => new SkillRequiredPayload {skillType = payload.skillType, value = payload.value },
                AttributeRequiredPayload payload => new AttributeRequiredPayload{attributeType = payload.attributeType, value = payload.value },
                ItemRequiredPayload payload => new ItemRequiredPayload { itemName = payload.itemName, quantity =  payload.quantity },
                MoneyRequiredPayload payload => new MoneyRequiredPayload { quantity = payload.quantity },
                _ => (AccessibilityPayload)JsonUtility.FromJson(JsonUtility.ToJson(source), source.GetType())
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

    [Serializable]
    public abstract class AccessibilityPayload {
        public abstract AccessibilityKind Kind { get; }
    }

    [Serializable]
    public sealed class IsVisitableOnlyOncePayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.IsVisitableOnlyOnce;
    }

    [Serializable]
    public sealed class NotVisitableAfterVisitingPayload : AccessibilityPayload {
        public string nodeId;
        public override AccessibilityKind Kind => AccessibilityKind.NotVisitableAfterVisiting;
    }
    
    [Serializable]
    public sealed class VisitableOnlyAfterVisitingPayload : AccessibilityPayload {
        public string nodeId;
        public override AccessibilityKind Kind => AccessibilityKind.VisitableOnlyAfterVisiting;
    }
    
    [Serializable]
    public sealed class IntelRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.IntelRequired;
        public string intelID;
    }
    
    [Serializable]
    public sealed class QuestRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.QuestRequired;
        public string questID;
    }
    
    [Serializable]
    public sealed class AttributeRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.AttributeRequired;
        public int value;
        [EnumToggleButtons] public AttributeType attributeType;
    }
     
    [Serializable]
    public sealed class SkillRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.SkillRequired;
        public string value;
        [EnumToggleButtons] public SkillType skillType;
    }
    
    [Serializable]
    public sealed class ItemRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.ItemRequired;
        public string itemName;
        public int quantity;
    }
    
    [Serializable]
    public sealed class MoneyRequiredPayload : AccessibilityPayload {
        public override AccessibilityKind Kind => AccessibilityKind.MoneyRequired;
        public int quantity;
    }
}