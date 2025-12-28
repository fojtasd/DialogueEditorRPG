#if UNITY_EDITOR
using System;
using DialogueSystem.Node;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public abstract class BaseNode : Node {
        IVisualElementScheduledItem _flickerSchedule;
        VisualElement _flickerTarget = new();

        bool _isGlowing;
        public GraphViewElement GraphViewElement;
        public string ID { get; set; }
        public string NodeName { get; set; }
        public GroupElement GroupElement { get; set; }
        public ChoiceType ChoiceType { get; set; } = ChoiceType.SingleChoice;

        public virtual bool ShouldPersistToAssets => true;
        public virtual bool ShouldTrackName => true;
        public virtual bool ShouldIgnoreStartingNodeValidation => false;

        public virtual void Initialize(string nodeName, GraphViewElement graphViewElement, Vector2 position) {
            ID = Guid.NewGuid().ToString();
            NodeName = nodeName;
            GraphViewElement = graphViewElement;
            SetPosition(new Rect(position, Vector2.zero));
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt) {
            if (GraphViewElement == null)
                return;

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("🎯 Center on Node", _ => CenterOnNode());
            evt.menu.AppendAction("Ports/🔌️ Disconnect All Ports", _ => GraphViewElement.DisconnectAllPorts(this));
            evt.menu.AppendAction("Groups/Remove from Group",
                                  _ => RemoveFromGroup(),
                                  _ => GroupElement != null
                                      ? DropdownMenuAction.Status.Normal
                                      : DropdownMenuAction.Status.Disabled);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("🗑️ Delete", _ => DeleteNode());
            evt.menu.AppendSeparator();
        }

        void DeleteNode() {
            if (GraphViewElement == null)
                return;

            bool yes = EditorUtility.DisplayDialog(
                                                   "Delete node?",
                                                   $"Really delete node \"{NodeName}\"?\nThis cannot be undone.",
                                                   "Yes",
                                                   "No"
                                                  );

            if (!yes)
                return;
            GraphViewElement.RemoveElement(this);
        }

        void CenterOnNode() {
            GraphViewElement.ClearSelection();
            GraphViewElement.AddToSelection(this);
            GraphViewElement.FrameSelection();
        }

        public void ResetStyleAfterError() {
            mainContainer.style.backgroundColor = Constants.DefaultBackgroundColor;
            RefreshUIInternal();
        }

        public void SetErrorStyle(Color color) {
            mainContainer.style.backgroundColor = color;
        }


        void RemoveFromGroup() {
            if (GraphViewElement == null || GroupElement == null)
                return;

            GroupElement.RemoveElement(this);
        }

        protected abstract void RefreshUIInternal();

        public void RefreshUI() {
            RefreshUIInternal();
        }

        protected void HighlightNode(BaseNode nodeToHighlight) {
            GraphViewElement.HighlightedNode?.RemoveHighlight();
            GraphViewElement.HighlightedNode = nodeToHighlight;
            GraphViewElement.HighlightedNode.StartHighlighting();
        }

        public void RemoveHighlight() {
            _flickerSchedule?.Pause();
            _flickerTarget.style.backgroundColor = Constants.DefaultBackgroundColor;

            _flickerTarget.style.borderTopColor = Constants.DefaultBackgroundColor;
            _flickerTarget.style.borderBottomColor = Constants.DefaultBackgroundColor;
            _flickerTarget.style.borderLeftColor = Constants.DefaultBackgroundColor;
            _flickerTarget.style.borderRightColor = Constants.DefaultBackgroundColor;

            RefreshUIInternal();
        }

        public void StartHighlighting() {
            _flickerTarget = mainContainer;

            _flickerTarget.style.borderTopWidth = 4;
            _flickerTarget.style.borderBottomWidth = 4;
            _flickerTarget.style.borderLeftWidth = 4;
            _flickerTarget.style.borderRightWidth = 4;

            _flickerSchedule?.Pause();

            _flickerSchedule = _flickerTarget.schedule.Execute(() => {
                _isGlowing = !_isGlowing;

                _flickerTarget.style.backgroundColor = _isGlowing ? Constants.GlowingColor : Constants.DefaultBackgroundColor;

                _flickerTarget.style.borderTopColor = Color.yellow;
                _flickerTarget.style.borderBottomColor = Color.yellow;
                _flickerTarget.style.borderLeftColor = Color.yellow;
                _flickerTarget.style.borderRightColor = Color.yellow;
            }).Every(250); // Flicker every 0.5s
        }
    }
}
#endif