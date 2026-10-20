using System.Collections.Generic;
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

            var leagues = new List<League>(router.Session.Catalog.Leagues);
            foreach (var league in leagues)
            {
                if (league.Tier != LeagueTier.Continental)
                    router.Session.PrepareLeague(league.Id);
            }

            foreach (var league in leagues)
            {
                if (league.Tier == LeagueTier.Continental)
                    continue;
                AddLeagueCard(body.transform, router, league, CountClubs(router.Session.Catalog, league.Id));
            }
        }

        static void AddLeagueCard(Transform parent, ScreenRouter router, League league, int clubCount)
        {
            var panel = UiFactory.Panel(parent, league.Id, UiTheme.Panel);
            UiFactory.LayoutElement(panel.GetComponent<Image>(), 148);
            var row = UiFactory.Row(panel, "Inner", 18, new RectOffset(20, 20, 16, 16), true);

            var accent = UiFactory.Panel(row.transform, "Tier", UiTheme.Accent);
            var accentLe = accent.gameObject.AddComponent<LayoutElement>();
            accentLe.minWidth = 8;
            accentLe.preferredWidth = 8;
            accentLe.flexibleWidth = 0;

            var copy = UiFactory.Column(row.transform, "Copy", 6, new RectOffset(0, 0, 0, 0), false, true);
            var copyLe = copy.gameObject.AddComponent<LayoutElement>();
            copyLe.flexibleWidth = 1;
            var title = UiFactory.Label(copy.transform, "Name", league.Name.ToUpperInvariant(), 26, UiTheme.Text, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(title, 30);
            var desc = UiFactory.Label(copy.transform, "Desc", league.Description, 16, UiTheme.Muted);
            UiFactory.LayoutElement(desc, 48);
            var meta = UiFactory.Label(copy.transform, "Meta", $"{clubCount} clubes  ·  {league.ShortName}", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiFactory.LayoutElement(meta, 22);

            var choose = UiFactory.Button(row.transform, "SELECIONAR", () =>
            {
                router.Session.Draft.LeagueId = league.Id;
                router.Session.Draft.ClubId = null;
                router.Session.PrepareLeague(league.Id);
                router.Show("clubs");
            }, UiTheme.Accent);
            UiFactory.LayoutElement(choose, 48);
            var chooseSize = choose.GetComponent<LayoutElement>();
            chooseSize.minWidth = 180;
            chooseSize.preferredWidth = 180;
            chooseSize.flexibleWidth = 0;
        }

        static int CountClubs(WorldState world, string leagueId)
        {
            var count = 0;
            foreach (var club in world.ClubsInLeague(leagueId))
                count++;
            return count;
        }
    }
}
