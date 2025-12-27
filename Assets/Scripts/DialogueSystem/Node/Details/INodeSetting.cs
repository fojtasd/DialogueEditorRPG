using Contracts.Interfaces;

namespace DialogueSystem.Node {
    public interface INodeSetting : IDeepCloneable<INodeSetting> {
        string Title { get; }
        NodeSettingType Type { get; }

        bool TryGetPayload<TPayload>(out TPayload payload) where TPayload : class;
    }

    public enum NodeSettingType {
        Accessibility, Consequence
    }
}