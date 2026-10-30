using System;
using CoachMaster.App;
using CoachMaster.Career;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CoachMaster.UI.Scenes
{
    public sealed class MainMenuController : MonoBehaviour, IControlBackHandler
    {
        Text _continueLabel;
        VideoPlayer _film;
        RenderTexture _filmTexture;
        Action _pendingExit;
        float _exitStarted;

        void Start()
        {
            GameRoot.Require();
            GameRoot.EnsureUiInput();
            GameAudio.Music();
            LocalizationManager.LanguageChanged += RefreshContinueLabel;
            if (transform.Find("Design") != null)
            {
                var mounted = BroadcastUi.Mount(this);
                BindMenu(mounted);
                MenuFigures.Ensure(mounted);
                ShowMenuFilm(mounted);
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
            if (_film != null)
            {
                _film.loopPointReached -= OnFilmEnded;
                _film.Stop();
            }
            if (_filmTexture != null)
            {
                _filmTexture.Release();
                Destroy(_filmTexture);
                _filmTexture = null;
            }
        }

        void Update()
        {
            if (_pendingExit == null || _film == null || _film.clip == null)
                return;
            var length = (float)_film.length;
            if (length > 0.2f && Time.unscaledTime >= _exitStarted + length + 0.2f)
                FinishExit();
            else if (length <= 0.2f && Time.unscaledTime >= _exitStarted + 2.5f)
                FinishExit();
        }

        void ShowMenuFilm(Transform root)
        {
            if (!Application.isPlaying || root == null)
                return;
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                var name = child.name;
                if (name == "MenuCard" || name == "MenuVideo" || name == "CareerPicker")
                    continue;
                child.gameObject.SetActive(false);
            }
            EnsureFilm(root);
            var intro = Resources.Load<VideoClip>("INTRO");
            if (intro != null)
                PlayFilm(intro, true);
            var card = root.Find("MenuCard");
            if (card != null)
                card.SetAsLastSibling();
        }

        void EnsureFilm(Transform root)
        {
            if (_film != null)
                return;
            var existing = root.Find("MenuVideo");
            GameObject go;
            if (existing == null)
            {
                go = new GameObject("MenuVideo", typeof(RectTransform), typeof(RawImage), typeof(VideoPlayer));
                go.transform.SetParent(root, false);
            }
            else
                go = existing.gameObject;
            go.transform.SetAsFirstSibling();
            var image = go.GetComponent<RawImage>();
            if (image == null)
                image = go.AddComponent<RawImage>();
            var rect = image.rectTransform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            image.raycastTarget = false;
            image.color = Color.white;
            if (_filmTexture == null)
                _filmTexture = new RenderTexture(1920, 1080, 0);
            image.texture = _filmTexture;
            _film = go.GetComponent<VideoPlayer>();
            if (_film == null)
                _film = go.AddComponent<VideoPlayer>();
            _film.playOnAwake = false;
            _film.renderMode = VideoRenderMode.RenderTexture;
            _film.targetTexture = _filmTexture;
            _film.aspectRatio = VideoAspectRatio.Stretch;
            _film.audioOutputMode = VideoAudioOutputMode.Direct;
            _film.isLooping = true;
        }

        void PlayFilm(VideoClip clip, bool loop)
        {
            if (_film == null || clip == null)
                return;
            _film.loopPointReached -= OnFilmEnded;
            _film.Stop();
            _film.clip = clip;
            _film.isLooping = loop;
            if (!loop)
                _film.loopPointReached += OnFilmEnded;
            _film.Play();
        }

        void OnFilmEnded(VideoPlayer player)
        {
            FinishExit();
        }

        void BeginExit(Action next)
        {
            if (_pendingExit != null || next == null)
                return;
            var root = BroadcastUi.Mount(this);
            if (root != null)
            {
                Hide(root.Find("MenuCard"));
                Hide(root.Find("CareerPicker"));
                Hide(root.Find("GameLogo"));
                EnsureFilm(root);
                var film = root.Find("MenuVideo");
                if (film != null)
                    film.SetAsLastSibling();
            }
            _pendingExit = next;
            _exitStarted = Time.unscaledTime;
            var outro = Resources.Load<VideoClip>("saida");
            if (outro == null || _film == null)
            {
                FinishExit();
                return;
            }
            PlayFilm(outro, false);
        }

        void FinishExit()
        {
            if (_pendingExit == null)
                return;
            var next = _pendingExit;
            _pendingExit = null;
            if (_film != null)
                _film.loopPointReached -= OnFilmEnded;
            next();
        }

        void ExitToLeague()
        {
            BeginExit(() => GameRoot.Require().Open(SceneNames.LeagueSelect));
        }

        void ExitGame()
        {
            BeginExit(LeaveGame);
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
            SceneBind.Wire(root.Find("MenuCard/NovaCarreira"), ExitToLeague);
            SceneBind.Wire(root.Find("MenuCard/Definicoes"), OpenSettings);
            SceneBind.Wire(root.Find("MenuCard/Sair"), ExitGame);
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
            DressMenu(root);
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
            MenuFigures.Ensure(root);
            EnsureMenuLogo(root);

            var card = BroadcastUi.Panel(root, "MenuCard", new Color(0f, 0f, 0f, 0f), false);
            var cardImage = card.GetComponent<Image>();
            if (cardImage != null)
                cardImage.raycastTarget = false;
            BroadcastUi.Place(card.rectTransform, 0.58f, 0.26f, 0.94f, 0.74f);
            var start = BroadcastUi.Button(card.transform, "NovaCarreira", "NOVA CARREIRA", ExitToLeague, new Color(1f, 1f, 1f, 0.12f), Color.white, 22);
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
            var quit = BroadcastUi.Button(card.transform, "Sair", "SAIR", ExitGame, new Color(1f, 1f, 1f, 0.08f), Color.white, 18);
            BroadcastUi.Place(quit.GetComponent<RectTransform>(), 0.08f, 0.05f, 0.92f, 0.16f);
            DressMenu(root);
            if (Application.isPlaying)
                ShowMenuFilm(root);
        }

        static void DressMenu(Transform root)
        {
            Hide(root.Find("Title"));
            Hide(root.Find("TitleShade"));
            Hide(root.Find("MenuShadow"));
            Hide(root.Find("MenuCard/Lip"));
            Hide(root.Find("ControlHint"));
            EnsureMenuLogo(root);
            var card = root.Find("MenuCard") as RectTransform;
            if (card == null)
                return;
            var image = card.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color(0f, 0f, 0f, 0f);
                image.raycastTarget = false;
            }
            BroadcastUi.Place(card, 0.58f, 0.26f, 0.94f, 0.74f);
            var names = new[] { "NovaCarreira", "Continuar", "Definicoes", "Sair" };
            var shown = new System.Collections.Generic.List<RectTransform>();
            for (var i = 0; i < names.Length; i++)
            {
                var item = card.Find(names[i]) as RectTransform;
                if (item != null && item.gameObject.activeSelf)
                    shown.Add(item);
            }
            if (shown.Count == 0)
                return;
            var gap = 0.05f;
            var slot = (1f - gap * (shown.Count + 1)) / shown.Count;
            var height = slot * 0.76f;
            var pad = (slot - height) * 0.5f;
            for (var i = 0; i < shown.Count; i++)
            {
                var top = 1f - gap - i * (slot + gap);
                BroadcastUi.Place(shown[i], 0.1f, top - slot + pad, 0.9f, top - pad);
                var motion = shown[i].GetComponent<UiButtonMotion>();
                if (motion == null)
                    shown[i].gameObject.AddComponent<UiButtonMotion>();
            }
        }

        static void EnsureMenuLogo(Transform root)
        {
            if (root == null)
                return;
            var existing = root.Find("GameLogo");
            RawImage image;
            if (existing == null)
            {
                var go = new GameObject("GameLogo", typeof(RectTransform), typeof(RawImage));
                go.transform.SetParent(root, false);
                image = go.GetComponent<RawImage>();
            }
            else
                image = existing.GetComponent<RawImage>();
            if (image == null)
                return;
            var rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.07f, 0.56f);
            rect.pivot = new Vector2(0f, 0.5f);
            BroadcastUi.FitBrand(image, 640f, 240f);
            image.transform.SetAsLastSibling();
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
            var dim = BroadcastUi.Solid(overlay, "Dim", new Color(0f, 0f, 0f, 0f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Catch(dim, () => Destroy(overlay.gameObject));
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
                    () => BeginExit(() => GameRoot.Instance.Open(SceneNames.CareerHub)), ref count);
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
                    () => ExitToAutoSave(capturedSlot, overlay.gameObject), ref count);
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
                    () => ExitToNamedCareer(save.Id, overlay.gameObject), ref count);
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

        void ExitToAutoSave(int slot, GameObject overlay)
        {
            var root = GameRoot.Instance;
            var career = root == null ? null : CareerStore.TryLoadAutoSave(root.Session, slot);
            if (career == null)
                return;
            BeginExit(() => LoadSelectedCareer(career, overlay));
        }

        void ExitToNamedCareer(string id, GameObject overlay)
        {
            var root = GameRoot.Instance;
            var career = root == null ? null : CareerStore.TryLoadNamed(root.Session, id);
            if (career == null)
                return;
            BeginExit(() => LoadSelectedCareer(career, overlay));
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
