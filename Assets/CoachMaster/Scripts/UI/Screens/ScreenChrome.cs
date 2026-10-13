using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public static class ScreenChrome
    {
        public static RectTransform Page(Transform parent, string title, string subtitle, Action onBack, out VerticalLayoutGroup body)
        {
            var column = UiFactory.Column(parent, "Page", 16, new RectOffset(48, 48, 36, 36), stretch: true, controlChildHeight: true);
            var header = UiFactory.Label(column.transform, "Title", title, 36, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(header, 44);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = UiFactory.Label(column.transform, "Subtitle", subtitle, 20, UiTheme.Muted, TextAnchor.UpperLeft);
                UiFactory.LayoutElement(sub, 48);
            }

            body = UiFactory.Column(column.transform, "Body", 12, new RectOffset(0, 0, 0, 0), controlChildHeight: true);
            var bodyLe = body.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1;
            bodyLe.minHeight = 200;

            if (onBack != null)
            {
                var back = UiFactory.Button(column.transform, "Voltar", onBack, UiTheme.AccentDark);
                UiFactory.LayoutElement(back, 48);
            }

            return column.GetComponent<RectTransform>();
        }
    }
}
