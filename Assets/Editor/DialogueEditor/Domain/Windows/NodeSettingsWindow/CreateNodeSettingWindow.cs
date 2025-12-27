#if UNITY_EDITOR
using System;
using DialogueEditor.Windows;
using DialogueSystem.Node;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEngine;

namespace DialogueEditor.Elements {
    public class CreateNodeSettingWindow : OdinEditorWindow {
        [TabGroup("Accessibility")] [InlineProperty] [HideLabel]
        public AccessibilitySetting accessibility = new();

        [TabGroup("Consequence")] [InlineProperty] [HideLabel]
        public ConsequenceSetting consequence = new();

        Action<INodeSetting> _onCreate;
        DialogueEditorWindow _owner;

        protected override void OnDestroy() {
            base.OnDestroy();
            _owner?.SetGraphDimmed(false);
        }

        [TabGroup("Accessibility")]
        [Button(ButtonSizes.Large)]
        void CreateAccessibility() {
            if (!ValidateAccessibility())
                return;

            _onCreate?.Invoke(accessibility);
            Close();
        }

        [TabGroup("Consequence")]
        [Button(ButtonSizes.Large)]
        void CreateConsequence() {
            if (!ValidateConsequence())
                return;

            _onCreate?.Invoke(consequence);
            Close();
        }

        static bool ValidateAccessibility() {
            return true;
        }

        static bool ValidateConsequence() {
            return true;
        }

        public static void Open(DialogueEditorWindow ownerWindow, Action<INodeSetting> onCreate) {
            var win = GetWindow<CreateNodeSettingWindow>(true, "Add Node Setting");
            win._owner = ownerWindow;
            win._onCreate = onCreate;
            win.consequence.EnsurePayload();
            win.minSize = new Vector2(360, 240);
            ownerWindow?.SetGraphDimmed(true);
            win.ShowModalUtility();
        }
    }
}
#endif