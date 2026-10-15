using CoachMaster.App;
using CoachMaster.Career;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MainMenuController : MonoBehaviour
    {
        void Start()
        {
            GameRoot.Require();
            GameRoot.EnsureUiInput();
            GameAudio.Music();
            var root = BroadcastUi.Mount(this);
            if (root == null)
                return;

            BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, "Simulador de treinador");

            var kicker = BroadcastUi.Label(root, "Kicker", "SIMULADOR DE TREINADOR", 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(kicker.rectTransform, 0.07f, 0.7f, 0.46f, 0.76f);
            var title = BroadcastUi.Label(root, "Title", "SIMULADOR\nDE TREINADOR", 64, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.07f, 0.4f, 0.48f, 0.7f);
            var tag = BroadcastUi.Label(root, "Tag", "Escolhe um clube, assume o banco e constrói a época. O jogo mostra o que se vê do banco: conversas, escolhas e o resultado.", 22, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(tag.rectTransform, 0.07f, 0.24f, 0.46f, 0.4f);

            var card = BroadcastUi.Panel(root, "MenuCard", new Color(0.08f, 0.03f, 0.16f, 0.55f), true);
            BroadcastUi.Place(card.rectTransform, 0.54f, 0.18f, 0.94f, 0.78f);
            var cardTitle = BroadcastUi.Label(card.transform, "CardTitle", "ENTRAR NO BANCO", 16, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(cardTitle.rectTransform, 0.08f, 0.84f, 0.9f, 0.94f);
            var cardBody = BroadcastUi.Label(card.transform, "CardBody", "Uma carreira nova começa pela liga. O clube, o plantel e o balneário aparecem antes de assinares.", 18, UiTheme.Muted, TextAnchor.UpperLeft);
            BroadcastUi.Place(cardBody.rectTransform, 0.08f, 0.8f, 0.9f, 0.84f);

            var start = BroadcastUi.Button(card.transform, "NovaCarreira", "NOVA CARREIRA", OpenCareer, UiTheme.Accent, UiTheme.ButtonText, 22);
            BroadcastUi.Place(start.GetComponent<RectTransform>(), 0.08f, 0.62f, 0.92f, 0.78f);

            var session = GameRoot.Instance.Session;
            var career = session.Career;
            if (career == null && CareerStore.Exists)
                career = CareerStore.TryLoad(session);
            if (career != null)
            {
                var resume = BroadcastUi.Button(card.transform, "Continuar", "CONTINUAR  ·  " + career.Club.ShortName, () => GameRoot.Instance.Open(SceneNames.CareerHub), new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
                BroadcastUi.Place(resume.GetComponent<RectTransform>(), 0.08f, 0.44f, 0.92f, 0.58f);
            }

            var settings = BroadcastUi.Button(card.transform, "Definicoes", "DEFINIÇÕES", () =>
            {
                var audio = GameRoot.Instance.GetComponent<AudioSettingsOverlay>();
                if (audio != null)
                    audio.Toggle();
            }, new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
            BroadcastUi.Place(settings.GetComponent<RectTransform>(), 0.08f, 0.26f, 0.92f, 0.4f);
            var quit = BroadcastUi.Button(card.transform, "Sair", "SAIR", LeaveGame, new Color(1f, 1f, 1f, 0.08f), Color.white, 18);
            BroadcastUi.Place(quit.GetComponent<RectTransform>(), 0.08f, 0.08f, 0.92f, 0.22f);

            var hint = BroadcastUi.Label(root, "ControlHint", "Clica para entrar     Enter  avançar     Esc  voltar", 15, UiTheme.Muted, TextAnchor.MiddleLeft);
            BroadcastUi.Place(hint.rectTransform, 0.07f, 0.008f, 0.7f, 0.05f);
        }

        static void OpenCareer()
        {
            GameRoot.Require().Open(SceneNames.LeagueSelect);
        }

        public static void LeaveGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
