using System.Collections.Generic;
using System.Text;
using CoachMaster.Domain;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Screens
{
    public sealed class ClubInfoScreen : GameScreen
    {
        protected override void Build(ScreenRouter router)
        {
            UiFactory.FullScreen(Root.transform, "Bg", UiTheme.Canvas);
            var club = router.Session.Catalog.Clubs[router.Session.Draft.ClubId];
            ScreenChrome.Page(Root.transform, club.Name, $"{club.City}  ·  Fundado em {club.FoundedYear}  ·  {club.Stadium}", () => router.Show("clubs"), out var body);

            var scroll = UiFactory.Scroll(body.transform, out var content);
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 200;

            AddBlock(content, "História", club.History);
            AddBlock(content, "Reputação", club.PublicReputation);
            AddBlock(content, "Situação financeira", club.Finance.PublicSituation);
            AddBlock(content, "Identidade do antigo treinador",
                $"{club.Tactics.FormerCoachName}\n{club.Tactics.Summary}");
            AddBlock(content, "Cargo vago", club.PublicCoachVacancyNote);
            AddBlock(content, "Objetivos da direção", JoinLines(club.BoardObjectives));
            AddBlock(content, "Estado do plantel", club.PublicTeamState);
            AddBlock(content, "Equipa técnica", StaffText(club));
            AddBlock(content, "Plantel", SquadText(router, club));

            var choose = UiFactory.Button(body.transform, "Escolher este clube", () => router.Show("create"), UiTheme.Accent);
            UiFactory.LayoutElement(choose, 52);
        }

        static void AddBlock(Transform parent, string title, string body)
        {
            var panel = UiFactory.Panel(parent, title, UiTheme.Panel);
            var column = UiFactory.Column(panel, "Inner", 6, new RectOffset(16, 16, 12, 12), stretch: true, controlChildHeight: true);
            var heading = UiFactory.Label(column.transform, "H", title, 18, UiTheme.Accent, TextAnchor.UpperLeft, FontStyle.Bold);
            UiFactory.LayoutElement(heading, 24);
            var text = UiFactory.Label(column.transform, "B", body, 18, UiTheme.Text);
            var height = EstimateHeight(body);
            UiFactory.LayoutElement(text, height);
            UiFactory.LayoutElement(panel.GetComponent<Image>(), 24 + height + 28);
        }

        static float EstimateHeight(string body)
        {
            var lines = Mathf.Max(2, body.Split('\n').Length + body.Length / 90);
            return Mathf.Clamp(lines * 22, 48, 520);
        }

        static string JoinLines(List<string> items)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < items.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                sb.Append("· ").Append(items[i]);
            }
            return sb.ToString();
        }

        static string StaffText(Club club)
        {
            var sb = new StringBuilder();
            for (var i = 0; i < club.Staff.Count; i++)
            {
                if (i > 0) sb.Append('\n');
                var staff = club.Staff[i];
                sb.Append(PresentationLabels.StaffRole(staff.Role)).Append(" — ").Append(staff.FullName);
            }
            return sb.ToString();
        }

        static string SquadText(ScreenRouter router, Club club)
        {
            var players = new List<Player>();
            foreach (var id in club.PlayerIds)
                players.Add(router.Session.Catalog.Players[id]);
            players.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));

            var sb = new StringBuilder();
            foreach (var player in players)
            {
                if (sb.Length > 0) sb.Append('\n');
                sb.Append(player.SquadNumber.ToString("00"))
                    .Append("  ")
                    .Append(PresentationLabels.Position(player.Position).PadRight(3))
                    .Append("  ")
                    .Append(player.FullName)
                    .Append("  (").Append(player.Age).Append(")");
                if (player.IsCaptain) sb.Append("  ·  Capitão");
                if (player.IsViceCaptain) sb.Append("  ·  Vice-capitão");
            }
            return sb.ToString();
        }
    }
}
