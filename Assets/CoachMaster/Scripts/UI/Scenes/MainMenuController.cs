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
            if (transform.Find("Design") != null)
            {
                BindMenu(BroadcastUi.Mount(this));
                LocalizationManager.ApplyScene();
                LocalizationManager.ShowFirstRunPrompt(this);
                return;
            }

            BuildMenu(BroadcastUi.Mount(this), GameRoot.Instance.Session);
            LocalizationManager.ApplyScene();
            LocalizationManager.ShowFirstRunPrompt(this);
        }

        public void BakeDesign(GameSession session)
        {
            BuildMenu(BroadcastUi.CreateDesign(transform), session);
        }

        void BindMenu(Transform root)
        {
            Hide(root.Find("Tag"));
            Hide(root.Find("MenuCard/CardTitle"));
            Hide(root.Find("MenuCard/CardBody"));
            var session = GameRoot.Instance.Session;
            SceneBind.Wire(root.Find("MenuCard/NovaCarreira"), OpenCareer);
            SceneBind.Wire(root.Find("MenuCard/Definicoes"), OpenSettings);
            SceneBind.Wire(root.Find("MenuCard/Sair"), LeaveGame);
            var career = session.Career;
            if (career == null && CareerStore.Exists)
                career = CareerStore.TryLoad(session);
            var resume = root.Find("MenuCard/Continuar");
            if (career != null)
            {
                if (resume == null)
                {
                    var button = BroadcastUi.Button(root.Find("MenuCard"), "Continuar", "CONTINUAR  ·  " + career.Club.ShortName, () => GameRoot.Instance.Open(SceneNames.CareerHub), new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
                    BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.08f, 0.36f, 0.92f, 0.48f);
                }
                else
                {
                    resume.gameObject.SetActive(true);
                    resume.GetComponentInChildren<Text>().text = "CONTINUAR  ·  " + career.Club.ShortName;
                    SceneBind.Wire(resume, () => GameRoot.Instance.Open(SceneNames.CareerHub));
                }
            }
            else if (resume != null)
                resume.gameObject.SetActive(false);
        }

        static void Hide(Transform item)
        {
            if (item != null)
                item.gameObject.SetActive(false);
        }

        void OpenSettings()
        {
            var audio = GameRoot.Instance.GetComponent<AudioSettingsOverlay>();
            if (audio != null)
                audio.Toggle();
        }

        void BuildMenu(Transform root, GameSession session)
        {
            if (root == null)
                return;

            BroadcastUi.Backdrop(root);

            var titleShade = BroadcastUi.Label(root, "TitleShade", "SIMULADOR\nDE TREINADOR", 64, new Color(0f, 0f, 0f, 0.45f), TextAnchor.UpperLeft, FontStyle.Bold);
            BroadcastUi.Place(titleShade.rectTransform, 0.078f, 0.38f, 0.49f, 0.7f);
            var title = BroadcastUi.Label(root, "Title", "SIMULADOR\nDE TREINADOR", 64, Color.white, TextAnchor.UpperLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.07f, 0.4f, 0.48f, 0.72f);
            var shadow = BroadcastUi.Panel(root, "MenuShadow", new Color(0f, 0f, 0f, 0.55f), false);
            BroadcastUi.Place(shadow.rectTransform, 0.565f, 0.15f, 0.965f, 0.74f);
            var card = BroadcastUi.Panel(root, "MenuCard", new Color(0.1f, 0.04f, 0.2f, 0.92f), true);
            BroadcastUi.Place(card.rectTransform, 0.54f, 0.18f, 0.94f, 0.78f);
            var lip = BroadcastUi.Solid(card.transform, "Lip", new Color(1f, 1f, 1f, 0.16f), false);
            BroadcastUi.Place(lip.rectTransform, 0f, 0.94f, 1f, 1f);
            var start = BroadcastUi.Button(card.transform, "NovaCarreira", "NOVA CARREIRA", OpenCareer, UiTheme.Accent, UiTheme.ButtonText, 22);
            BroadcastUi.Place(start.GetComponent<RectTransform>(), 0.08f, 0.52f, 0.92f, 0.66f);

            var career = session.Career;
            if (career == null && CareerStore.Exists)
                career = CareerStore.TryLoad(session);
            if (career != null)
            {
                var resume = BroadcastUi.Button(card.transform, "Continuar", "CONTINUAR  ·  " + career.Club.ShortName, () => GameRoot.Instance.Open(SceneNames.CareerHub), new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
                BroadcastUi.Place(resume.GetComponent<RectTransform>(), 0.08f, 0.36f, 0.92f, 0.48f);
            }

            var settings = BroadcastUi.Button(card.transform, "Definicoes", "DEFINIÇÕES", OpenSettings, new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
            BroadcastUi.Place(settings.GetComponent<RectTransform>(), 0.08f, 0.2f, 0.92f, 0.32f);
            var quit = BroadcastUi.Button(card.transform, "Sair", "SAIR", LeaveGame, new Color(1f, 1f, 1f, 0.08f), Color.white, 18);
            BroadcastUi.Place(quit.GetComponent<RectTransform>(), 0.08f, 0.05f, 0.92f, 0.16f);

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
