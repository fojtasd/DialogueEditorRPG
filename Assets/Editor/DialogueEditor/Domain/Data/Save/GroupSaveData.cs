using System;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    [Serializable]
    public class GroupSaveData {
        [field: SerializeField] public string ID { get; set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
    }
}