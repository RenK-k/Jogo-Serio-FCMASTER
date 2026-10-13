using System.Collections.Generic;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class ClubListScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            var league = FindLeague(router);
            ScreenChrome.Page(Root.transform, league.Name, "Escolhe um clube para ver o dossier e, se quiseres, assumir o cargo.", () => router.Show("leagues"), out var body);

            var scroll = UiFactory.Scroll(body.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 240;

            var clubs = new List<Club>(router.Session.Catalog.ClubsInLeague(league.Id));
            clubs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var club in clubs)
                AddClubRow(content, router, club);
        }

        static void AddClubRow(Transform parent, ScreenRouter router, Club club)
        {
            var panel = UiFactory.Panel(parent, club.Id, UiTheme.Panel);
            UiFactory.LayoutElement(panel.GetComponent<Image>(), 118);
            var column = UiFactory.Column(panel, "Inner", 4, new RectOffset(16, 16, 10, 10), stretch: true, controlChildHeight: true);

            var title = UiFactory.Label(column.transform, "Name", club.Name, 24, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(title, 28);
            var meta = UiFactory.Label(column.transform, "Meta", $"{club.City}  ·  {club.Stadium}  ·  {club.Tactics.Formation}", 16, UiTheme.Muted);
            UiFactory.LayoutElement(meta, 22);

            var open = UiFactory.Button(column.transform, "Ver clube", () =>
            {
                router.Session.Draft.ClubId = club.Id;
                router.Show("club");
            }, UiTheme.Accent);
            UiFactory.LayoutElement(open, 36);
        }

        static League FindLeague(ScreenRouter router)
        {
            foreach (var league in router.Session.Catalog.Leagues)
            {
                if (league.Id == router.Session.Draft.LeagueId)
                    return league;
            }

            return router.Session.Catalog.Leagues[0];
        }
    }
}
