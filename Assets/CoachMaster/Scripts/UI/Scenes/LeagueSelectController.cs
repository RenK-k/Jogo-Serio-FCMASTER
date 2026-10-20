using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class LeagueSelectController : MonoBehaviour
    {
        void Start()
        {
            LocalizationManager.ApplyScene();
            var session = GameRoot.Require().Session;
            if (transform.Find("Design") != null)
            {
                BindLeagues(BroadcastUi.Mount(this), session);
                return;
            }

            Paint(session, false);
        }

        public void BakeDesign(GameSession session)
        {
            Paint(session, true);
        }

        void BindLeagues(Transform root, GameSession session)
        {
            SceneBind.Wire(root.Find("Header/Back"), () => GameRoot.Instance.Open(SceneNames.MainMenu));
            var content = root.Find("Leagues/Content");
            if (content == null)
                return;
            var leagues = new List<League>(session.Catalog.Leagues);
            leagues.RemoveAll(league => league.Tier == LeagueTier.Continental);
            var template = content.Find("League0");
            for (var i = 0; i < leagues.Count; i++)
            {
                var league = leagues[i];
                var card = content.Find("League" + i);
                if (card == null && template != null)
                {
                    card = Instantiate(template.gameObject, content, false).transform;
                    card.name = "League" + i;
                }
                if (card == null)
                    continue;
                SetCardText(card, "Name", LocalizationManager.Translate(league.Name.ToUpperInvariant()));
                SetCardText(card, "Desc", LeagueDescription(league));
                SetCardText(card, "Tier", LocalizationManager.Translate(TierLabel(league.Tier)));
                var countNode = card.Find("Count");
                var countLabel = countNode == null ? null : countNode.GetComponent<Text>();
                if (countLabel != null)
                    countLabel.text = league.ClubCount + (LocalizationManager.Current == GameLanguage.English ? " clubs" : " clubes");
                System.Action choose = () =>
                {
                    session.Draft.LeagueId = league.Id;
                    session.Draft.ClubId = null;
                    session.PrepareLeague(league.Id);
                    GameRoot.Instance.Open(SceneNames.TeamSelect);
                };
                SceneBind.Wire(card, choose);
                SceneBind.Wire(card.Find("Select"), choose);
            }

            for (var i = leagues.Count; i < content.childCount; i++)
            {
                var card = content.Find("League" + i);
                if (card != null)
                    card.gameObject.SetActive(false);
            }
        }

        void Paint(GameSession session, bool bake)
        {
            var root = bake ? BroadcastUi.CreateDesign(transform) : BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, LocalizationManager.Translate("Escolha a liga"));
            Header(root, "LIGAS", LocalizationManager.Current == GameLanguage.English
                ? "12 competitions · 7 countries · 244 clubs"
                : "12 competições · 7 países · 244 clubes", () => GameRoot.Instance.Open(SceneNames.MainMenu));

            var scroll = BroadcastUi.Scroll(root, "Leagues", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.08f, 0.1f, 0.92f, 0.78f);
            var leagues = new List<League>(session.Catalog.Leagues);
            leagues.RemoveAll(league => league.Tier == LeagueTier.Continental);
            for (var i = 0; i < leagues.Count; i++)
            {
                var league = leagues[i];
                var card = BroadcastUi.Panel(content, "League" + i, new Color(0.1f, 0.04f, 0.2f, 0.72f), true);
                BroadcastUi.Preferred(card, 132);
                var tier = LocalizationManager.Translate(TierLabel(league.Tier)).ToUpperInvariant();
                var badge = BroadcastUi.Label(card.transform, "Tier", tier, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(badge.rectTransform, 0.04f, 0.68f, 0.4f, 0.9f);
                var count = BroadcastUi.Label(card.transform, "Count", league.ClubCount + (LocalizationManager.Current == GameLanguage.English ? " clubs" : " clubes"), 14, UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(count.rectTransform, 0.55f, 0.68f, 0.72f, 0.9f);
                var name = BroadcastUi.Label(card.transform, "Name", LocalizationManager.Translate(league.Name.ToUpperInvariant()), 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.04f, 0.34f, 0.7f, 0.68f);
                var desc = BroadcastUi.Label(card.transform, "Desc", LeagueDescription(league), 16, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(desc.rectTransform, 0.04f, 0.08f, 0.7f, 0.36f);
                System.Action choose = () =>
                {
                    session.Draft.LeagueId = league.Id;
                    session.Draft.ClubId = null;
                    session.PrepareLeague(league.Id);
                    GameRoot.Instance.Open(SceneNames.TeamSelect);
                };
                BroadcastUi.Hit(card, choose);
                var pick = BroadcastUi.Button(card.transform, "Select", LocalizationManager.Translate("ESCOLHER"), choose, UiTheme.Accent, UiTheme.ButtonText, 16);
                BroadcastUi.Place(pick.GetComponent<RectTransform>(), 0.74f, 0.28f, 0.96f, 0.72f);
            }

            var hint = BroadcastUi.Label(root, "ControlHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.08f, 0.008f, 0.8f, 0.05f);
        }

        static void SetCardText(Transform card, string childName, string value)
        {
            var child = card == null ? null : card.Find(childName);
            var label = child == null ? null : child.GetComponent<Text>();
            if (label != null)
                label.text = value ?? "";
        }

        static string TierLabel(LeagueTier tier)
        {
            switch (tier)
            {
                case LeagueTier.First: return "1ª DIVISÃO";
                case LeagueTier.Second: return "2ª DIVISÃO";
                case LeagueTier.Third: return "3ª DIVISÃO";
                case LeagueTier.Fourth: return "4ª DIVISÃO";
                default: return "COMPETIÇÃO";
            }
        }

        static string LeagueDescription(League league)
        {
            if (LocalizationManager.Current != GameLanguage.English)
                return league.Description;
            if (league.Id == Ids.League("atlas"))
                return "Portugal's top competition. Eighteen clubs contest the title and fight to stay up.";
            if (league.Id == Ids.League("horizonte"))
                return "Portugal's second tier. Eighteen clubs are chasing promotion to Liga Portugal.";
            if (league.Tier == LeagueTier.Continental)
                return "The leading clubs from each top division. Choose a domestic league; the standings appear in your career.";

            var country = "";
            switch (league.Id)
            {
                case "league:inglesa":
                case "league:inglesa-championship":
                case "league:inglesa-league-one":
                case "league:inglesa-2": country = "England"; break;
                case "league:francesa": country = "France"; break;
                case "league:bundesliga": country = "Germany"; break;
                case "league:eredivisie": country = "the Netherlands"; break;
                case "league:brasileira": country = "Brazil"; break;
                case "league:espanhola":
                case "league:espanhola-2": country = "Spain"; break;
                default: country = LocalizationManager.Translate(league.Name); break;
            }
            var division = league.Tier == LeagueTier.First ? "First division" :
                league.Tier == LeagueTier.Second ? "Second division" :
                league.Tier == LeagueTier.Third ? "Third division" : "Fourth division";
            return division + " in " + country + ". " + league.ClubCount + " clubs.";
        }

        internal static void Header(Transform root, string title, string subtitle, UnityEngine.Events.UnityAction back)
        {
            var bar = BroadcastUi.Zone(root, "Header", 0.04f, 0.8f, 0.96f, 0.9f);
            var button = BroadcastUi.Button(bar, "Back", LocalizationManager.Translate("VOLTAR"), () => back(), new Color(1f, 1f, 1f, 0.1f), Color.white, 16);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0f, 0.15f, 0.12f, 0.85f);
            var heading = BroadcastUi.Label(bar, "Title", LocalizationManager.Translate(title), 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.14f, 0.42f, 0.7f, 0.95f);
            var sub = BroadcastUi.Label(bar, "Subtitle", LocalizationManager.Translate(subtitle), 16, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(sub.rectTransform, 0.14f, 0.05f, 0.96f, 0.42f);
        }
    }
}
