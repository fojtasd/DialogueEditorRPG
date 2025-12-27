using System.Collections.Generic;

namespace DialogueSystem.Utilities {
    public static class NodeUtility {
        public static string GetNextNodeName(HashSet<string> nodeNames) {
            var counter = 1;

            while (nodeNames.Contains(counter.ToString())) counter++;

            return counter.ToString();
        }
    }
}