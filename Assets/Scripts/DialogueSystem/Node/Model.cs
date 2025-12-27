using System;
using System.Collections.Generic;
using DialogueSystem.Node;
using UnityEngine;

namespace Contracts.Dialogues.Nodes {
    /// <summary>
    ///     This container is holding information of Dialogue Node, such as <see cref="Text" />, <see cref="SpeakerType" />  or
    ///     <see cref="NodeSettings" />
    /// </summary>
    [Serializable]
    public class Model {
        /// <summary>
        /// Dialogue text said in this node
        /// </summary>
        [field: SerializeField, TextArea] public string Text { get; set; } = "Dialogue text";
        /// <summary>
        /// Entity speaking in this node
        /// </summary>
        [field: SerializeField] public SpeakerType SpeakerType { get; set; } = SpeakerType.NPC;
        /// <summary>
        /// Is visiting this node somehow restricted?
        /// </summary>
        [Header("Additional Info")] 
        [field: SerializeReference] public List<INodeSetting> NodeSettings { get; set; }
    }
}