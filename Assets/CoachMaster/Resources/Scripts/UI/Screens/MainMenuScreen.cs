using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class MainMenuScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            var stripe = UiFactory.Panel(Root.transform, "GreenStripe", UiTheme.Accent);
            var stripeRect = stripe.GetComponent<RectTransform>();
            stripeRect.anchorMin = new Vector2(0f, 0f);
            stripeRect.anchorMax = new Vector2(0f, 1f);
            stripeRect.pivot = new Vector2(0f, 0.5f);
            stripeRect.sizeDelta = new Vector2(8, 0);
            stripeRect.anchoredPosition = Vector2.zero;

            var column = UiFactory.Column(Root.transform, "Menu", 14, new RectOffset(80, 80, 70, 48), stretch: true, controlChildHeight: true);
            column.childAlignment = TextAnchor.MiddleLeft;

            var spacerTop = new GameObject("Top", typeof(RectTransform), typeof(LayoutElement));
            spacerTop.transform.SetParent(column.transform, false);
            spacerTop.GetComponent<LayoutElement>().flexibleHeight = 1;

            var brandObject = new GameObject("Brand", typeof(RectTransform), typeof(RawImage), typeof(LayoutElement));
            brandObject.transform.SetParent(column.transform, false);
            var brand = brandObject.GetComponent<RawImage>();
            BroadcastUi.ApplyBrand(brand);
            var brandLayout = brandObject.GetComponent<LayoutElement>();
            brandLayout.preferredHeight = 54f;
            brandLayout.minHeight = 54f;
            brandLayout.preferredWidth = 54f;
            brandLayout.flexibleWidth = 0f;
            var title = UiFactory.Label(column.transform, "Title", "SIMULADOR DE TREINADOR", 48, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(title, 72);
            var tag = UiFactory.Label(column.transform, "Tag", "Escolhe um clube, assume o banco e constrói a tua história.", 20, UiTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(tag, 32);

            var start = UiFactory.Button(column.transform, "NOVA CARREIRA", () => router.Show("leagues"), UiTheme.Accent);
            UiFactory.LayoutElement(start, 56);
            start.GetComponent<LayoutElement>().preferredWidth = 360;
            start.GetComponent<LayoutElement>().flexibleWidth = 0;

            var spacerBottom = new GameObject("Bottom", typeof(RectTransform), typeof(LayoutElement));
            spacerBottom.transform.SetParent(column.transform, false);
            spacerBottom.GetComponent<LayoutElement>().flexibleHeight = 1;

            var foot = UiFactory.Label(column.transform, "Foot", "Seleciona um clube.", 14, UiTheme.Muted, TextAnchor.MiddleLeft);
            UiFactory.LayoutElement(foot, 22);
        }
    }
}
