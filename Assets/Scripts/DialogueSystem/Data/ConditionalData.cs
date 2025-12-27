using System;
using Contracts.Contracts;
using DialogueSystem.Node;
using UnityEngine;

namespace DialogueSystem {
    /// <summary>
    ///     Data stored on Conditional Node. Based on <see cref="ConditionTargetType" /> can be used for evaluation
    ///     of Conditional Node.
    /// </summary>
    [Serializable]
    public class ConditionalNodeData {
        /// <summary>
        ///     This is minimal expected value from the check
        /// </summary>
        [field: SerializeField]
        public int ExpectedValue { get; set; }

        /// <summary>
        ///     If  <see cref="ConditionTargetType" /> is <see cref="ConditionTargetType.Attribute" />, this
        ///     field will provide information of what Attribute is expected to be checked
        /// </summary>
        [field: SerializeField]
        public AttributeType AttributeType { get; set; }

        /// <summary>
        ///     If  <see cref="ConditionTargetType" /> is <see cref="ConditionTargetType.Skill" />, this
        ///     field will provide information of what Skill is expected to be checked
        /// </summary>
        [field: SerializeField]
        public SkillType SkillType { get; set; }

        /// <summary>
        ///     Provides information what needs to be checked
        /// </summary>
        [field: SerializeField]
        public ConditionTargetType Kind { get; set; }
    }
}