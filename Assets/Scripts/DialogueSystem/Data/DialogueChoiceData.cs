using System;
using UnityEngine;

namespace DialogueSystem {
    /// <summary>
    ///     Connection to next node provided with choice
    /// </summary>
    [Serializable]
    public class DialogueChoiceData {
        /// <summary>
        ///     Reference to the following node
        /// </summary>
        [field: SerializeField]
        public NodeBaseSO NextDialogue { get; set; }
    }
}