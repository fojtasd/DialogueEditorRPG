using UnityEngine;

namespace DialogueSystem {
    public class GroupSO : ScriptableObject {
        [field: SerializeField] public string GroupName { get; set; }

        public void Initialize(string groupName) {
            GroupName = groupName;
        }
    }
}