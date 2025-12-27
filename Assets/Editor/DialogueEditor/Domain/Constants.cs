using Contracts.Dialogues;
using UnityEngine;

namespace DialogueEditor {
    public static class Constants {
        public static readonly string GraphFolderPath = "Assets/Editor/DialogueSystem/Graphs";
        public static readonly Color DefaultBackgroundColor = new(29f / 255f, 29f / 255f, 30f / 255f);
        public static readonly Color GlowingBorderColor = new(1f, 0.82f, 0.31f);
        public static readonly Color GlowingColor = new(1f, 0.9f, 0.2f, 0.15f);

        public static readonly string SpeakerTooltip = "NPC:\n" +
                                                       "- this dialogue sentence was said by NPC\n" +
                                                       "PLAYER:\n" +
                                                       "- this dialogue sentence was said by player\n" +
                                                       "NARRATOR:\n" +
                                                       "- this dialogue sentence was said by narrator\n" +
                                                       "CHECK:\n" +
                                                       "- this dialogue node is a check and requires exactly 2 choices\n" +
                                                       "- first choice is for successful check and second for failed check\n" +
                                                       "- also needs exactly 1 attribute requirement to check\n";

        public static readonly string NodeIndicatorTooltip = "The number of node various rules, such as Accessibility or Consequences of the node. Node is providing something or its accessibility is limited by some kind of condition.";
        public static readonly string NodeNameTooltip = "Node name must be unique, identifies node.";
        public static readonly string FocusOnButtonTooltip = "Center view on the node.";
        public static readonly string AddChoiceButtonTooltip = "Adds new choice and creates new port. Be aware that node should always contains at least one ChoicePort. Otherwise its considered as ending point.";
        public static readonly string FollowNextNodeButtonTooltip = "Center view on the next node.";
        public static readonly string DeleteChoiceButtonTooltip = "Deletes this choice.";
        public static readonly string IfNodeTypeTooltip = "Type of stat this IF node should check. SKILL or ATTRIBUTE.";

        public static Color GetSpeakerBorderColor(SpeakerType speakerType) {
            return speakerType switch {
                SpeakerType.PLAYER => new Color32(155, 197, 61, 255), // Green
                SpeakerType.NPC => new Color32(150, 150, 150, 255), // Default gray
                SpeakerType.NARRATOR => new Color32(255, 165, 0, 255), // Orange
                _ => new Color32(150, 150, 150, 255) // Default gray
            };
        }
    }
}