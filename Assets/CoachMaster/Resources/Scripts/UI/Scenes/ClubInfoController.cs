using System.Collections.Generic;
using System.Text;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class ClubInfoController : MonoBehaviour
    {
        void Start()
        {
            LocalizationManager.ApplyScene();
            var session = GameRoot.Require().Session;
            if (string.IsNullOrEmpty(session.Draft.ClubId) || !session.Catalog.Clubs.ContainsKey(session.Draft.ClubId))
            {
                GameRoot.Instance.Open(SceneNames.TeamSelect);
                return;
            }

            var club = session.Catalog.Clubs[session.Draft.ClubId];
            if (transform.Find("Design") != null)
            {
                BindClub(BroadcastUi.Mount(this), session, club);
                return;
            }

            Paint(session, club, false);
        }

        public void BakeDesign(GameSession session)
        {
            if (string.IsNullOrEmpty(session.Draft.LeagueId))
                session.Draft.LeagueId = Ids.League("atlas");
            session.PrepareLeague(session.Draft.LeagueId);
            if (string.IsNullOrEmpty(session.Draft.ClubId))
            {
                foreach (var club in session.Catalog.ClubsInLeague(session.Draft.LeagueId))
                {
                    session.Draft.ClubId = club.Id;
                    break;
                }
            }

            if (session.Catalog.Clubs.ContainsKey(session.Draft.ClubId))
                Paint(session, session.Catalog.Clubs[session.Draft.ClubId], true);
        }

        void BindClub(Transform root, GameSession session, Club club)
        {
            SceneBind.Wire(root.Find("Header/Back"), () => GameRoot.Instance.Open(SceneNames.TeamSelect));
            var wordmark = SceneBind.Get<Text>(root, "Wordmark/League");
            if (wordmark != null)
                wordmark.text = club.Name.ToUpperInvariant();
            var title = SceneBind.Get<Text>(root, "Header/Title");
            if (title != null)
                title.text = club.Name.ToUpperInvariant();
            var sub = SceneBind.Get<Text>(root, "Header/Subtitle");
            if (sub != null)
                sub.text = LocalizationManager.Translate(club.City) + "   ·   " + club.FoundedYear + "   ·   " + LocalizationManager.Translate(club.Stadium);
            var crest = SceneBind.Get<Image>(root, "KitCard/Crest");
            if (crest != null)
                crest.sprite = CrestStudio.For(club);
            var kit = root.Find("KitCard/Kit")?.GetComponent<Image>();
            if (kit != null)
                kit.gameObject.SetActive(false);
            var heading = SceneBind.Get<Text>(root, "Plantel/Title");
            if (heading != null)
                heading.text = LocalizationManager.Translate("PLANTEL") + "  ·  " + club.Tactics.Formation;
            var body = SceneBind.Get<Text>(root, "Historia/Scroll/Content/Body");
            if (body != null)
                BroadcastUi.SetWrapped(body, History(club), 200);
            var field = root.Find("Plantel/Field") as RectTransform;
            if (field != null)
            {
                for (var i = field.childCount - 1; i >= 0; i--)
                    Destroy(field.GetChild(i).gameObject);
                BroadcastUi.Place(field, 0.18f, 0.015f, 0.82f, 0.985f);
                BroadcastUi.DrawVerticalHalfPitch(field, new Color(0.12f, 0.46f, 0.22f));
                PlaceFormation(field, session, club);
            }

            SceneBind.Wire(root.Find("Choose"), () => GameRoot.Instance.Open(SceneNames.CreateCareer));
        }

        void Paint(GameSession session, Club club, bool bake)
        {
            var root = bake ? BroadcastUi.CreateDesign(transform) : BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, club.Name);
            LeagueSelectController.Header(root, club.Name.ToUpperInvariant(), LocalizationManager.Translate(club.City) + "   ·   " + club.FoundedYear + "   ·   " + LocalizationManager.Translate(club.Stadium), () => GameRoot.Instance.Open(SceneNames.TeamSelect));

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
            Card(root, "Historia", 0.26f, 0.16f, 0.52f, 0.78f, LocalizationManager.Translate("HISTÓRIA"), History(club));
            var pitch = BroadcastUi.Panel(root, "Plantel", new Color(0.08f, 0.03f, 0.16f, 0.55f));
            BroadcastUi.Place(pitch.rectTransform, 0.54f, 0.16f, 0.97f, 0.78f);
            var heading = BroadcastUi.Label(pitch.transform, "Title", LocalizationManager.Translate("PLANTEL") + "  ·  " + club.Tactics.Formation, 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.04f, 0.9f, 0.96f, 0.98f);
            var field = new GameObject("Field", typeof(RectTransform)).GetComponent<RectTransform>();
            field.SetParent(pitch.transform, false);
            BroadcastUi.Place(field, 0.18f, 0.015f, 0.82f, 0.985f);
            BroadcastUi.DrawVerticalHalfPitch(field, new Color(0.12f, 0.46f, 0.22f));
            PlaceFormation(field, session, club);

            var choose = BroadcastUi.Button(root, "Choose", "ASSUMIR O BANCO", () => GameRoot.Instance.Open(SceneNames.CreateCareer), UiTheme.Accent, UiTheme.ButtonText, 20);
            BroadcastUi.Place(choose.GetComponent<RectTransform>(), 0.34f, 0.065f, 0.66f, 0.14f);
            var hint = BroadcastUi.Label(root, "SettingsHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleLeft);
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
            var slotPositions = StartingShape.SlotPositions(club.Tactics.Formation);
            for (var index = 0; index < eleven.Count && index < 11; index++)
            {
                var player = eleven[index];
                var position = index < slotPositions.Length ? slotPositions[index] : player.Position;
                var card = PlayerCardView.Create(host, "P" + player.SquadNumber, player, club, position, true);
                card.rectTransform.anchorMin = card.rectTransform.anchorMax = MatchEngine.Slot(true, index, club.Tactics.Formation);
                card.rectTransform.sizeDelta = new Vector2(88, 100);
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
            if (LocalizationManager.Current == GameLanguage.English)
            {
                var country = LocalizationManager.Translate(club.City);
                var text = new StringBuilder();
                text.Append("Founded in ").Append(club.FoundedYear).Append(", ").Append(club.Name)
                    .Append(" represent ").Append(country).Append(" and compete in a ")
                    .Append(club.Reputation >= 70 ? "top-flight project." : club.Reputation >= 45 ? "competitive league project." : "developing league project.")
                    .AppendLine().AppendLine();
                text.AppendLine("REPUTATION").AppendLine(club.Reputation >= 78 ? "A leading club with high expectations." :
                    club.Reputation >= 66 ? "An established side with a strong following." :
                    club.Reputation >= 48 ? "A competitive club with room to grow." : "A modest club working to establish itself.").AppendLine();
                text.AppendLine("FINANCES").AppendLine(club.Finance != null && club.Finance.TransferBudget >= 10000000
                    ? "The club has room to invest in the squad."
                    : club.Finance != null && club.Finance.TransferBudget >= 1000000
                        ? "The budget allows selective investment."
                        : "The club needs to manage its budget carefully.").AppendLine();
                text.AppendLine("IDENTITY").AppendLine(LocalizationManager.Translate(club.Tactics.Summary)).AppendLine();
                text.AppendLine("MANAGER POSITION").AppendLine("The board is looking for a manager to lead the project.").AppendLine();
                text.AppendLine("OBJECTIVES");
                foreach (var item in club.BoardObjectives)
                    text.Append("· ").AppendLine(EnglishObjective(item));
                return text.ToString();
            }

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

        static string EnglishObjective(string objective)
        {
            if (string.IsNullOrEmpty(objective)) return "Maintain the club's progress.";
            switch (objective)
            {
                case "Terminar no primeiro terço da tabela": return "Finish in the top third of the table.";
                case "Disputar o título": return "Challenge for the title.";
                case "Garantir a manutenção": return "Secure top-flight survival.";
                case "Lutar pela qualificação europeia": return "Challenge for European qualification.";
                case "Ficar a meio da tabela": return "Finish in mid-table.";
                case "Manutenção confortável": return "Secure a comfortable mid-table finish.";
                case "Evitar a descida": return "Avoid relegation.";
                case "Manutenção": return "Secure the club's place in the division.";
                case "Lutar pela subida": return "Challenge for promotion.";
                case "Subir de divisão": return "Win promotion.";
                case "Chegar aos lugares de subida": return "Reach the promotion places.";
                case "Pontuar com regularidade": return "Pick up points consistently.";
                case "Manter o balneário calmo": return "Keep the dressing room settled.";
                default: return "Meet the board's expectations in the league.";
            }
        }
    }
}
