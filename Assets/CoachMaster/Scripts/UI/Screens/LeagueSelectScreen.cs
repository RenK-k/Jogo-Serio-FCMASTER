using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class LeagueSelectScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            ScreenChrome.Page(Root.transform, "Nova carreira", "Escolhe a liga em que queres começar.", () => router.Show("main"), out var body);

            foreach (var league in router.Session.Catalog.Leagues)
                AddLeagueCard(body.transform, router, league);
        }

        static void AddLeagueCard(Transform parent, ScreenRouter router, League league)
        {
            var panel = UiFactory.Panel(parent, league.Id, UiTheme.Panel);
            UiFactory.LayoutElement(panel.GetComponent<Image>(), 168);
            var column = UiFactory.Column(panel, "Inner", 6, new RectOffset(20, 20, 16, 16), stretch: true, controlChildHeight: true);

            var title = UiFactory.Label(column.transform, "Name", league.Name, 28, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(title, 32);
            var desc = UiFactory.Label(column.transform, "Desc", league.Description, 18, UiTheme.Muted);
            UiFactory.LayoutElement(desc, 44);

            var choose = UiFactory.Button(column.transform, "Escolher " + league.Name, () =>
            {
                router.Session.Draft.LeagueId = league.Id;
                router.Session.Draft.ClubId = null;
                router.Show("clubs");
            }, UiTheme.Accent);
            UiFactory.LayoutElement(choose, 42);
        }
    }
}
