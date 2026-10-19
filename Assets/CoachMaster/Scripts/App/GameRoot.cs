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
        RawImage _loadingLogoBlend;
        Texture2D _loadingAnimation;
        Texture2D _loadingSnapshot;
        Coroutine _loadRoutine;
        const float LoadingFrameSeconds = 0.05f;
        const int LoadingFrameColumns = 8;
        const int LoadingFrameRows = 8;
        const int LoadingFrameCount = 48;

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

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SquadDragCard.Release();
            EnsureUiInput();
            FitCanvases();
            ControlChrome.DisableAutomaticNavigation();
            ControlChrome.ResetForScene();
        }

        public static void ApplyUiScale()
        {
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            for (var i = 0; i < scalers.Length; i++)
            {
                scalers[i].uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                var scale = Mathf.Clamp(SettingsPrefs.UiScale, 0.8f, 1.2f);
                scalers[i].referenceResolution = new Vector2(1920f / scale, 1080f / scale);
                scalers[i].screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            }
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
                return;
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
            if (_loadRoutine != null)
                StopCoroutine(_loadRoutine);
            _loadRoutine = StartCoroutine(LoadScene(sceneName));
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

            _loadingAnimation = Resources.Load<Texture2D>("fcmasterGIFAnimated");

            var logoObject = new GameObject("LoadingMark", typeof(RectTransform), typeof(RawImage));
            logoObject.transform.SetParent(canvasObject.transform, false);
            var logoRect = logoObject.GetComponent<RectTransform>();
            logoRect.anchorMin = logoRect.anchorMax = new Vector2(0.5f, 0.12f);
            logoRect.sizeDelta = new Vector2(180f, 180f);
            _loadingLogo = logoObject.GetComponent<RawImage>();
            _loadingLogo.raycastTarget = false;

            var blendObject = new GameObject("LoadingMarkBlend", typeof(RectTransform), typeof(RawImage));
            blendObject.transform.SetParent(canvasObject.transform, false);
            var blendRect = blendObject.GetComponent<RectTransform>();
            blendRect.anchorMin = blendRect.anchorMax = new Vector2(0.5f, 0.12f);
            blendRect.sizeDelta = logoRect.sizeDelta;
            _loadingLogoBlend = blendObject.GetComponent<RawImage>();
            _loadingLogoBlend.raycastTarget = false;
            _loadingLogoBlend.color = new Color(1f, 1f, 1f, 0f);
            SetLoadingFrame(0, 0f);
            canvasObject.SetActive(false);
        }

        IEnumerator LoadScene(string sceneName)
        {
            yield return new WaitForEndOfFrame();
            if (_loadingSnapshot != null)
                Destroy(_loadingSnapshot);
            _loadingSnapshot = ScreenCapture.CaptureScreenshotAsTexture();
            _loadingBackdrop.texture = _loadingSnapshot;
            SetLoadingFrame(0);
            IsLoading = true;
            _loadingCanvas.gameObject.SetActive(true);
            _loadingGroup.alpha = 0f;
            var fadeIn = 0f;
            while (fadeIn < 1f)
            {
                fadeIn += Time.unscaledDeltaTime / (SettingsPrefs.ReduceMotion ? 0.06f : 0.22f);
                _loadingGroup.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(fadeIn));
                yield return null;
            }

            var operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                yield return FadeLoadingOut();
                IsLoading = false;
                _loadRoutine = null;
                yield break;
            }

            var animationElapsed = 0f;
            var visibleAfterLoad = 0f;
            while (!operation.isDone || visibleAfterLoad < 0.25f)
            {
                var delta = Time.unscaledDeltaTime;
                if (SettingsPrefs.ReduceMotion)
                    SetLoadingFrame(0);
                else
                {
                    animationElapsed = (animationElapsed + delta) % (LoadingFrameSeconds * LoadingFrameCount);
                    var framePosition = animationElapsed / LoadingFrameSeconds;
                    var frameIndex = Mathf.FloorToInt(framePosition);
                    SetLoadingFrame(frameIndex, framePosition - frameIndex);
                }
                if (operation.isDone)
                    visibleAfterLoad += delta;
                yield return null;
            }

            yield return FadeLoadingOut();
            IsLoading = false;
            _loadRoutine = null;
        }

        void SetLoadingFrame(int index, float blend = 0f)
        {
            if (_loadingLogo == null)
                return;
            if (_loadingAnimation == null)
                _loadingAnimation = Resources.Load<Texture2D>("fcmasterGIFAnimated");
            if (_loadingAnimation != null)
            {
                var frame = ((index % LoadingFrameCount) + LoadingFrameCount) % LoadingFrameCount;
                SetAtlasFrame(_loadingLogo, frame, 1f - Mathf.Clamp01(blend));
                if (_loadingLogoBlend != null)
                    SetAtlasFrame(_loadingLogoBlend, (frame + 1) % LoadingFrameCount, Mathf.Clamp01(blend));
                return;
            }

            _loadingLogo.texture = Resources.Load<Texture2D>("fcmasterGIF");
            if (_loadingLogo.texture == null)
                _loadingLogo.texture = BroadcastUi.BrandTexture;
            _loadingLogo.uvRect = new Rect(0f, 0f, 1f, 1f);
            _loadingLogo.color = Color.white;
            if (_loadingLogoBlend != null)
                _loadingLogoBlend.color = new Color(1f, 1f, 1f, 0f);
        }

        void SetAtlasFrame(RawImage image, int index, float alpha)
        {
            if (image == null)
                return;
            image.texture = _loadingAnimation;
            var column = index % LoadingFrameColumns;
            var row = index / LoadingFrameColumns;
            var cellWidth = 1f / LoadingFrameColumns;
            var cellHeight = 1f / LoadingFrameRows;
            var insetX = 0.5f / _loadingAnimation.width;
            var insetY = 0.5f / _loadingAnimation.height;
            image.uvRect = new Rect(column * cellWidth + insetX,
                1f - (row + 1) * cellHeight + insetY,
                cellWidth - insetX * 2f,
                cellHeight - insetY * 2f);
            image.color = new Color(1f, 1f, 1f, alpha);
        }

        IEnumerator FadeLoadingOut()
        {
            var start = _loadingGroup.alpha;
            var fade = 0f;
            while (fade < 1f)
            {
                fade += Time.unscaledDeltaTime / (SettingsPrefs.ReduceMotion ? 0.06f : 0.28f);
                _loadingGroup.alpha = Mathf.Lerp(start, 0f, Mathf.Clamp01(fade));
                yield return null;
            }
            _loadingCanvas.gameObject.SetActive(false);
            if (_loadingBackdrop != null)
                _loadingBackdrop.texture = null;
            if (_loadingSnapshot != null)
            {
                Destroy(_loadingSnapshot);
                _loadingSnapshot = null;
            }
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
            Career = CareerFactory.Create(Draft.CoachName, Draft.ClubId, Data.WorldCatalog.Create());
            Career.InjuryFrequency = SettingsPrefs.DefaultInjuryFrequency;
            Career.RefereeStrictness = SettingsPrefs.DefaultRefereeStrictness;
            Career.RealismPreset = SettingsPrefs.DefaultRealismPreset;
            Career.LastAutoSaveDateTicks = Career.Calendar.CurrentDate.Ticks;
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

        public void Reset()
        {
            LeagueId = null;
            ClubId = null;
            CoachName = "";
        }
    }
}
