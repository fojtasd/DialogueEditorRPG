using System;

namespace DialogueSystem.Node {
    public static class NodeSettingExtensions {
        public static TPayload GetPayloadOrThrow<TPayload>(this INodeSetting setting) where TPayload : class {
            if (setting == null)
                throw new ArgumentNullException(nameof(setting));

            if (setting.TryGetPayload(out TPayload payload))
                return payload;

            throw new InvalidOperationException($"Unable to resolve payload of type {typeof(TPayload).Name} from {setting.GetType().Name}.");
        }

        public static TPayload GetPayloadOrDefault<TPayload>(this INodeSetting setting) where TPayload : class {
            if (setting == null)
                return null;

            return setting.TryGetPayload(out TPayload payload) ? payload : null;
        }
    }
}

