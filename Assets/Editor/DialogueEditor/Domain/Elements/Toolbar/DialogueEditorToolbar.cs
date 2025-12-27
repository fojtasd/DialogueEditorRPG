#if UNITY_EDITOR
using System;
using DialogueEditor.Utilities;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public enum SaveCommandType {
        GraphOnly,
        GraphAndConvert,
        ConvertOnly
    }

    public class DialogueEditorToolbar : Toolbar {
        const float FileNameLabelWidth = 70f;

        readonly GraphViewElement _graphViewElement;

        public DialogueEditorToolbar(string defaultFileName,
                                     GraphViewElement graphViewElement,
                                     Action<bool> onSave,
                                     Action onSaveGraphAs,
                                     Action onLoad,
                                     Action onClear,
                                     Action onReset,
                                     Action onToggleErrorInfo) {
            _graphViewElement = graphViewElement ?? throw new ArgumentNullException(nameof(graphViewElement));

            FileNameTextField = ElementUtility.CreateTextField(defaultFileName, "File Name:",
                                                               callback => {
                                                                   if (FileNameTextField != null)
                                                                       FileNameTextField.value = callback.newValue
                                                                                                         .RemoveWhitespaces()
                                                                                                         .RemoveSpecialCharacters();
                                                               });
            ConfigureFileNameLabelWidth();

            SaveButton = new ToolbarMenu { text = "Save", style = { alignSelf = Align.Center, height = 20f } };
            SaveButton.menu.AppendAction("Save Graph", _ => onSave(true), DropdownMenuAction.AlwaysEnabled);
            SaveButton.menu.AppendAction("Save Graph As...", _ => onSaveGraphAs(), DropdownMenuAction.AlwaysEnabled);
            SaveButton.menu.AppendAction("Save Graph and Convert to Scriptable Objects", _ => onSave(false), DropdownMenuAction.AlwaysEnabled);

            Button loadButton = ElementUtility.CreateButton("Load", onLoad);
            Button clearButton = ElementUtility.CreateButton("Clear", onClear);
            Button resetButton = ElementUtility.CreateButton("Reset", onReset);
            ErrorIndicatorButton = ElementUtility.CreateButton("Errors: 0", onToggleErrorInfo);
            ErrorIndicatorButton.AddToClassList("ds-toolbar__button__error-indicator");

            VisualElement searchContainer = CreateSearchContainer();

            Add(FileNameTextField);
            Add(CreateSeparator());
            Add(ErrorIndicatorButton);
            Add(CreateSeparator());
            Add(SaveButton);
            Add(loadButton);
            Add(CreateSeparator());
            Add(clearButton);
            Add(resetButton);
            Add(CreateSeparator());
            Add(searchContainer);

            this.AddStyleSheets("DSToolbarStyles");
        }

        public TextField FileNameTextField { get; }
        public ToolbarMenu SaveButton { get; }
        public Button ErrorIndicatorButton { get; }


        void ConfigureFileNameLabelWidth() {
            VisualElement labelElement = FileNameTextField.labelElement;

            labelElement.style.width = FileNameLabelWidth;
            labelElement.style.minWidth = FileNameLabelWidth;
            labelElement.style.maxWidth = FileNameLabelWidth;
        }

        static VisualElement CreateSeparator() {
            var separator = new VisualElement();
            separator.AddToClassList("toolbar-separator");
            return separator;
        }

        VisualElement CreateSearchContainer() {
            var searchContainer = new VisualElement { style = { flexDirection = FlexDirection.Row, alignItems = Align.Center, justifyContent = Justify.Center } };
            var searchLabel = new Label("Search: ") { style = { maxWidth = 80, paddingLeft = 10 } };
            var searchInput = new TextField { style = { minWidth = 200 } };

            searchInput.RegisterValueChangedCallback(evt => { _graphViewElement.SearchNodes(evt.newValue); });

            searchInput.RegisterCallback<KeyDownEvent>(evt => {
                if (evt.keyCode is not (KeyCode.Return or KeyCode.KeypadEnter))
                    return;

                string term = searchInput.value;
                BaseNode foundDialogueNode = _graphViewElement.SearchNode(term);

                if (foundDialogueNode == null)
                    return;

                _graphViewElement.ClearSelection();
                _graphViewElement.AddToSelection(foundDialogueNode);
                _graphViewElement.FrameSelection();
            });

            searchInput.name = "Search Input";
            searchInput.AddToClassList("search-input");

            searchContainer.Add(searchLabel);
            searchContainer.Add(searchInput);
            return searchContainer;
        }
    }
}
#endif