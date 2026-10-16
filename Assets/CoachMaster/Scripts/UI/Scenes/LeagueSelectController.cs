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
            var leagues = new List<League>(session.Catalog.Leagues);
            leagues.RemoveAll(league => league.Tier == LeagueTier.Continental);
            for (var i = 0; i < leagues.Count; i++)
            {
                var league = leagues[i];
                var card = content.Find("League" + i);
                if (card == null)
                    continue;
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
        }

        void Paint(GameSession session, bool bake)
        {
            var root = bake ? BroadcastUi.CreateDesign(transform) : BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, "Escolha a liga");
            Header(root, "LIGAS", "Portugal, Inglaterra, França e Espanha. Cada país tem duas divisões.", () => GameRoot.Instance.Open(SceneNames.MainMenu));

            var scroll = BroadcastUi.Scroll(root, "Leagues", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.08f, 0.1f, 0.92f, 0.78f);
            var leagues = new List<League>(session.Catalog.Leagues);
            leagues.RemoveAll(league => league.Tier == LeagueTier.Continental);
            for (var i = 0; i < leagues.Count; i++)
            {
                var league = leagues[i];
                var card = BroadcastUi.Panel(content, "League" + i, new Color(0.1f, 0.04f, 0.2f, 0.72f), true);
                BroadcastUi.Preferred(card, 132);
                var art = LeagueBackdrop.First(league.Id);
                if (art != null)
                {
                    var photo = BroadcastUi.Solid(card.transform, "Art", Color.white, false);
                    BroadcastUi.Stretch(photo.rectTransform);
                    photo.sprite = art;
                    photo.preserveAspect = false;
                    photo.color = new Color(1f, 1f, 1f, 0.55f);
                }
                var tier = league.Tier == LeagueTier.First ? "1ª DIVISÃO" : "2ª DIVISÃO";
                var badge = BroadcastUi.Label(card.transform, "Tier", tier, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(badge.rectTransform, 0.04f, 0.68f, 0.4f, 0.9f);
                var count = BroadcastUi.Label(card.transform, "Count", league.ClubCount + " clubes", 14, UiTheme.Muted, TextAnchor.MiddleRight);
                BroadcastUi.Place(count.rectTransform, 0.55f, 0.68f, 0.72f, 0.9f);
                var name = BroadcastUi.Label(card.transform, "Name", league.Name.ToUpperInvariant(), 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.04f, 0.34f, 0.7f, 0.68f);
                var desc = BroadcastUi.Label(card.transform, "Desc", league.Description, 16, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(desc.rectTransform, 0.04f, 0.08f, 0.7f, 0.36f);
                System.Action choose = () =>
                {
                    session.Draft.LeagueId = league.Id;
                    session.Draft.ClubId = null;
                    session.PrepareLeague(league.Id);
                    GameRoot.Instance.Open(SceneNames.TeamSelect);
                };
                BroadcastUi.Hit(card, choose);
                var pick = BroadcastUi.Button(card.transform, "Select", "ESCOLHER", choose, UiTheme.Accent, UiTheme.ButtonText, 16);
                BroadcastUi.Place(pick.GetComponent<RectTransform>(), 0.74f, 0.28f, 0.96f, 0.72f);
            }

            var hint = BroadcastUi.Label(root, "ControlHint", "Enter  escolher     Esc  voltar     Setas  navegar", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.08f, 0.008f, 0.8f, 0.05f);
        }

        internal static void Header(Transform root, string title, string subtitle, UnityEngine.Events.UnityAction back)
        {
            var bar = BroadcastUi.Zone(root, "Header", 0.04f, 0.8f, 0.96f, 0.9f);
            var button = BroadcastUi.Button(bar, "Back", "VOLTAR", () => back(), new Color(1f, 1f, 1f, 0.1f), Color.white, 16);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0f, 0.15f, 0.12f, 0.85f);
            var heading = BroadcastUi.Label(bar, "Title", title, 32, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(heading.rectTransform, 0.14f, 0.42f, 0.7f, 0.95f);
            var sub = BroadcastUi.Label(bar, "Subtitle", subtitle, 16, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(sub.rectTransform, 0.14f, 0.05f, 0.96f, 0.42f);
        }
    }
}
