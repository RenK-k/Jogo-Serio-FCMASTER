using System.Collections;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.MatchPlay;
using FStudio.Data;
using FStudio.Events;
using FStudio.Graphics.Cameras;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Balls;
using FStudio.MatchEngine.Cameras;
using FStudio.MatchEngine.Enums;
using FStudio.MatchEngine.Events;
using FStudio.MatchEngine.Tactics;
using FStudio.UI;
using FStudio.UI.MatchThemes;
using FStudio.UI.Panels;
using FStudio.UI.Events;
using Shared.Responses;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace CoachMaster.UI.Scenes
{
    public sealed class SimulatorMatchHost : MonoBehaviour
    {
        public static bool Running { get; private set; }
        public static bool Spectating { get; private set; }
        public static bool PitchVisible { get; private set; }
        public bool BoardVisible => _pin != ViewPin.Pitch && (_pin == ViewPin.Board || (Spectating && !_attack));
        MatchSession _match;
        bool _hooked;
        bool _shots;
        bool _attack;
        bool _stadiumCamera;
        float _dangerUntil;
        Camera _cover;
        string _homeClub = "";
        string _awayClub = "";
        bool _choiceStarted;
        bool _ownControl;
        ViewPin _pin;
        RenderPipelineAsset _menuPipeline;
        bool _matchPipeline;

        enum ViewPin { Auto, Pitch, Board }

        void Update()
        {
            if (!Running)
            {
                PitchVisible = false;
                RestoreMenuPipeline();
                if (!Mathf.Approximately(Time.timeScale, 1f))
                    Time.timeScale = 1f;
                return;
            }

            if (MatchManager.Current == null)
                return;
            UseMatchPipeline();

            var day = GetComponent<MatchDayController>();
            var paused = MatchPause.IsPaused || (day != null && day.IsPaused) || AudioSettingsOverlay.IsVisible;
            Time.timeScale = paused ? 0f : 1.5f;
            SimulatorCareer.PushLiveTactics(_match);
            PollKicks();
            var live = PitchWanted;
            PitchVisible = live;
            var design = transform.Find("Design");
            if (design != null && design.gameObject.activeSelf == live)
                design.gameObject.SetActive(!live);
            ShowPitch(live);
            ShowScoreboard(live);
            HideTacticBar();
        }

        public void Begin(MatchSession match)
        {
            _match = match;
            Running = true;
            _pin = ViewPin.Auto;
            _ownControl = false;
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(false);
            if (!_hooked)
            {
                EventManager.Subscribe<MatchCompleteEvent>(Complete);
                _hooked = true;
            }

            CareerTacticLock.ReturnToMatchDay = ShowCareerMatch;
            CareerTacticLock.WatchSimulation = BeginSpectator;
            PlayerPrefs.SetInt("SETTING_SIDE", 0);
            StartCoroutine(Open());
        }

        void BeginSpectator()
        {
            Spectating = true;
            _ownControl = false;
            _pin = ViewPin.Auto;
            _attack = false;
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(true);
            HideMatchScreens();
            ShowPitch(true);
            RestoreEventSystem(gameObject.scene);
            if (_shots)
                return;
            EventManager.Subscribe<PlayerShootEvent>(NoteShot);
            EventManager.Subscribe<PlayerChipShootEvent>(NoteChip);
            _shots = true;
        }

        void NoteShot(PlayerShootEvent _)
        {
            _dangerUntil = Time.unscaledTime + 2.4f;
        }

        void NoteChip(PlayerChipShootEvent _)
        {
            _dangerUntil = Time.unscaledTime + 2.4f;
        }

        public void Mirror(MatchSession session)
        {
            var match = MatchManager.Current;
            if (match == null || session == null)
                return;
            session.HomeGoals = match.homeTeamScore;
            session.AwayGoals = match.awayTeamScore;
            session.Minute = match.minutes;
            var ball = Ball.Current;
            var holder = ball == null ? null : ball.HolderPlayer;
            if (holder == null || holder.GameTeam == null)
                return;
            var home = holder.GameTeam == match.GameTeam1;
            session.HomeHasBall = home;
            var players = home ? match.GameTeam1.GamePlayers : match.GameTeam2.GamePlayers;
            if (players == null)
                return;
            for (var i = 0; i < players.Length; i++)
            {
                if (players[i] != holder)
                    continue;
                session.BallHolder = i;
                break;
            }
        }

        public bool AttackMoment()
        {
            var match = MatchManager.Current;
            var ball = Ball.Current;
            if (match == null || ball == null)
                return Time.unscaledTime < _dangerUntil;
            var along = ball.transform.position.x / Mathf.Max(1f, match.fieldEndX);
            if (along > 0.78f || along < 0.22f)
                _dangerUntil = Time.unscaledTime + 1.5f;
            return Time.unscaledTime < _dangerUntil;
        }

        public void ShowBoard(bool board)
        {
            _pin = board ? ViewPin.Board : ViewPin.Pitch;
            if (board)
            {
                if (!Spectating)
                    _ownControl = true;
                Spectating = true;
                _attack = false;
            }
            else if (_ownControl)
            {
                Spectating = false;
                _ownControl = false;
            }
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(board);
            if (board)
                HideMatchScreens();
        }

        bool PitchWanted
        {
            get
            {
                if (_pin == ViewPin.Pitch)
                    return true;
                if (_pin == ViewPin.Board)
                    return false;
                return _attack || !Spectating;
            }
        }

        public void SetAttack(bool attack)
        {
            if (_pin != ViewPin.Auto)
                return;
            if (attack && MatchView() == null)
                attack = false;
            if (_attack == attack)
                return;
            _attack = attack;
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(!attack);
            ShowPitch(attack);
            if (!attack)
                HideMatchScreens();
        }

        public static bool TrySlot(bool home, int index, out Vector2 slot)
        {
            slot = default;
            var player = PlayerAt(home, index);
            if (player == null)
                return false;
            slot = ToSlot(player.Position);
            return true;
        }

        public static bool TryLook(bool home, int index, out Vector2 slot)
        {
            slot = default;
            var player = PlayerAt(home, index);
            if (player == null)
                return false;
            var ahead = player.Position + player.Direction;
            if (ahead == player.Position && Ball.Current != null)
                ahead = Ball.Current.transform.position;
            slot = ToSlot(ahead);
            return true;
        }

        public static bool TryBall(out Vector2 slot)
        {
            slot = default;
            if (Ball.Current == null || MatchManager.Current == null)
                return false;
            slot = ToSlot(Ball.Current.transform.position);
            return true;
        }

        public static float BallLift
        {
            get
            {
                if (Ball.Current == null)
                    return 0f;
                return Mathf.Clamp01(Ball.Current.transform.position.y / 4f);
            }
        }

        static FStudio.MatchEngine.Players.PlayerBase PlayerAt(bool home, int index)
        {
            var match = MatchManager.Current;
            if (!Spectating || match == null)
                return null;
            var team = home ? match.GameTeam1 : match.GameTeam2;
            if (team == null || team.GamePlayers == null || index < 0 || index >= team.GamePlayers.Length)
                return null;
            return team.GamePlayers[index];
        }

        static Vector2 ToSlot(Vector3 world)
        {
            var match = MatchManager.Current;
            var length = Mathf.Max(1f, match.fieldEndX);
            var width = Mathf.Max(1f, match.fieldEndY);
            return new Vector2(Mathf.Clamp01(world.z / width), Mathf.Clamp01(world.x / length));
        }

        void UseMatchPipeline()
        {
            if (_matchPipeline)
                return;
            var pipeline = MatchPipeline();
            if (pipeline == null)
                return;
            _menuPipeline = GraphicsSettings.defaultRenderPipeline;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            Shader.SetGlobalFloat("_SHADER_LAYER_COUNT", 8f);
            _matchPipeline = true;
        }

        void RestoreMenuPipeline()
        {
            if (!_matchPipeline)
                return;
            GraphicsSettings.defaultRenderPipeline = _menuPipeline;
            QualitySettings.renderPipeline = _menuPipeline;
            _matchPipeline = false;
        }

        static RenderPipelineAsset MatchPipeline()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>("Assets/FootballSimulator/URP/Mobile/Good.asset");
#else
            var assets = Resources.FindObjectsOfTypeAll<RenderPipelineAsset>();
            for (var i = 0; i < assets.Length; i++)
            {
                if (assets[i] != null && assets[i].name == "Good")
                    return assets[i];
            }
            return null;
#endif
        }

        void ShowScoreboard(bool show)
        {
            var board = Object.FindAnyObjectByType<LiveScoreboardPanel>(FindObjectsInactive.Include);
            if (board == null)
                return;
            if (!show)
            {
                if (board.IsActive)
                    board.Disappear();
                return;
            }

            var node = board.transform;
            while (node != null)
            {
                if (!node.gameObject.activeSelf)
                    node.gameObject.SetActive(true);
                node = node.parent;
            }
            if (!board.IsActive)
                board.Appear();
            var match = MatchManager.Current;
            if (match != null)
                board.WriteScore(match.homeTeamScore, match.awayTeamScore);
        }

        static void HideTacticBar()
        {
            var tactics = Object.FindAnyObjectByType<FStudio.MatchEngine.UI.TeamTacticsUI>(FindObjectsInactive.Include);
            if (tactics != null && tactics.IsActive)
                tactics.Disappear();
        }

        void HideMatchScreens()
        {
            var career = gameObject.scene;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas == null || canvas.gameObject.scene == career || canvas.renderMode == RenderMode.WorldSpace)
                    continue;
                canvas.gameObject.SetActive(false);
            }
        }

        void ShowPitch(bool show)
        {
            var view = MatchView();
            if (view != null)
            {
                var node = view.transform;
                while (node != null)
                {
                    if (!node.gameObject.activeSelf)
                        node.gameObject.SetActive(true);
                    node = node.parent;
                }
                view.targetTexture = null;
                view.targetDisplay = 0;
                view.cullingMask = ~0;
                view.enabled = true;
                view.depth = 20;
                if (_cover != null)
                    _cover.enabled = false;
                if (!_stadiumCamera && CameraSystem.Current != null)
                {
                    _stadiumCamera = true;
                    if (CameraSystem.Current.CurrentCameraType != "Stadium")
                        _ = CameraSystem.Current.SwitchCamera("Stadium");
                }
                return;
            }

            if (!show)
                return;
            var cover = Cover();
            cover.targetTexture = null;
            cover.targetDisplay = 0;
            cover.enabled = true;
        }

        static Camera MatchView()
        {
            if (CameraSystem.Current != null) {
                if (CameraSystem.Current.camera != null)
                    return CameraSystem.Current.camera;
                var attached = CameraSystem.Current.GetComponent<Camera>();
                if (attached != null)
                    return attached;
            }
            if (MainCamera.Current != null && MainCamera.Current.Camera != null)
                return MainCamera.Current.Camera;
            var tagged = GameObject.FindGameObjectWithTag("MainCamera");
            return tagged != null ? tagged.GetComponent<Camera>() : null;
        }

        Camera Cover()
        {
            if (_cover != null)
                return _cover;
            var go = new GameObject("SimCoverCamera");
            go.transform.SetParent(transform, false);
            _cover = go.AddComponent<Camera>();
            _cover.clearFlags = CameraClearFlags.SolidColor;
            _cover.backgroundColor = new Color(0.04f, 0.06f, 0.1f, 1f);
            _cover.cullingMask = 0;
            _cover.depth = -50;
            var listener = go.GetComponent<AudioListener>();
            if (listener != null)
                Destroy(listener);
            return _cover;
        }

        void ShowCareerMatch()
        {
            Running = false;
            Time.timeScale = 1f;
            RestoreMenuPipeline();
            var career = gameObject.scene;
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                if (canvases[i].gameObject.scene != career)
                    canvases[i].gameObject.SetActive(false);
            }

            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
                cameras[i].enabled = cameras[i].gameObject.scene == career;
            Restore<AudioListener>(career);
            RestoreEventSystem(career);
            var day = GetComponent<MatchDayController>();
            if (day != null)
                day.ResumeSimulation();
        }

        void ArmChoice()
        {
            _choiceStarted = false;
            CareerTacticLock.CareerChoice = true;
            CareerTacticLock.HomeVote = "";
            CareerTacticLock.AwayVote = "";
            CareerTacticLock.Cast = CastKick;
            var career = GameRoot.Instance == null || GameRoot.Instance.Session == null ? null : GameRoot.Instance.Session.Career;
            var mine = career != null && career.Coach != null ? career.Coach.ClubId : "";
            _homeClub = _match != null && _match.Home != null ? _match.Home.Id : "";
            _awayClub = _match != null && _match.Away != null ? _match.Away.Id : "";
            CareerTacticLock.HomeName = _match != null && _match.Home != null ? _match.Home.Name : "";
            CareerTacticLock.AwayName = _match != null && _match.Away != null ? _match.Away.Name : "";
            CareerTacticLock.OwnSide = mine == _awayClub
                ? MatchCreateRequest.UserTeam.Away
                : MatchCreateRequest.UserTeam.Home;
            CareerTacticLock.VersusHuman = CareerOnline.OtherHuman(career, _match == null ? null : _match.Fixture);
        }

        void CastKick(string choice)
        {
            if (_choiceStarted || _match == null)
                return;
            var career = GameRoot.Instance == null || GameRoot.Instance.Session == null ? null : GameRoot.Instance.Session.Career;
            var club = career != null && career.Coach != null ? career.Coach.ClubId : _homeClub;
            NoteVote(club, choice);
            if (CareerTacticLock.VersusHuman && (LanRoom.IsHost || LanRoom.IsGuest))
                LanRoom.Send("KICK|" + club + "|" + choice);
            if (!CareerTacticLock.VersusHuman)
                BeginChosen(choice);
            else
                TryAgreed();
        }

        void PollKicks()
        {
            if (!CareerTacticLock.VersusHuman || _choiceStarted)
                return;
            while (LanRoom.TryTake(out var line))
            {
                if (!string.IsNullOrEmpty(line) && line.StartsWith("KICK|"))
                {
                    var bits = line.Split('|');
                    if (bits.Length >= 3)
                    {
                        NoteVote(bits[1], bits[2]);
                        if (LanRoom.IsHost)
                            LanRoom.Send(line);
                    }
                }
                else
                    LanRoom.Postpone(line);
            }
            TryAgreed();
        }

        void NoteVote(string club, string choice)
        {
            if (choice != "play" && choice != "sim")
                return;
            if (club == _homeClub)
                CareerTacticLock.HomeVote = choice;
            else if (club == _awayClub)
                CareerTacticLock.AwayVote = choice;
        }

        void TryAgreed()
        {
            if (!CareerTacticLock.VersusHuman || _choiceStarted)
                return;
            var home = CareerTacticLock.HomeVote;
            var away = CareerTacticLock.AwayVote;
            if (string.IsNullOrEmpty(home) || home != away)
                return;
            BeginChosen(home);
        }

        void BeginChosen(string choice)
        {
            if (_choiceStarted)
                return;
            _choiceStarted = true;
            CareerTacticLock.Cast = null;
            MatchSettingsPanel.SIDE = choice == "play" ? CareerTacticLock.OwnSide : MatchCreateRequest.UserTeam.None;
            var panel = FindAnyObjectByType<UpcomingMatchPanel>();
            if (panel != null)
                panel.StartMatch();
        }

        IEnumerator Open()
        {
            var root = CoachMaster.App.GameRoot.Instance;
            if (root != null)
                root.PresentLoading();
            SimulatorCareer.Prepare(_match);
            if (!gameObject.GetComponent<ModernStadiumDress>())
                gameObject.AddComponent<ModernStadiumDress>();
            var loaded = SceneManager.GetSceneByName("_StartingScene");
            if (!loaded.isLoaded)
            {
                var load = SceneManager.LoadSceneAsync("_StartingScene", LoadSceneMode.Additive);
                if (load == null)
                {
                    if (root != null)
                        yield return root.DismissLoading();
                    yield break;
                }
                while (!load.isDone)
                    yield return null;
            }

            for (var i = 0; i < 30 && UILoader.Current == null; i++)
                yield return null;
            if (UILoader.Current == null || _match == null)
            {
                if (root != null)
                    yield return root.DismissLoading();
                yield break;
            }
            PreferSimulator();
            var general = UILoader.Current.GeneralUILoader.Load();
            while (!general.IsCompleted)
                yield return null;
            if (general.IsFaulted)
                Debug.LogException(general.Exception);
            var world = SceneManager.GetSceneByName("DefaultScene");
            if (!world.isLoaded)
            {
                var worldLoad = SceneManager.LoadSceneAsync("DefaultScene", LoadSceneMode.Additive);
                if (worldLoad != null)
                {
                    while (!worldLoad.isDone)
                        yield return null;
                }
            }

            PreferSimulator();
            ArmChoice();
            var request = new MatchCreateRequest(SimulatorCareer.BuildTeam(_match, true), SimulatorCareer.BuildTeam(_match, false));
            request.aiLevel = AILevel.Legendary;
            request.userTeam = MatchCreateRequest.UserTeam.None;
            request.dayTime = SimulatorCareer.KickoffLight(_match);
            var match = MatchEngineLoader.CreateMatch(request);
            while (!match.IsCompleted)
                yield return null;
            if (match.IsFaulted)
            {
                Debug.LogException(match.Exception);
                ShowCareerMatch();
            }
            PreferSimulator();
            if (root != null)
                yield return root.DismissLoading();
        }

        void PreferSimulator()
        {
            var career = gameObject.scene;
            HandOff<AudioListener>(career);
            HandOff<EventSystem>(career);
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var stadiumCamera = false;
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.scene != career)
                    stadiumCamera = true;
            }

            if (!stadiumCamera)
                return;
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.scene == career)
                    cameras[i].enabled = false;
            }
        }

        static void HandOff<T>(Scene career) where T : Behaviour
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var other = false;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.scene != career)
                    other = true;
            }

            if (!other)
                return;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].gameObject.scene == career)
                    all[i].enabled = false;
            }
        }

        void RestoreCareer()
        {
            var career = gameObject.scene;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                if (cameras[i].gameObject.scene == career)
                    cameras[i].enabled = true;
            }

            Restore<AudioListener>(career);
            RestoreEventSystem(career);
        }

        static void RestoreEventSystem(Scene career)
        {
            var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem keep = null;
            for (var i = 0; i < all.Length; i++)
            {
                if (keep == null && all[i].gameObject.scene == career)
                    keep = all[i];
                all[i].enabled = false;
            }

            if (keep != null)
                keep.enabled = true;
        }

        static void Restore<T>(Scene career) where T : Behaviour
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
                all[i].enabled = all[i].gameObject.scene == career;
        }

        void Complete(MatchCompleteEvent _)
        {
            Running = false;
            Spectating = false;
            _attack = false;
            _pin = ViewPin.Auto;
            Time.timeScale = 1f;
            RestoreMenuPipeline();
            if (_match == null || MatchManager.Current == null)
                return;
            _match.HomeGoals = MatchManager.Current.homeTeamScore;
            _match.AwayGoals = MatchManager.Current.awayTeamScore;
            _match.Minute = 90f;
            _match.Finished = true;
            _match.Phase = MatchPhase.FullTime;
            Running = false;
            ShowPitch(false);
            RestoreCareer();
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(true);
        }

        void OnDestroy()
        {
            if (_hooked)
                EventManager.UnSubscribe<MatchCompleteEvent>(Complete);
            if (_shots)
            {
                EventManager.UnSubscribe<PlayerShootEvent>(NoteShot);
                EventManager.UnSubscribe<PlayerChipShootEvent>(NoteChip);
            }
            Spectating = false;
            Running = false;
            PitchVisible = false;
            _pin = ViewPin.Auto;
            RestoreMenuPipeline();
            Time.timeScale = 1f;
        }
    }
}
