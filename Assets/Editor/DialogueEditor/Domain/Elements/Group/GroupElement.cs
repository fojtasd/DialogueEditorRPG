#if UNITY_EDITOR
using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace DialogueEditor.Elements {
    public sealed class GroupElement : Group {
        readonly Color _defaultBorderColor;
        readonly float _defaultBorderWidth;
        Color _defaultTitleColor;
        bool _hasCachedTitleColor;
        Label _titleLabel;

        public GroupElement(string groupTitle, Vector2 position) {
            ID = Guid.NewGuid().ToString();

            title = groupTitle;
            OldTitle = groupTitle;

            SetPosition(new Rect(position, Vector2.zero));

            _defaultBorderColor = contentContainer.style.borderBottomColor.value;
            _defaultBorderWidth = contentContainer.style.borderBottomWidth.value;

            EnsureTitleLabelCached();
            schedule.Execute(EnsureTitleLabelCached).ExecuteLater(0);
        }

        public string ID { get; set; }
        public string OldTitle { get; set; }

        public void SetErrorStyle() {
            ApplyColors(Color.red, 2f, Color.red);
        }

        public void ResetStyle() {
            ApplyColors(_defaultBorderColor,
                        _defaultBorderWidth,
                        _hasCachedTitleColor ? _defaultTitleColor : null);
        }

        public void ApplyColors(Color? borderColor = null, float? borderWidth = null, Color? titleColor = null) {
            EnsureTitleLabelCached();

            if (borderColor.HasValue) {
                Color resolvedBorderColor = borderColor.Value;
                contentContainer.style.borderBottomColor = resolvedBorderColor;
                contentContainer.style.borderTopColor = resolvedBorderColor;
                contentContainer.style.borderLeftColor = resolvedBorderColor;
                contentContainer.style.borderRightColor = resolvedBorderColor;

                float widthToApply = borderWidth ?? _defaultBorderWidth;
                SetBorderWidth(widthToApply);
            } else if (borderWidth.HasValue) {
                SetBorderWidth(borderWidth.Value);
            }
        }

        void SetBorderWidth(float width) {
            contentContainer.style.borderBottomWidth = width;
            contentContainer.style.borderTopWidth = width;
            contentContainer.style.borderLeftWidth = width;
            contentContainer.style.borderRightWidth = width;
        }

        void EnsureTitleLabelCached() {
            if (_titleLabel == null) {
                _titleLabel = this.Q<Label>("title-label") ?? this.Q<Label>();
            }

            if (_titleLabel != null && !_hasCachedTitleColor) {
                _defaultTitleColor = ResolveLabelColor(_titleLabel);
                _hasCachedTitleColor = true;
            }
        }

        static Color ResolveLabelColor(Label label) {
            if (label == null)
                return Color.white;

            StyleColor currentStyle = label.style.color;
            return currentStyle.keyword == StyleKeyword.Undefined ? currentStyle.value : label.resolvedStyle.color;
        }
    }
}
#endif