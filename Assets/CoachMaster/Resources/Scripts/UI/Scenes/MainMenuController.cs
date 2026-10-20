using CoachMaster.App;
using CoachMaster.Career;
using UnityEngine;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MainMenuController : MonoBehaviour, IControlBackHandler
    {
        Text _continueLabel;

        void Start()
        {
            GameRoot.Require();
            GameRoot.EnsureUiInput();
            GameAudio.Music();
            LocalizationManager.LanguageChanged += RefreshContinueLabel;
            if (transform.Find("Design") != null)
            {
                BindMenu(BroadcastUi.Mount(this));
                LocalizationManager.ApplyScene();
                RefreshContinueLabel();
                LocalizationManager.ShowFirstRunPrompt(this);
                return;
            }

            BuildMenu(BroadcastUi.Mount(this), GameRoot.Instance.Session);
            LocalizationManager.ApplyScene();
            RefreshContinueLabel();
            LocalizationManager.ShowFirstRunPrompt(this);
        }

        void OnDestroy()
        {
            LocalizationManager.LanguageChanged -= RefreshContinueLabel;
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
            if (HasSavedCareers(session))
            {
                if (resume == null)
                {
                    var button = BroadcastUi.Button(root.Find("MenuCard"), "Continuar", CareerPickerLabel(), OpenCareerPicker, new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
                    BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.08f, 0.36f, 0.92f, 0.48f);
                    _continueLabel = button.GetComponentInChildren<Text>();
                }
                else
                {
                    resume.gameObject.SetActive(true);
                    _continueLabel = resume.GetComponentInChildren<Text>();
                    SceneBind.Wire(resume, OpenCareerPicker);
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
            if (HasSavedCareers(session))
            {
                var resume = BroadcastUi.Button(card.transform, "Continuar", CareerPickerLabel(), OpenCareerPicker, new Color(1f, 1f, 1f, 0.12f), Color.white, 18);
                BroadcastUi.Place(resume.GetComponent<RectTransform>(), 0.08f, 0.36f, 0.92f, 0.48f);
                _continueLabel = resume.GetComponentInChildren<Text>();
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

        static bool HasSavedCareers(GameSession session)
        {
            if (session != null && session.Career != null)
                return true;
            if (CareerStore.Exists || CareerStore.ListNamedSaves().Count > 0)
                return true;
            for (var slot = 1; slot <= 5; slot++)
                if (CareerStore.HasAutoSave(slot))
                    return true;
            return false;
        }

        static string CareerPickerLabel()
        {
            return LocalizationManager.Current == GameLanguage.English ? "CONTINUE" : "CONTINUAR";
        }

        void RefreshContinueLabel()
        {
            if (_continueLabel != null)
                _continueLabel.text = CareerPickerLabel();
        }

        void OpenCareerPicker()
        {
            var root = BroadcastUi.Mount(this);
            if (root == null)
                return;
            var previous = root.Find("CareerPicker");
            if (previous != null)
                Destroy(previous.gameObject);

            var overlay = BroadcastUi.Zone(root, "CareerPicker", 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(overlay, "Dim", new Color(0.01f, 0.005f, 0.03f, 0.84f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Hit(dim, () => Destroy(overlay.gameObject));
            var card = BroadcastUi.Panel(overlay, "Card", new Color(0.1f, 0.04f, 0.2f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.16f, 0.08f, 0.84f, 0.92f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(card.transform, "Title", english ? "CHOOSE A CAREER" : "ESCOLHE UMA CARREIRA", 22, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.05f, 0.91f, 0.82f, 0.99f);
            var scroll = BroadcastUi.Scroll(card.transform, "CareerList", out var content);
            BroadcastUi.Place(scroll.GetComponent<RectTransform>(), 0.045f, 0.06f, 0.955f, 0.89f);

            var session = GameRoot.Instance.Session;
            var current = session.Career;
            if (current == null && CareerStore.Exists)
                current = CareerStore.TryLoad(session);
            var count = 0;
            if (current != null)
            {
                AddCareerButton(content, english ? "CONTINUE CURRENT  ·  " : "CONTINUAR A ATUAL  ·  ", current.Club.ShortName + "  ·  " + current.Coach.Name,
                    () => GameRoot.Instance.Open(SceneNames.CareerHub), ref count);
            }

            for (var slot = 1; slot <= 5; slot++)
            {
                if (!CareerStore.HasAutoSave(slot))
                    continue;
                var capturedSlot = slot;
                var label = CareerStore.AutoSaveLabel(slot, current == null ? session.Catalog : current.World);
                if (english)
                    label = label.Replace("CÓPIA", "SAVE").Replace("VAZIA", "EMPTY").Replace("GRAVAÇÃO ANTIGA", "OLD SAVE").Replace("data antiga", "old save date");
                AddCareerButton(content, (english ? "AUTOSAVE " : "AUTOSAVE ") + slot + "  ·  ", label,
                    () => LoadAutoSave(capturedSlot, overlay.gameObject), ref count);
            }

            var saves = CareerStore.ListNamedSaves();
            for (var i = 0; i < saves.Count; i++)
            {
                var save = saves[i];
                var clubName = save.ClubId;
                if (current != null && current.World.Clubs.TryGetValue(save.ClubId, out var club))
                    clubName = club.ShortName;
                var label = save.Name + "  ·  " + clubName + "  ·  " + save.CoachName + "  ·  " + save.SavedAt.ToString("dd/MM/yyyy");
                AddCareerButton(content, english ? "SAVED CAREER  ·  " : "CARREIRA GUARDADA  ·  ", label,
                    () => LoadNamedCareer(save.Id, overlay.gameObject), ref count);
            }

            if (count == 0)
            {
                var empty = BroadcastUi.Label(content, "Empty", english ? "No saved careers yet." : "Ainda não há carreiras guardadas.", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 54f;
            }
        }

        public bool HandleControlBack()
        {
            var picker = transform.Find("Design/CareerPicker") ?? transform.Find("CareerPicker");
            if (picker == null)
                return false;
            Destroy(picker.gameObject);
            return true;
        }

        static void AddCareerButton(Transform content, string prefix, string details, System.Action action, ref int count)
        {
            var button = BroadcastUi.Button(content, "CareerOption" + count, prefix + details, action,
                new Color(1f, 1f, 1f, 0.1f), Color.white, 14);
            button.GetComponentInChildren<Text>().resizeTextForBestFit = true;
            button.GetComponentInChildren<Text>().resizeTextMinSize = 10;
            button.GetComponentInChildren<Text>().resizeTextMaxSize = 14;
            button.gameObject.AddComponent<LayoutElement>().preferredHeight = 58f;
            count++;
        }

        static void LoadAutoSave(int slot, GameObject overlay)
        {
            var root = GameRoot.Instance;
            var career = root == null ? null : CareerStore.TryLoadAutoSave(root.Session, slot);
            LoadSelectedCareer(career, overlay);
        }

        static void LoadNamedCareer(string id, GameObject overlay)
        {
            var root = GameRoot.Instance;
            var career = root == null ? null : CareerStore.TryLoadNamed(root.Session, id);
            LoadSelectedCareer(career, overlay);
        }

        static void LoadSelectedCareer(CareerState career, GameObject overlay)
        {
            if (career == null || GameRoot.Instance == null)
                return;
            GameRoot.Instance.Session.Restore(career);
            GameRoot.Instance.LiveMatch = null;
            CareerStore.TrySave(career);
            if (overlay != null)
                Destroy(overlay);
            GameRoot.Instance.Open(SceneNames.CareerHub);
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
