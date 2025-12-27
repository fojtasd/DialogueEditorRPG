#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace DialogueEditor.Utilities {
    public static class ElementUtility {
        public static Button CreateButton(string text, Action onClick = null) {
            Button button = new(onClick) { text = text };

            return button;
        }

        public static Foldout CreateFoldout(string title, bool isCollapsed = false) {
            Foldout foldout = new() { value = !isCollapsed, text = title };
            return foldout;
        }

        public static TextField CreateTextField(string value = null, string label = null,
                                                EventCallback<ChangeEvent<string>> onValueChanged = null) {
            TextField textField = new() { value = value, label = label };
            textField.verticalScrollerVisibility = ScrollerVisibility.Auto;

            if (onValueChanged != null) {
                textField.RegisterValueChangedCallback(onValueChanged);
            }

            return textField;
        }

        public static TextField CreateTextArea(string value = null, string label = null,
                                               EventCallback<ChangeEvent<string>> onValueChanged = null) {
            TextField textArea = CreateTextField(value, label, onValueChanged);

            textArea.multiline = true;

            return textArea;
        }
    }
}
#endif