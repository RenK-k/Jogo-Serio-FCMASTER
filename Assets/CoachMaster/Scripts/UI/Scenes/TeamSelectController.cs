using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class TeamSelectController : MonoBehaviour
    {
        Club _selected;
        Image _kit;
        Image _crest;
        Text _name;
        Text _meta;
        Text _reputation;
        Text _squad;
        readonly List<Image> _cards = new List<Image>();
        readonly List<Club> _clubs = new List<Club>();

        void Start()
        {
            LocalizationManager.ApplyScene();
            var session = GameRoot.Require().Session;
            if (string.IsNullOrEmpty(session.Draft.LeagueId))
            {
                GameRoot.Instance.Open(SceneNames.LeagueSelect);
                return;
            }

            session.PrepareLeague(session.Draft.LeagueId);
            if (transform.Find("Design") != null)
            {
                BindClubs(BroadcastUi.Mount(this), session);
                return;
            }

            Paint(session, false);
        }

        public void BakeDesign(GameSession session)
        {
            if (string.IsNullOrEmpty(session.Draft.LeagueId))
                session.Draft.LeagueId = Ids.League("atlas");
            session.PrepareLeague(session.Draft.LeagueId);
            Paint(session, true);
        }

        void BindClubs(Transform root, GameSession session)
        {
            SceneBind.Wire(root.Find("Header/Back"), () => GameRoot.Instance.Open(SceneNames.LeagueSelect));
            _crest = SceneBind.Get<Image>(root, "Preview/Crest");
            _kit = SceneBind.Get<Image>(root, "Preview/Kit");
            if (_kit != null)
                _kit.gameObject.SetActive(false);
            _name = SceneBind.Get<Text>(root, "Preview/ClubName");
            _meta = SceneBind.Get<Text>(root, "Preview/Meta");
            _reputation = SceneBind.Get<Text>(root, "Preview/Reputation");
            _squad = SceneBind.Get<Text>(root, "Preview/Squad");
            SceneBind.Wire(root.Find("Preview/Choose"), () =>
            {
                if (_selected == null)
                    return;
                session.Draft.ClubId = _selected.Id;
                GameRoot.Instance.Open(SceneNames.ClubInfo);
            });
            var content = root.Find("Clubs/Content");
            for (var i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
            foreach (var club in session.Catalog.ClubsInLeague(session.Draft.LeagueId))
                _clubs.Add(club);
            _clubs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));
            for (var i = 0; i < _clubs.Count; i++)
            {
                var club = _clubs[i];
                var index = i;
                var card = BroadcastUi.Panel(content, "Club" + i, new Color(0.1f, 0.04f, 0.2f, 0.72f), true);
                BroadcastUi.Preferred(card, 118);
                _cards.Add(card);
                var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
                var crestRect = crestGo.GetComponent<RectTransform>();
                crestRect.SetParent(card.transform, false);
                crestRect.anchorMin = new Vector2(0.03f, 0.52f);
                crestRect.anchorMax = new Vector2(0.14f, 0.92f);
                crestRect.offsetMin = Vector2.zero;
                crestRect.offsetMax = Vector2.zero;
                var crest = crestGo.GetComponent<Image>();
                crest.raycastTarget = false;
                crest.preserveAspect = true;
                crest.sprite = CrestStudio.For(club);
                var name = BroadcastUi.Label(card.transform, "Name", club.Name.ToUpperInvariant(), 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.17f, 0.48f, 0.96f, 0.9f);
                var meta = BroadcastUi.Label(card.transform, "Meta", LocalizationManager.Translate(club.City) + "   ·   " + LocalizationManager.Translate(club.Stadium), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(meta.rectTransform, 0.17f, 0.1f, 0.96f, 0.48f);
                BroadcastUi.Hit(card, () =>
                {
                    if (_selected == club)
                    {
                        session.Draft.ClubId = club.Id;
                        GameRoot.Instance.Open(SceneNames.ClubInfo);
                        return;
                    }

                    Select(index);
                });
            }

            if (_clubs.Count > 0)
                Select(0);
        }

        void Paint(GameSession session, bool bake)
        {
            var root = bake ? BroadcastUi.CreateDesign(transform) : BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            var league = LeagueName(session);
            BroadcastUi.Wordmark(root, league);
            var subtitle = LocalizationManager.Current == GameLanguage.English
                ? league + "  ·  choose the club you want to manage."
                : league + "  ·  escolhe o banco que vais assumir.";
            LeagueSelectController.Header(root, "CLUBES", subtitle, () => GameRoot.Instance.Open(SceneNames.LeagueSelect));

            foreach (var club in session.Catalog.ClubsInLeague(session.Draft.LeagueId))
                _clubs.Add(club);
            _clubs.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

            var scroll = BroadcastUi.Scroll(root, "Clubs", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.03f, 0.1f, 0.5f, 0.78f);
            for (var i = 0; i < _clubs.Count; i++)
            {
                var club = _clubs[i];
                var index = i;
                var card = BroadcastUi.Panel(content, "Club" + i, new Color(0.1f, 0.04f, 0.2f, 0.72f), true);
                BroadcastUi.Preferred(card, 118);
                _cards.Add(card);
                var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
                var crestRect = crestGo.GetComponent<RectTransform>();
                crestRect.SetParent(card.transform, false);
                crestRect.anchorMin = new Vector2(0.03f, 0.52f);
                crestRect.anchorMax = new Vector2(0.14f, 0.92f);
                crestRect.offsetMin = Vector2.zero;
                crestRect.offsetMax = Vector2.zero;
                var crest = crestGo.GetComponent<Image>();
                crest.raycastTarget = false;
                crest.preserveAspect = true;
                crest.sprite = CrestStudio.For(club);
                var name = BroadcastUi.Label(card.transform, "Name", club.Name.ToUpperInvariant(), 20, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                BroadcastUi.Place(name.rectTransform, 0.17f, 0.48f, 0.96f, 0.9f);
                var meta = BroadcastUi.Label(card.transform, "Meta", LocalizationManager.Translate(club.City) + "   ·   " + LocalizationManager.Translate(club.Stadium), 14, UiTheme.Muted, TextAnchor.MiddleLeft);
                BroadcastUi.Place(meta.rectTransform, 0.17f, 0.1f, 0.96f, 0.48f);
                BroadcastUi.Hit(card, () =>
                {
                    if (_selected == club)
                    {
                        session.Draft.ClubId = club.Id;
                        GameRoot.Instance.Open(SceneNames.ClubInfo);
                        return;
                    }

                    Select(index);
                });
            }

            var preview = BroadcastUi.Panel(root, "Preview", new Color(0.08f, 0.03f, 0.16f, 0.62f), true);
            BroadcastUi.Place(preview.rectTransform, 0.53f, 0.1f, 0.97f, 0.78f);
            var crestSlot = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            var crestSlotRect = crestSlot.GetComponent<RectTransform>();
            crestSlotRect.SetParent(preview.transform, false);
            crestSlotRect.anchorMin = new Vector2(0.1f, 0.72f);
            crestSlotRect.anchorMax = new Vector2(0.36f, 0.94f);
            crestSlotRect.offsetMin = Vector2.zero;
            crestSlotRect.offsetMax = Vector2.zero;
            _crest = crestSlot.GetComponent<Image>();
            _crest.preserveAspect = true;
            _crest.raycastTarget = false;
            _kit = null;
            _name = BroadcastUi.Label(preview.transform, "ClubName", "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_name.rectTransform, 0.42f, 0.78f, 0.94f, 0.94f);
            _meta = BroadcastUi.Label(preview.transform, "Meta", "", 16, UiTheme.Accent, TextAnchor.UpperLeft);
            BroadcastUi.Place(_meta.rectTransform, 0.42f, 0.62f, 0.94f, 0.78f);
            _reputation = BroadcastUi.Label(preview.transform, "Reputation", "", 16, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(_reputation.rectTransform, 0.42f, 0.34f, 0.94f, 0.58f);
            _squad = BroadcastUi.Label(preview.transform, "Squad", "", 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(_squad.rectTransform, 0.42f, 0.18f, 0.94f, 0.34f);
            var choose = BroadcastUi.Button(preview.transform, "Choose", LocalizationManager.Translate("VER CLUBE"), () =>
            {
                if (_selected == null)
                    return;
                session.Draft.ClubId = _selected.Id;
                GameRoot.Instance.Open(SceneNames.ClubInfo);
            }, UiTheme.Accent, UiTheme.ButtonText, 18);
            BroadcastUi.Place(choose.GetComponent<RectTransform>(), 0.08f, 0.04f, 0.92f, 0.15f);

            if (_clubs.Count > 0)
                Select(0);

            var hint = BroadcastUi.Label(root, "ControlHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.04f, 0.008f, 0.7f, 0.05f);
        }

        void Select(int index)
        {
            _selected = _clubs[index];
            _crest.sprite = CrestStudio.For(_selected);
            _name.text = _selected.Name.ToUpperInvariant();
            _meta.text = LocalizationManager.Translate(_selected.City) + "\n" + LocalizationManager.Translate(_selected.Stadium) + "   ·   " + _selected.FoundedYear;
            _reputation.text = ClubReputation(_selected);
            _squad.text = LocalizationManager.Translate(_selected.Tactics.Summary);
            for (var i = 0; i < _cards.Count; i++)
                _cards[i].color = i == index ? new Color(0.22f, 0.1f, 0.4f, 0.95f) : new Color(0.1f, 0.04f, 0.2f, 0.72f);
        }

        static string LeagueName(GameSession session)
        {
            foreach (var league in session.Catalog.Leagues)
            {
                if (league.Id == session.Draft.LeagueId)
                    return LocalizationManager.Translate(league.Name);
            }

            return "Liga";
        }

        static string ClubReputation(Club club)
        {
            if (LocalizationManager.Current != GameLanguage.English)
                return club.PublicReputation;
            if (club.Reputation >= 78) return "A leading club with high expectations.";
            if (club.Reputation >= 66) return "An established top-flight side.";
            if (club.Reputation >= 52) return "A competitive club with a solid reputation.";
            if (club.Reputation >= 38) return "A developing club aiming to move up the divisions.";
            return "A modest club working to establish itself.";
        }
    }
}
