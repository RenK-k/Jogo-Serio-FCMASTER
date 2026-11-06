using System.Collections;
using CoachMaster.Career;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.UI;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;

namespace CoachMaster.App
{
    public sealed class GameRoot : MonoBehaviour
    {
        public static GameRoot Instance { get; private set; }
        public static bool IsLoading { get; private set; }

        public GameSession Session { get; private set; }
        public MatchSession LiveMatch { get; set; }
        [SerializeField] GameAudioLibrary _audioLibrary;
        Canvas _loadingCanvas;
        CanvasGroup _loadingGroup;
        RawImage _loadingBackdrop;
        RawImage _loadingLogo;
        Texture2D _loadingFallback;
        Coroutine _loadRoutine;
        string _loadingTarget;
        string _queuedScene;
        int _loadingUsers;
        float _careerSaveClock;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildLoadingOverlay();
            if (Session == null)
                Session = new GameSession();
            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureUiInput();
            if (GetComponent<ControlChrome>() == null)
                gameObject.AddComponent<ControlChrome>();
            if (GetComponent<AudioSettingsOverlay>() == null)
                gameObject.AddComponent<AudioSettingsOverlay>();
            if (_audioLibrary == null)
                _audioLibrary = Resources.Load<GameAudioLibrary>("GameAudioLibrary");
            if (_audioLibrary == null)
                _audioLibrary = Resources.Load<GameAudioLibrary>("Audio/GameAudioLibrary");
            GameAudio.Bind(_audioLibrary);
            DisplayPrefs.Boot();
        }

