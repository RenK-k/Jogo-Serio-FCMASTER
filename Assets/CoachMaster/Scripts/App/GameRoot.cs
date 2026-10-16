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

        public GameSession Session { get; private set; }
        public MatchSession LiveMatch { get; set; }
        [SerializeField] GameAudioLibrary _audioLibrary;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
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
        }

        static void FitCanvases()
        {
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None);
            for (var i = 0; i < scalers.Length; i++)
            {
                scalers[i].uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scalers[i].referenceResolution = new Vector2(1920, 1080);
                scalers[i].screenMatchMode = CanvasScaler.ScreenMatchMode.Shrink;
            }
        }

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
            SceneManager.LoadScene(sceneName);
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
