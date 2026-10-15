using System.Collections.Generic;
using System.Text;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class ClubInfoController : MonoBehaviour
    {
        void Start()
        {
            var session = GameRoot.Require().Session;
            if (string.IsNullOrEmpty(session.Draft.ClubId) || !session.Catalog.Clubs.ContainsKey(session.Draft.ClubId))
            {
                GameRoot.Instance.Open(SceneNames.TeamSelect);
                return;
            }

            var club = session.Catalog.Clubs[session.Draft.ClubId];
            var root = BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, club.ShortName);
            LeagueSelectController.Header(root, club.Name.ToUpperInvariant(), club.City + "   ·   " + club.FoundedYear + "   ·   " + club.Stadium, () => GameRoot.Instance.Open(SceneNames.TeamSelect));

            var kitCard = BroadcastUi.Panel(root, "KitCard", new Color(0.08f, 0.03f, 0.16f, 0.62f));
            BroadcastUi.Place(kitCard.rectTransform, 0.03f, 0.16f, 0.24f, 0.78f);
            var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            var crestRect = crestGo.GetComponent<RectTransform>();
            crestRect.SetParent(kitCard.transform, false);
            BroadcastUi.Place(crestRect, 0.22f, 0.55f, 0.78f, 0.92f);
            var crest = crestGo.GetComponent<Image>();
            crest.preserveAspect = true;
            crest.raycastTarget = false;
            crest.sprite = CrestStudio.For(club);
            var kitGo = new GameObject("Kit", typeof(RectTransform), typeof(Image));
            var kitRect = kitGo.GetComponent<RectTransform>();
            kitRect.SetParent(kitCard.transform, false);
            BroadcastUi.Place(kitRect, 0.18f, 0.08f, 0.82f, 0.5f);
            var kit = kitGo.GetComponent<Image>();
            kit.preserveAspect = true;
            kit.raycastTarget = false;
            KitView.Apply(kit, club);

            Card(root, "Historia", 0.26f, 0.16f, 0.52f, 0.78f, "HISTÓRIA", History(club));
            var pitch = BroadcastUi.Panel(root, "Plantel", new Color(0.08f, 0.03f, 0.16f, 0.55f));
            BroadcastUi.Place(pitch.rectTransform, 0.54f, 0.16f, 0.97f, 0.78f);
            var heading = BroadcastUi.Label(pitch.transform, "Title", "PLANTEL  ·  " + club.Tactics.Formation, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.04f, 0.9f, 0.96f, 0.98f);
            var field = new GameObject("Field", typeof(RectTransform)).GetComponent<RectTransform>();
            field.SetParent(pitch.transform, false);
            BroadcastUi.Place(field, 0.02f, 0.02f, 0.98f, 0.88f);
            PlaceFormation(field, session, club);

            var choose = BroadcastUi.Button(root, "Choose", "ASSUMIR O BANCO", () => GameRoot.Instance.Open(SceneNames.CreateCareer), UiTheme.Accent, UiTheme.ButtonText, 20);
            BroadcastUi.Place(choose.GetComponent<RectTransform>(), 0.34f, 0.065f, 0.66f, 0.14f);
            var hint = BroadcastUi.Label(root, "ControlHint", "Enter  assumir     Esc  voltar", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.04f, 0.008f, 0.5f, 0.05f);
        }

        static void PlaceFormation(RectTransform host, GameSession session, Club club)
        {
            var squad = new List<Player>();
            foreach (var id in club.PlayerIds)
            {
                if (session.Catalog.Players.TryGetValue(id, out var player))
                    squad.Add(player);
            }

            var eleven = new List<Player>();
            var bench = new List<Player>();
            StartingShape.Split(squad, club.Tactics.Formation, eleven, bench);
            var lines = StartingShape.Lines(club.Tactics.Formation);
            var cursor = 0;
            var rows = lines.Length;
            for (var row = 0; row < rows; row++)
            {
                var count = lines[row];
                var y = rows == 1 ? 0.5f : 0.12f + row * (0.76f / (rows - 1));
                for (var col = 0; col < count && cursor < eleven.Count; col++)
                {
                    var player = eleven[cursor];
                    cursor++;
                    var card = BroadcastUi.Panel(host, "P" + player.SquadNumber, new Color(0.1f, 0.04f, 0.18f, 0.94f));
                    card.rectTransform.anchorMin = card.rectTransform.anchorMax = new Vector2((col + 1f) / (count + 1f), y);
                    card.rectTransform.sizeDelta = new Vector2(108, 92);
                    var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
                    var faceRect = faceGo.GetComponent<RectTransform>();
                    faceRect.SetParent(card.transform, false);
                    faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.5f, 0.68f);
                    faceRect.sizeDelta = new Vector2(40, 40);
                    var face = faceGo.GetComponent<Image>();
                    face.sprite = FaceBook.For(player) ?? PortraitStudio.For(player.Id);
                    face.preserveAspect = true;
                    face.raycastTarget = false;
                    var number = BroadcastUi.Label(card.transform, "Number", player.SquadNumber.ToString(), 12, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(number.rectTransform, 0.04f, 0.78f, 0.28f, 0.96f);
                    var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(), 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(name.rectTransform, 0.04f, 0.3f, 0.96f, 0.48f);
                    var meta = BroadcastUi.Label(card.transform, "Meta", PresentationLabels.Position(player.Position) + "  ·  " + player.Nationality, 10, UiTheme.Accent, TextAnchor.MiddleCenter);
                    BroadcastUi.Place(meta.rectTransform, 0.04f, 0.06f, 0.96f, 0.28f);
                }
            }
        }

        static void Card(Transform root, string name, float x0, float y0, float x1, float y1, string title, string body)
        {
            var panel = BroadcastUi.Panel(root, name, new Color(0.08f, 0.03f, 0.16f, 0.55f));
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            var heading = BroadcastUi.Label(panel.transform, "Title", title, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.05f, 0.88f, 0.95f, 0.97f);
            var scroll = BroadcastUi.Scroll(panel.transform, "Scroll", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.03f, 0.03f, 0.97f, 0.86f);
            var text = BroadcastUi.Label(content, "Body", "", 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.SetWrapped(text, body, 200);
        }

        static string History(Club club)
        {
            var sb = new StringBuilder();
            sb.AppendLine(club.History).AppendLine();
            sb.AppendLine("REPUTAÇÃO").AppendLine(club.PublicReputation).AppendLine();
            sb.AppendLine("FINANÇAS").AppendLine(club.Finance.PublicSituation).AppendLine();
            sb.AppendLine("IDENTIDADE").AppendLine(club.Tactics.Summary).AppendLine();
            sb.AppendLine("CARGO VAGO").AppendLine(club.PublicCoachVacancyNote).AppendLine();
            sb.AppendLine("OBJETIVOS");
            foreach (var item in club.BoardObjectives)
                sb.Append("· ").AppendLine(item);
            return sb.ToString();
        }
    }
}
