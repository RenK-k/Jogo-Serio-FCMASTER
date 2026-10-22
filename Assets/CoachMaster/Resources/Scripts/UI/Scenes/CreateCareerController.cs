using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class CreateCareerController : MonoBehaviour
    {
        void Start()
        {
            LocalizationManager.ApplyScene();
            var session = GameRoot.Require().Session;
            if (string.IsNullOrEmpty(session.Draft.ClubId) || !session.Catalog.Clubs.ContainsKey(session.Draft.ClubId))
            {
                GameRoot.Instance.Open(SceneNames.ClubInfo);
                return;
            }

            var club = session.Catalog.Clubs[session.Draft.ClubId];
            if (transform.Find("Design") != null)
            {
                BindName(BroadcastUi.Mount(this), session, club);
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

        void BindName(Transform root, GameSession session, Club club)
        {
            SceneBind.Wire(root.Find("Header/Back"), () => GameRoot.Instance.Open(SceneNames.ClubInfo));
            var title = SceneBind.Get<Text>(root, "Card/Club");
            if (title != null)
                title.text = club.Name.ToUpperInvariant();
            var crest = SceneBind.Get<Image>(root, "Card/Crest");
            if (crest != null)
                crest.sprite = CrestStudio.For(club);
            var field = SceneBind.Get<InputField>(root, "Card/Input");
            var error = SceneBind.Get<Text>(root, "Card/Error");
            if (field != null && !string.IsNullOrEmpty(session.Draft.CoachName))
                field.text = session.Draft.CoachName;
            SceneBind.Wire(root.Find("Card/Confirm"), () => ConfirmName(session, field, error));
            if (field != null)
            {
                field.onSubmit.RemoveAllListeners();
                field.onEndEdit.RemoveAllListeners();
                field.onSubmit.AddListener(text => ConfirmName(session, field, error));
            }
        }

        void ConfirmName(GameSession session, InputField field, Text error)
        {
            var name = field == null || field.text == null ? "" : field.text.Trim();
            if (name.Length < 2)
            {
                if (error != null)
                    error.text = "Escreve o nome do treinador.";
                return;
            }

            session.Draft.CoachName = name;
            session.StartCareer();
            CareerStore.Save(session.Career);
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void Paint(GameSession session, Club club, bool bake)
        {
            var root = bake ? BroadcastUi.CreateDesign(transform) : BroadcastUi.Mount(this);
            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, "Novo treinador");
            LeagueSelectController.Header(root, "O TEU NOME", "Vais assumir o " + club.Name + ".", () => GameRoot.Instance.Open(SceneNames.ClubInfo));

            var card = BroadcastUi.Panel(root, "Card", new Color(0.08f, 0.03f, 0.16f, 0.7f), true);
            BroadcastUi.Place(card.rectTransform, 0.28f, 0.16f, 0.72f, 0.78f);
            var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            var crestRect = crestGo.GetComponent<RectTransform>();
            crestRect.SetParent(card.transform, false);
            crestRect.anchorMin = new Vector2(0.5f, 0.68f);
            crestRect.anchorMax = new Vector2(0.5f, 0.68f);
            crestRect.sizeDelta = new Vector2(120, 120);
            var crest = crestGo.GetComponent<Image>();
            crest.preserveAspect = true;
            crest.raycastTarget = false;
            crest.sprite = CrestStudio.For(club);
            var clubName = BroadcastUi.Label(card.transform, "Club", club.Name.ToUpperInvariant(), 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(clubName.rectTransform, 0.08f, 0.5f, 0.92f, 0.58f);
            var prompt = BroadcastUi.Label(card.transform, "Prompt", "Como te vão tratar no balneário?", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(prompt.rectTransform, 0.08f, 0.42f, 0.92f, 0.5f);

            var field = UiFactory.Input(card.transform, "Nome do treinador");
            BroadcastUi.Place(field.GetComponent<RectTransform>(), 0.1f, 0.28f, 0.9f, 0.4f);
            if (!string.IsNullOrEmpty(session.Draft.CoachName))
                field.text = session.Draft.CoachName;
            var error = BroadcastUi.Label(card.transform, "Error", "", 15, UiTheme.Danger, TextAnchor.MiddleCenter);
            BroadcastUi.Place(error.rectTransform, 0.1f, 0.2f, 0.9f, 0.27f);
            var confirm = BroadcastUi.Button(card.transform, "Confirm", "COMEÇAR ÉPOCA", () =>
            {
                var name = field.text == null ? "" : field.text.Trim();
                if (name.Length < 2)
                {
                    error.text = "Escreve o nome do treinador.";
                    return;
                }

                session.Draft.CoachName = name;
                session.StartCareer();
                CareerStore.Save(session.Career);
                GameRoot.Instance.Open(SceneNames.CareerHub);
            }, UiTheme.Accent, UiTheme.ButtonText, 20);
            BroadcastUi.Place(confirm.GetComponent<RectTransform>(), 0.18f, 0.06f, 0.82f, 0.18f);
            field.onSubmit.AddListener(text => confirm.onClick.Invoke());
            field.onEndEdit.AddListener(text =>
            {
                if (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
                    confirm.onClick.Invoke();
            });

            var hint = BroadcastUi.Label(root, "SettingsHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.06f, 0.008f, 0.6f, 0.05f);
        }
    }
}
