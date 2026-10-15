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
            router.Session.PrepareLeague(league.Id);
            ScreenChrome.Page(Root.transform, "Selecionar clube", $"{league.Name}  ·  escolhe o clube e vê a camisola no menu.", () => router.Show("leagues"), out var body);

            var scroll = UiFactory.Scroll(body.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 240;

            var clubs = new List<Club>(router.Session.Catalog.ClubsInLeague(league.Id));
            clubs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            foreach (var club in clubs)
                AddClubRow(content, router, club);

            var hint = UiFactory.Label(
                body.transform,
                "KitHint",
                "Podes substituir qualquer camisola largando um PNG em Assets/CoachMaster/Resources/Kits/ com o nome do clube, por exemplo norte-maritimo.png.",
                14,
                UiTheme.Muted);
            UiFactory.LayoutElement(hint, 36);
        }

        static void AddClubRow(Transform parent, ScreenRouter router, Club club)
        {
            var panel = UiFactory.Panel(parent, club.Id, UiTheme.Panel);
            UiFactory.LayoutElement(panel.GetComponent<Image>(), 132);
            var row = UiFactory.Row(panel, "Inner", 16, new RectOffset(12, 16, 10, 10), true);

            var kit = KitView.Create(row.transform, club, new Vector2(92, 112), true);
            var kitLe = kit.gameObject.AddComponent<LayoutElement>();
            kitLe.minWidth = 92;
            kitLe.preferredWidth = 92;
            kitLe.minHeight = 112;
            kitLe.flexibleWidth = 0;

            var copy = UiFactory.Column(row.transform, "Copy", 4, new RectOffset(0, 0, 8, 8), false, true);
            var copyLe = copy.gameObject.AddComponent<LayoutElement>();
            copyLe.flexibleWidth = 1;
            var title = UiFactory.Label(copy.transform, "Name", club.Name.ToUpperInvariant(), 22, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(title, 28);
            var meta = UiFactory.Label(copy.transform, "Meta", $"{club.City}  ·  {club.Stadium}", 15, UiTheme.Muted);
            UiFactory.LayoutElement(meta, 22);
            var tactics = UiFactory.Label(copy.transform, "Tactics", club.Tactics.Formation + "  ·  " + club.Tactics.Pressing, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(tactics, 20);

            var open = UiFactory.Button(row.transform, "VER CLUBE", () =>
            {
                router.Session.Draft.ClubId = club.Id;
                router.Show("club");
            }, UiTheme.Accent);
            UiFactory.LayoutElement(open, 44);
            var openSize = open.GetComponent<LayoutElement>();
            openSize.minWidth = 160;
            openSize.preferredWidth = 160;
            openSize.flexibleWidth = 0;
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
