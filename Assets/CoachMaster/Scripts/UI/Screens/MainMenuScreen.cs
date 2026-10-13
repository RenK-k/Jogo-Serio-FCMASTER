using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class MainMenuScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            var column = UiFactory.Column(Root.transform, "Menu", 18, new RectOffset(400, 400, 64, 64), stretch: true, controlChildHeight: true);
            column.childAlignment = TextAnchor.MiddleCenter;

            var spacerTop = new GameObject("Top", typeof(RectTransform), typeof(LayoutElement));
            spacerTop.transform.SetParent(column.transform, false);
            spacerTop.GetComponent<LayoutElement>().flexibleHeight = 1;

            var card = UiFactory.Panel(column.transform, "WelcomeCard", UiTheme.Panel);
            UiFactory.LayoutElement(card.GetComponent<Image>(), 300);
            var cardContent = UiFactory.Column(card, "WelcomeContent", 16, new RectOffset(34, 34, 28, 28), true, true);

            var eyebrow = UiFactory.Label(cardContent.transform, "Eyebrow", "GESTÃO · TÁTICA · LIDERANÇA", 14, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.LayoutElement(eyebrow, 24);
            var title = UiFactory.Label(cardContent.transform, "Title", "COACH MASTER", 54, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiFactory.LayoutElement(title, 66);

            var tag = UiFactory.Label(cardContent.transform, "Tag", "Constrói a tua história no banco.", 21, UiTheme.Muted, TextAnchor.MiddleCenter);
            UiFactory.LayoutElement(tag, 34);

            var start = UiFactory.Button(cardContent.transform, "Nova Carreira", () => router.Show("leagues"), UiTheme.Accent);
            UiFactory.LayoutElement(start, 56);
            start.GetComponent<LayoutElement>().preferredWidth = 420;

            var spacerBottom = new GameObject("Bottom", typeof(RectTransform), typeof(LayoutElement));
            spacerBottom.transform.SetParent(column.transform, false);
            spacerBottom.GetComponent<LayoutElement>().flexibleHeight = 1;
        }
    }
}
