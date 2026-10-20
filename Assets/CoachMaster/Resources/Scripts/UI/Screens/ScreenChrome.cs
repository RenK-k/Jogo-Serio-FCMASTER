using System;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public static class ScreenChrome
    {
        public static RectTransform Page(Transform parent, string title, string subtitle, Action onBack, out VerticalLayoutGroup body)
        {
            var page = UiFactory.Column(parent, "Page", 0, new RectOffset(0, 0, 0, 0), stretch: true, controlChildHeight: true);

            var header = UiFactory.Panel(page.transform, "Header", UiTheme.Header);
            UiFactory.LayoutElement(header.GetComponent<Image>(), 92);
            var headerRow = UiFactory.Row(header, "HeaderRow", 18, new RectOffset(28, 36, 16, 12), true);

            if (onBack != null)
            {
                var back = UiFactory.Button(headerRow.transform, "VOLTAR", onBack, UiTheme.AccentDark);
                UiFactory.LayoutElement(back, 44);
                var backSize = back.GetComponent<LayoutElement>();
                backSize.minWidth = 132;
                backSize.preferredWidth = 132;
                backSize.flexibleWidth = 0;
                back.GetComponentInChildren<Text>().fontSize = 16;
            }

            var titles = UiFactory.Column(headerRow.transform, "Titles", 2, new RectOffset(0, 0, 0, 0), false, true);
            var titlesLe = titles.gameObject.AddComponent<LayoutElement>();
            titlesLe.flexibleWidth = 1;
            titlesLe.minHeight = 56;
            var heading = UiFactory.Label(titles.transform, "Title", title.ToUpperInvariant(), 30, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 34);
            if (!string.IsNullOrEmpty(subtitle))
            {
                var sub = UiFactory.Label(titles.transform, "Subtitle", subtitle, 16, UiTheme.Muted, TextAnchor.UpperLeft);
                UiFactory.LayoutElement(sub, 22);
            }

            var green = UiFactory.Panel(page.transform, "AccentBar", UiTheme.Accent);
            UiFactory.LayoutElement(green.GetComponent<Image>(), 4);

            body = UiFactory.Column(page.transform, "Body", 12, new RectOffset(36, 36, 18, 24), controlChildHeight: true);
            var bodyLe = body.gameObject.AddComponent<LayoutElement>();
            bodyLe.flexibleHeight = 1;
            bodyLe.minHeight = 200;
            return page.GetComponent<RectTransform>();
        }
    }
}
