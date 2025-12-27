#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using DialogueEditor.Utilities;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public class StickyNoteElement : GraphElement {
        const string UnityTextInputClass = "unity-text-field__input";
        const string StickyNoteStyleSheet = "StickyNoteStyles";
        const string StickyNoteBaseClass = "sticky-note";
        const string StickyNoteLockedClass = "sticky-note--locked";
        const string StickyNoteLockActiveClass = "sticky-note__toolbar-button--lock--active";
        const string StickyNoteBoldClass = "sticky-note--bold";
        const string StickyNoteBoldActiveClass = "sticky-note__toolbar-button--bold--active";
        const string StickyNoteFontSizeFieldClass = "sticky-note__font-size-field";

        const float MinWidth = 400f;
        const float MinHeight = 140f;
        const float DefaultFontSize = 14f;
        const float MinFontSize = 10f;
        const float MaxFontSize = 36f;

        static readonly List<StickyNoteColorOption> ColorOptions = new() {
            new StickyNoteColorOption("Yellow", "sticky-note--yellow",
                                      "sticky-note__color-dropdown--yellow"),
            new StickyNoteColorOption("Blue", "sticky-note--blue",
                                      "sticky-note__color-dropdown--blue"),
            new StickyNoteColorOption("Green", "sticky-note--green",
                                      "sticky-note__color-dropdown--green"),
            new StickyNoteColorOption("Pink", "sticky-note--pink",
                                      "sticky-note__color-dropdown--pink")
        };

        readonly VisualElement _resizeHandle;

        readonly TextField _textField;
        readonly VisualElement _textInput;
        Button _boldButton;
        PopupField<StickyNoteColorOption> _colorDropdown;
        StickyNoteColorOption _currentColorOption;
        Button _deleteButton;
        IntegerField _fontSizeField;
        bool _isResizing;
        Button _lockButton;

        public StickyNoteElement() {
            this.AddStyleSheets(StickyNoteStyleSheet);
            AddToClassList(StickyNoteBaseClass);

            style.width = MinWidth;
            style.minWidth = MinWidth;
            style.minHeight = MinHeight;

            capabilities |= Capabilities.Movable;
            capabilities |= Capabilities.Selectable;
            capabilities |= Capabilities.Deletable;
            capabilities |= Capabilities.Resizable;

            _textField = new TextField { multiline = true };
            _textField.ClearClassList();

            _textInput = _textField.Q(className: UnityTextInputClass);
            _textInput?.ClearClassList();
            if (_textInput != null)
                _textInput.style.color = Color.black;

            _textField.style.color = Color.black;

            _textField.style.flexGrow = 1;
            _textField.style.whiteSpace = WhiteSpace.Normal;
            _textField.style.marginTop = 4;
            ApplyFontSize();

            StickyNoteColorOption defaultColor = ColorOptions[0];
            VisualElement toolbar = CreateToolbar(defaultColor);
            RegisterCallback<AttachToPanelEvent>(_ => HideUnityResizerIcon());

            Add(toolbar);
            Add(_textField);

            ApplyColor(defaultColor);
            ApplyBoldState();
            UpdateResizeHandleState();
        }

        public bool IsLocked { get; private set; }

        public bool IsBold { get; private set; }

        public float FontSize { get; private set; } = DefaultFontSize;

        void HideUnityResizerIcon() {
            var resizer = this.Q<Resizer>();

            VisualElement icon = resizer?.Q(className: "resizer-icon");
            if (icon != null)
                icon.style.display = DisplayStyle.None;
        }

        public void SetFontSize(float fontSize) {
            FontSize = Mathf.Clamp(fontSize, MinFontSize, MaxFontSize);
            ApplyFontSize();
        }

        public void SetText(string text) {
            _textField.value = text;
        }

        public string GetText() {
            return _textField.value;
        }

        public override void SetPosition(Rect newRect) {
            newRect.width = Mathf.Max(MinWidth, newRect.width);
            newRect.height = Mathf.Max(MinHeight, newRect.height);

            base.SetPosition(newRect);

            style.width = newRect.width;
            style.height = newRect.height;
        }

        public string GetColorClassName() {
            return _currentColorOption?.ClassName;
        }

        public void SetColorByClassName(string className) {
            StickyNoteColorOption option = null;

            if (!string.IsNullOrEmpty(className))
                option = ColorOptions.FirstOrDefault(color => color.ClassName == className);

            ApplyColor(option ?? ColorOptions[0]);
        }

        public void SetLockState(bool isLocked) {
            IsLocked = isLocked;
            ApplyLockState();
        }

        public void SetBoldState(bool isBold) {
            IsBold = isBold;
            ApplyBoldState();
        }

        VisualElement CreateToolbar(StickyNoteColorOption defaultColor) {
            var toolbar = new VisualElement();
            toolbar.AddToClassList("sticky-note__toolbar");

            _colorDropdown = new PopupField<StickyNoteColorOption>(
                                                                   ColorOptions,
                                                                   ColorOptions.IndexOf(defaultColor),
                                                                   option => option.DisplayName,
                                                                   option => option.DisplayName);
            _colorDropdown.RegisterValueChangedCallback(evt => ApplyColor(evt.newValue));
            _colorDropdown.AddToClassList("sticky-note__color-dropdown");
            _colorDropdown.style.marginLeft = 4;

            _fontSizeField = CreateFontSizeField();

            Button increaseOrderButton = new(() => AdjustOrder(1)) { text = "↑", style = { marginLeft = 2, marginRight = 2 } };
            increaseOrderButton.AddToClassList("sticky-note__toolbar-button");

            Button decreaseOrderButton = new(() => AdjustOrder(-1)) { text = "↓", style = { marginRight = 2 } };
            decreaseOrderButton.AddToClassList("sticky-note__toolbar-button");

            _boldButton = new Button(ToggleBold) { text = "B", style = { marginLeft = 2, marginRight = 2 } };
            _boldButton.AddToClassList("sticky-note__toolbar-button");
            _boldButton.AddToClassList("sticky-note__toolbar-button--bold");


            _deleteButton = new Button(DeleteSelf) { text = "Delete", style = { marginLeft = 2 } };
            _deleteButton.AddToClassList("sticky-note__toolbar-button");
            _deleteButton.AddToClassList("sticky-note__toolbar-button--delete");


            _lockButton = new Button(ToggleLock) { text = "LOCK", style = { marginRight = 2 } };
            _lockButton.AddToClassList("sticky-note__toolbar-button");
            _lockButton.AddToClassList("sticky-note__toolbar-button--lock");

            toolbar.Add(_lockButton);
            toolbar.Add(_colorDropdown);
            toolbar.Add(_fontSizeField);
            toolbar.Add(increaseOrderButton);
            toolbar.Add(decreaseOrderButton);
            toolbar.Add(_boldButton);
            toolbar.Add(_deleteButton);

            ApplyLockState();

            return toolbar;
        }

        void ApplyFontSize() {
            if (_textField != null)
                _textField.style.fontSize = FontSize;

            if (_textInput != null)
                _textInput.style.fontSize = FontSize;

            _fontSizeField?.SetValueWithoutNotify(Mathf.RoundToInt(FontSize));
        }

        IntegerField CreateFontSizeField() {
            var fontSizeField = new IntegerField { value = Mathf.RoundToInt(FontSize), isDelayed = true, label = string.Empty, style = { width = 54 } };
            fontSizeField.AddToClassList(StickyNoteFontSizeFieldClass);

            fontSizeField.RegisterValueChangedCallback(evt => {
                if (IsLocked) {
                    fontSizeField.SetValueWithoutNotify(Mathf.RoundToInt(FontSize));
                    return;
                }

                int clampedValue = Mathf.Clamp(evt.newValue, Mathf.RoundToInt(MinFontSize), Mathf.RoundToInt(MaxFontSize));
                FontSize = clampedValue;
                ApplyFontSize();
            });

            return fontSizeField;
        }

        void ApplyColor(StickyNoteColorOption option) {
            if (option == null)
                return;

            if (_currentColorOption != null)
                RemoveFromClassList(_currentColorOption.ClassName);
            if (_currentColorOption != null)
                _colorDropdown?.RemoveFromClassList(_currentColorOption.DropdownClassName);

            _currentColorOption = option;
            AddToClassList(option.ClassName);
            _colorDropdown?.AddToClassList(option.DropdownClassName);
            if (_colorDropdown != null && _colorDropdown.value != option)
                _colorDropdown.SetValueWithoutNotify(option);
        }

        void DeleteSelf() {
            if (IsLocked)
                return;

            bool confirmed = EditorUtility.DisplayDialog("Delete sticky note?", "Are you sure you want to delete this sticky note?", "Delete", "Cancel");
            if (!confirmed)
                return;

            var graphView = GetFirstAncestorOfType<GraphView>();
            graphView?.RemoveElement(this);
        }

        void ToggleLock() {
            IsLocked = !IsLocked;
            ApplyLockState();
        }

        void ToggleBold() {
            if (IsLocked)
                return;

            IsBold = !IsBold;
            ApplyBoldState();
        }

        void ApplyBoldState() {
            if (_boldButton != null) {
                if (IsBold)
                    _boldButton.AddToClassList(StickyNoteBoldActiveClass);
                else
                    _boldButton.RemoveFromClassList(StickyNoteBoldActiveClass);
            }

            if (IsBold)
                AddToClassList(StickyNoteBoldClass);
            else
                RemoveFromClassList(StickyNoteBoldClass);

            if (_textField != null)
                _textField.style.unityFontStyleAndWeight = IsBold ? FontStyle.Bold : FontStyle.Normal;

            if (_textInput != null)
                _textInput.style.unityFontStyleAndWeight = IsBold ? FontStyle.Bold : FontStyle.Normal;
        }

        void ApplyLockState() {
            if (IsLocked) {
                capabilities &= ~(Capabilities.Movable | Capabilities.Deletable);
                AddToClassList(StickyNoteLockedClass);
                _lockButton?.AddToClassList(StickyNoteLockActiveClass);
                _boldButton?.SetEnabled(false);
                _fontSizeField?.SetEnabled(false);
                if (_textField != null) {
                    _textField.isReadOnly = true;
                }

                _deleteButton?.SetEnabled(false);
                if (_lockButton != null)
                    _lockButton.text = "UNLOCK";
            } else {
                capabilities |= Capabilities.Movable | Capabilities.Deletable;
                RemoveFromClassList(StickyNoteLockedClass);
                _lockButton?.RemoveFromClassList(StickyNoteLockActiveClass);
                _boldButton?.SetEnabled(true);
                _fontSizeField?.SetEnabled(true);
                if (_textField != null) {
                    _textField.isReadOnly = false;
                }

                _deleteButton?.SetEnabled(true);
                if (_lockButton != null)
                    _lockButton.text = "LOCK";
            }

            UpdateResizeHandleState();
        }

        void AdjustOrder(int delta) {
            VisualElement container = parent;
            if (container == null)
                return;

            int currentIndex = container.IndexOf(this);
            if (currentIndex < 0)
                return;

            int targetIndex = Mathf.Clamp(currentIndex + delta, 0, container.childCount - 1);
            if (targetIndex == currentIndex)
                return;

            container.Remove(this);
            container.Insert(targetIndex, this);
        }

        void UpdateResizeHandleState() {
            if (_resizeHandle == null)
                return;

            bool isEnabled = !IsLocked;

            _resizeHandle.SetEnabled(isEnabled);
            _resizeHandle.style.display = isEnabled ? DisplayStyle.Flex : DisplayStyle.None;
            _resizeHandle.pickingMode = isEnabled ? PickingMode.Position : PickingMode.Ignore;

            if (!isEnabled && _isResizing)
                StopResizing();
        }

        void StopResizing() {
            _isResizing = false;
        }

        sealed class StickyNoteColorOption {
            public StickyNoteColorOption(string displayName, string className, string dropdownClassName) {
                DisplayName = displayName;
                ClassName = className;
                DropdownClassName = dropdownClassName;
            }

            public string DisplayName { get; }
            public string ClassName { get; }
            public string DropdownClassName { get; }
        }
    }
}
#endif