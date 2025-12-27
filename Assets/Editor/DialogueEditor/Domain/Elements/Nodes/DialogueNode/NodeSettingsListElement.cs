#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using DialogueEditor.Windows;
using DialogueSystem.Node;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public class NodeSettingsListElement : VisualElement {
        readonly List<INodeSetting> _items;
        readonly ListView _list;
        readonly DialogueEditorWindow _ownerWindow;


        public NodeSettingsListElement(List<INodeSetting> items, DialogueEditorWindow ownerWindow) {
            _items = items;
            _ownerWindow = ownerWindow;

            style.flexDirection = FlexDirection.Column;

            // Toolbar
            var toolbar = new VisualElement { style = { flexDirection = FlexDirection.Row, justifyContent = Justify.Center, flexBasis = 25 }, name = "Toolbar" };

            var addBtn = new Button(() => {
                CreateNodeSettingWindow.Open(_ownerWindow, setting => {
                    if (setting == null)
                        return;

                    _items.Add(setting.DeepClone());
                    Refresh();
                    NotifyChanged();
                });
                // TODO
                // sem si dej svůj factory podle toho co chceš přidat
                // _items.Add(new SomeSetting(...));
            }) { text = "Add...", style = { flexGrow = 1 } };

            var removeBtn = new Button(() => {
                if (_list == null)
                    return;
                int i = _list.selectedIndex;
                if (i < 0 || i >= _items.Count)
                    return;
                _items.RemoveAt(i);
                Refresh();
                NotifyChanged();
            }) { text = "Remove", style = { flexGrow = 1 } };

            toolbar.Add(addBtn);
            toolbar.Add(removeBtn);
            Add(toolbar);

            // ListView
            _list = new ListView {
                itemsSource = _items,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight,
                style = { marginTop = 12 },
                makeItem = () => {
                    var row = new VisualElement();
                    row.AddToClassList("node-setting--item");

                    var label = new Label { name = "title" };
                    row.Add(label);

                    return row;
                },
                bindItem = (ve, index) => {
                    INodeSetting setting = _items[index];

                    ve.Q<Label>("title").text = setting.Title;

                    // reset type classes
                    ve.RemoveFromClassList("node-setting--requirement");
                    ve.RemoveFromClassList("node-setting--consequence");
                    ve.RemoveFromClassList("node-setting--defaults");

                    // apply type class
                    ve.AddToClassList(TypeToClass(setting.Type));
                }
            };

            Add(_list);
            Refresh();
        }

        public event Action ListChanged;

        void NotifyChanged() {
            ListChanged?.Invoke();
        }

        static string TypeToClass(NodeSettingType type) {
            return type switch {
                NodeSettingType.Accessibility => "node-setting--requirement",
                NodeSettingType.Consequence => "node-setting--consequence",
                _ => "node-setting--defaults"
            };
        }

        void Refresh() {
            _list.Rebuild(); // po add/remove je tohle nejjistější
        }
    }
}
#endif