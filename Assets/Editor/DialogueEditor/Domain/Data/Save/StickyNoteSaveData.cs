using System;
using UnityEngine;

namespace DialogueEditor.Data.Save {
    [Serializable]
    public class StickyNoteSaveData {
        [field: SerializeField] public string Text { get; set; }
        [field: SerializeField] public Vector2 Position { get; set; }
        [field: SerializeField] public Vector2 Size { get; set; }
        [field: SerializeField] public string ColorClass { get; set; }
        [field: SerializeField] public bool IsLocked { get; set; }
        [field: SerializeField] public bool IsBold { get; set; }
        [field: SerializeField] public float FontSize { get; set; }
    }
}