        void OnDestroy()
        {
            if (Instance == this)
                SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnApplicationQuit()
        {
            var career = Session == null ? null : Session.Career;
            if (career == null || SceneManager.GetActiveScene().name == SceneNames.MainMenu
                || (career.Online != null && career.Online.Active))
                return;
            CareerStore.TrySave(career);
        }

        void Update()
        {
            PulseLoadingLogo();
            var career = Session == null ? null : Session.Career;
            if (career == null || SceneManager.GetActiveScene().name == SceneNames.MainMenu)
                return;
            career.PlayTimeSeconds += Time.unscaledDeltaTime;
            _careerSaveClock += Time.unscaledDeltaTime;
            if (_careerSaveClock < 60f || (career.Online != null && career.Online.Active))
                return;
            _careerSaveClock = 0f;
            CareerStore.TrySave(career);
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SquadDragCard.Release();
            EnsureUiInput();
            FitCanvases();
            ControlChrome.DisableAutomaticNavigation();
            ControlChrome.ResetForScene();
            LocalizationManager.ApplyScene();
            ControlBindings.Apply();
        }

        public static void ApplyUiScale()
        {
            var scale = Mathf.Clamp(SettingsPrefs.UiScale, 0.8f, 1.2f);
            var target = new Vector2(1920f / scale, 1080f / scale);
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            for (var i = 0; i < scalers.Length; i++)
            {
                var scaler = scalers[i];
                if (scaler == null)
                    continue;
                scaler.transform.localScale = Vector3.one;
                if (scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                    && scaler.screenMatchMode == CanvasScaler.ScreenMatchMode.Shrink
                    && scaler.referenceResolution == target)
                    continue;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = target;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            }
            UiScaleInvariant.ApplyAll();
        }

        static void FitCanvases() => ApplyUiScale();

        public static void EnsureUiInput()
        {
            var systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            if (systems.Length == 0)
            {
                var created = new GameObject("EventSystem");
                created.AddComponent<EventSystem>();
                created.AddComponent<InputSystemUIInputModule>();
                systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
            }

            foreach (var system in systems)
            {
                var legacy = system.GetComponent<StandaloneInputModule>();
                if (legacy != null)
                    legacy.enabled = false;
                var module = system.GetComponent<InputSystemUIInputModule>();
                if (module == null)
                    module = system.gameObject.AddComponent<InputSystemUIInputModule>();
                if (module.actionsAsset == null)
                    module.AssignDefaultActions();
            }
            ControlBindings.Apply();
        }

        public static GameRoot Require()
        {
            if (Instance != null)
                return Instance;

            var go = new GameObject("GameRoot");
            return go.AddComponent<GameRoot>();
        }

        public void Open(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
                return;
            if (_loadRoutine != null || IsLoading)
            {
                if (sceneName != _loadingTarget)
                    _queuedScene = sceneName;
                return;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
                return;

            _loadingTarget = sceneName;
            IsLoading = true;
            _loadRoutine = StartCoroutine(LoadScene(operation));
        }

        void BuildLoadingOverlay()
        {
            if (_loadingCanvas != null)
                return;

            var canvasObject = new GameObject("LoadingOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
            canvasObject.transform.SetParent(transform, false);
            _loadingCanvas = canvasObject.GetComponent<Canvas>();
            _loadingCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _loadingCanvas.overrideSorting = true;
            _loadingCanvas.sortingOrder = 32760;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            _loadingGroup = canvasObject.GetComponent<CanvasGroup>();
            _loadingGroup.interactable = false;
            _loadingGroup.blocksRaycasts = true;

            var backdropObject = new GameObject("PreviousScreen", typeof(RectTransform), typeof(RawImage));
            backdropObject.transform.SetParent(canvasObject.transform, false);
            var backdropRect = backdropObject.GetComponent<RectTransform>();
            backdropRect.anchorMin = Vector2.zero;
            backdropRect.anchorMax = Vector2.one;
            backdropRect.offsetMin = Vector2.zero;
            backdropRect.offsetMax = Vector2.zero;
            _loadingBackdrop = backdropObject.GetComponent<RawImage>();
            _loadingBackdrop.raycastTarget = false;
            _loadingBackdrop.color = new Color(0f, 0f, 0f, 0.42f);

            _loadingFallback = Resources.Load<Texture2D>("fcmasterGIF");
            if (_loadingFallback == null)
                _loadingFallback = BroadcastUi.BrandTexture;

            var logoObject = new GameObject("LoadingMark", typeof(RectTransform), typeof(RawImage));
            logoObject.transform.SetParent(canvasObject.transform, false);
            var logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.08f);
            logoRect.pivot = new Vector2(0.5f, 0.5f);
            _loadingLogo = logoObject.GetComponent<RawImage>();
            _loadingLogo.raycastTarget = false;
            ApplyLoadingLogo();
            canvasObject.SetActive(false);
        }

        public void PresentLoading()
        {
            BuildLoadingOverlay();
            _loadingUsers++;
            IsLoading = true;
            _loadingBackdrop.color = new Color(0f, 0f, 0f, 0.42f);
            _loadingBackdrop.texture = null;
            ApplyLoadingLogo();
            if (!_loadingCanvas.gameObject.activeSelf)
                _loadingCanvas.gameObject.SetActive(true);
            _loadingGroup.alpha = 1f;
            _loadingGroup.blocksRaycasts = true;
        }

        public IEnumerator DismissLoading()
        {
            _loadingUsers = Mathf.Max(0, _loadingUsers - 1);
            if (_loadingUsers > 0)
                yield break;
            yield return FadeLoadingOut();
        }

        void ApplyLoadingLogo()
        {
            if (_loadingLogo == null)
                return;
            _loadingLogo.rectTransform.anchorMin = _loadingLogo.rectTransform.anchorMax = new Vector2(0.5f, 0.08f);
            _loadingLogo.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            BroadcastUi.ApplyBrand(_loadingLogo);
            if (_loadingLogo.texture == null)
                _loadingLogo.texture = _loadingFallback;
            BroadcastUi.FitBrand(_loadingLogo, 280f, 96f);
        }

        void PulseLoadingLogo()
        {
            if (_loadingLogo == null || _loadingCanvas == null || !_loadingCanvas.gameObject.activeSelf)
                return;
            var pulse = 1f + Mathf.Sin(Time.unscaledTime * 3.1f) * 0.08f;
            _loadingLogo.rectTransform.localScale = new Vector3(pulse, pulse, 1f);
        }

        IEnumerator LoadScene(AsyncOperation operation)
        {
            PresentLoading();
            if (_loadingGroup != null)
                _loadingGroup.alpha = 0f;
            var shown = 0f;
            var fadeIn = SettingsPrefs.ReduceMotion ? 0.05f : 0.16f;
            while (!operation.isDone || shown < 0.42f)
            {
                shown += Time.unscaledDeltaTime;
                if (_loadingGroup != null)
                    _loadingGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(shown / fadeIn));
                if (operation.isDone && shown >= 0.42f)
                    break;
                yield return null;
            }

            yield return DismissLoading();
            IsLoading = _loadingUsers > 0;
            _loadRoutine = null;
            _loadingTarget = null;
            var queuedScene = _queuedScene;
            _queuedScene = null;
            if (!string.IsNullOrEmpty(queuedScene))
                Open(queuedScene);
        }

        IEnumerator FadeLoadingOut()
        {
            var start = _loadingGroup.alpha;
            var fade = 0f;
            while (fade < 1f)
            {
                fade += Time.unscaledDeltaTime / (SettingsPrefs.ReduceMotion ? 0.06f : 0.18f);
                _loadingGroup.alpha = Mathf.Lerp(start, 0f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fade)));
                yield return null;
            }
            _loadingCanvas.gameObject.SetActive(false);
            IsLoading = false;
        }
    }

    public sealed class GameSession
    {
        public WorldState Catalog { get; }
        public NewCareerDraft Draft { get; } = new NewCareerDraft();
        public CareerState Career { get; private set; }

        public GameSession()
        {
            Catalog = Data.WorldCatalog.CreateShell();
        }

        public void PrepareLeague(string leagueId)
        {
            Data.WorldCatalog.EnsureLeagueClubs(Catalog, leagueId);
        }

        public void StartCareer()
        {
            var world = Draft.PreviewWorld ?? Data.WorldCatalog.Create();
            Career = CareerFactory.Create(Draft.CoachName, Draft.ClubId, world);
            Career.InjuryFrequency = Mathf.Clamp(Draft.InjuryFrequency, 0, 2);
            Career.RefereeStrictness = Mathf.Clamp(Draft.RefereeStrictness, 0, 2);
            Career.RealismPreset = Mathf.Clamp(Draft.RealismPreset, 0, 4);
            Career.MatchSpeedIndex = Mathf.Clamp(Draft.MatchSpeedIndex, 0, 4);
            Career.AutomaticSquadSelection = Draft.AutomaticSquadSelection;
            Career.AutomaticTraining = Draft.AutomaticTraining;
            Career.MatchHighlights = Mathf.Clamp(Draft.MatchHighlights, 0, 3);
            Career.PauseOnGoal = Draft.PauseOnGoal;
            Career.PauseOnRedCard = Draft.PauseOnRedCard;
            Career.LiveRatings = Draft.LiveRatings;
            Career.DatabaseCustomizationEnabled = Draft.PersonalizeDatabase;
            SettingsPrefs.ApplyCareerGameplay(Career);
            SettingsPrefs.Commit();
            Career.LastAutoSaveDateTicks = Career.Calendar.CurrentDate.Ticks;
            if (Draft.PreviewWorld == null)
                Data.ModFolder.Apply(Career);
            Draft.PreviewWorld = null;
        }

        public WorldState PrepareCareerPreview()
        {
            if (Draft.PreviewWorld == null)
            {
                Draft.PreviewWorld = Data.WorldCatalog.CompleteCareerWorld(Catalog);
                Data.ModFolder.Apply(Draft.PreviewWorld);
            }
            return Draft.PreviewWorld;
        }

        public void Restore(CareerState career)
        {
            Career = career;
        }

        public void ClearCareer()
        {
            Career = null;
            Draft.Reset();
        }
    }

    public sealed class NewCareerDraft
    {
        public string LeagueId;
        public string ClubId;
        public string CoachName = "";
        public int MatchSpeedIndex = SettingsPrefs.MatchSpeedIndex;
        public int InjuryFrequency = SettingsPrefs.DefaultInjuryFrequency;
        public int RefereeStrictness = SettingsPrefs.DefaultRefereeStrictness;
        public int RealismPreset = SettingsPrefs.DefaultRealismPreset;
        public bool AutomaticSquadSelection = SettingsPrefs.AutomaticSquadSelection;
        public bool AutomaticTraining = SettingsPrefs.AutomaticTraining;
        public int MatchHighlights = SettingsPrefs.MatchHighlights;
        public bool PauseOnGoal = SettingsPrefs.PauseOnGoal;
        public bool PauseOnRedCard = SettingsPrefs.PauseOnRedCard;
        public bool LiveRatings = SettingsPrefs.LiveRatings;
        public bool PersonalizeDatabase;
        public WorldState PreviewWorld;

        public void Reset()
        {
            LeagueId = null;
            ClubId = null;
            CoachName = "";
            MatchSpeedIndex = SettingsPrefs.MatchSpeedIndex;
            InjuryFrequency = SettingsPrefs.DefaultInjuryFrequency;
            RefereeStrictness = SettingsPrefs.DefaultRefereeStrictness;
            RealismPreset = SettingsPrefs.DefaultRealismPreset;
            AutomaticSquadSelection = SettingsPrefs.AutomaticSquadSelection;
            AutomaticTraining = SettingsPrefs.AutomaticTraining;
            MatchHighlights = SettingsPrefs.MatchHighlights;
            PauseOnGoal = SettingsPrefs.PauseOnGoal;
            PauseOnRedCard = SettingsPrefs.PauseOnRedCard;
            LiveRatings = SettingsPrefs.LiveRatings;
            PersonalizeDatabase = false;
            PreviewWorld = null;
        }
    }
}
