using UnityEngine;

namespace DialogueSystem {
    public static class NodeBaseSOExtensions {
        /// <summary>
        ///     Attempts to cast the current dialogue to the desired subtype using pattern matching.
        /// </summary>
        static bool TryAs<T>(this NodeBaseSO node, out T result) where T : NodeBaseSO {
            if (node is T typedDialogue) {
                result = typedDialogue;
                return true;
            }

            result = null;
            return false;
        }

        /// <summary>
        ///     Casts the current dialogue to the desired subtype or logs an error when the cast fails.
        /// </summary>
        public static bool TryAs<T>(this NodeBaseSO node, out T result, Object context) where T : NodeBaseSO {
            if (node.TryAs(out result)) {
                return true;
            }

            Debug.LogError($"Failed to cast dialogue of type {node?.GetType().Name ?? "<null>"} to {typeof(T).Name}.", context);
            return false;
        }
    }
}