using System.Collections.Generic;
using System.Text;
using CoachMaster.App;
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
            var root = transform;
            var session = GameRoot.Require().Session;
            var club = session.Catalog.Clubs[session.Draft.ClubId];
            SceneUtil.Find<Text>(root, "Header/Title").text = club.Name.ToUpperInvariant();
            SceneUtil.Find<Text>(root, "Header/Subtitle").text = $"{club.City}  ·  {club.FoundedYear}  ·  {club.Stadium}";
            SceneUtil.BindButton(root, "Header/Back", () => GameRoot.Instance.Open(SceneNames.TeamSelect));
            SceneUtil.BindButton(root, "Choose", () => GameRoot.Instance.Open(SceneNames.CreateCareer));
            KitView.Apply(SceneUtil.Find<Image>(root, "KitSlot/Kit"), club, SceneUtil.Find<Text>(root, "KitSlot/Hint"));
            SceneUtil.Find<Text>(root, "Dossier").text = BuildDossier(session, club);
        }

        static string BuildDossier(GameSession session, Club club)
        {
            var sb = new StringBuilder();
            sb.AppendLine("HISTÓRIA").AppendLine(club.History).AppendLine();
            sb.AppendLine("REPUTAÇÃO").AppendLine(club.PublicReputation).AppendLine();
            sb.AppendLine("FINANÇAS").AppendLine(club.Finance.PublicSituation).AppendLine();
            sb.AppendLine("IDENTIDADE").AppendLine(club.Tactics.Summary).AppendLine();
            sb.AppendLine("CARGO VAGO").AppendLine(club.PublicCoachVacancyNote).AppendLine();
            sb.AppendLine("OBJETIVOS");
            foreach (var item in club.BoardObjectives)
                sb.Append("· ").AppendLine(item);
            sb.AppendLine().AppendLine("PLANTEL");
            var players = new List<Player>();
            foreach (var id in club.PlayerIds)
                players.Add(session.Catalog.Players[id]);
            players.Sort((a, b) => a.SquadNumber.CompareTo(b.SquadNumber));
            foreach (var player in players)
            {
                sb.Append(player.SquadNumber.ToString("00")).Append("  ")
                    .Append(PresentationLabels.Position(player.Position)).Append("  ")
                    .Append(player.FullName);
                if (player.IsCaptain) sb.Append("  C");
                sb.AppendLine();
            }
            return sb.ToString();
        }
    }
}
