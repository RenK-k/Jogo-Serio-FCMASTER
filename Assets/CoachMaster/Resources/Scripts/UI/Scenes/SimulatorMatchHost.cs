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
using UnityEngine.UI;

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
        MatchMinimapGraphic _map;
        float _sampleAt;
        bool _feed;
        MatchShotEvent _lastShot;
        float _lastShotAt;
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
            KeepOneEventSystem();

            var day = GetComponent<MatchDayController>();
            var paused = MatchPause.IsPaused || (day != null && day.IsPaused) || AudioSettingsOverlay.IsVisible;
            Time.timeScale = paused ? 0f : 1.5f;
            SimulatorCareer.PushLiveTactics(_match);
            PollKicks();
            var live = PitchWanted;
            PitchVisible = live;
            var design = transform.Find("Design");
            if (design != null && !paused && design.gameObject.activeSelf == live)
                design.gameObject.SetActive(!live);
            ShowPitch(live);
            ShowScoreboard(live);
            ShowMinimap(false);
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
            Listen();
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
            Listen();
        }

        void Listen()
        {
            if (_feed)
                return;
            _feed = true;
            _shots = true;
            EventManager.Subscribe<PlayerShootEvent>(NoteShot);
            EventManager.Subscribe<PlayerChipShootEvent>(NoteChip);
            EventManager.Subscribe<GoalEvent>(NoteGoal);
            EventManager.Subscribe<PlayerPassEvent>(NotePass);
            EventManager.Subscribe<CornerEvent>(NoteCorner);
            EventManager.Subscribe<ThrowInEvent>(NoteThrow);
            EventManager.Subscribe<KeeperSavesTheBallEvent>(NoteSave);
            EventManager.Subscribe<BallHitTheWoodWorkEvent>(NoteWood);
        }

        void NoteShot(PlayerShootEvent shot)
        {
            _dangerUntil = Time.unscaledTime + 2.4f;
            RecordShot(shot == null ? null : shot.Player);
        }

        void NoteChip(PlayerChipShootEvent shot)
        {
            _dangerUntil = Time.unscaledTime + 2.4f;
            RecordShot(shot == null ? null : shot.Player);
        }

        void RecordShot(FStudio.MatchEngine.Players.PlayerBase player)
        {
            if (_match == null || player == null || MatchManager.Current == null)
                return;
            var home = player.GameTeam == MatchManager.Current.GameTeam1;
            if (home)
                _match.HomeShots++;
            else
                _match.AwayShots++;
            var id = SquadId(player);
            var spot = ToSlot(player.Position);
            var depth = home ? spot.y : 1f - spot.y;
            if (depth > 0.72f)
            {
                if (home)
                    _match.HomeChances++;
                else
                    _match.AwayChances++;
            }
            _lastShot = _match.RecordShot(home, id, spot, spot, Mathf.Clamp01(0.08f + depth * 0.2f), "miss", false);
            _lastShotAt = Time.unscaledTime;
            var who = NameOf(id);
            _match.AddNote(who + " remata.", "shot", id);
        }

        void NoteGoal(GoalEvent goal)
        {
            if (_match == null || goal == null)
                return;
            var home = !goal.HomeOrAway;
            if (_lastShot != null && Time.unscaledTime - _lastShotAt < 5f)
                _lastShot.Outcome = "goal";
            var id = _lastShot != null && _lastShot.Home == home ? _lastShot.ShooterId : null;
            if (!string.IsNullOrEmpty(id))
            {
                _match.GoalsScored.TryGetValue(id, out var scored);
                _match.GoalsScored[id] = scored + 1;
            }
            var who = NameOf(id);
            _match.AddNote(string.IsNullOrEmpty(who) ? "Golo." : "Golo de " + who + ".", "goal", id);
        }

        void NotePass(PlayerPassEvent pass)
        {
            if (_match == null || pass == null || pass.Player == null || MatchManager.Current == null)
                return;
            var home = pass.Player.GameTeam == MatchManager.Current.GameTeam1;
            var id = SquadId(pass.Player);
            _match.RecordPass(home, id, null, ToSlot(pass.Player.Position), ToSlot(pass.Player.Position), false);
        }

        void NoteCorner(CornerEvent _)
        {
            if (_match != null)
                _match.AddNote("Canto.", "corner");
        }

        void NoteThrow(ThrowInEvent _)
        {
            if (_match != null)
                _match.AddNote("Lançamento.", "throwin");
        }

        void NoteSave(KeeperSavesTheBallEvent save)
        {
            if (_match == null || save == null || save.Player == null)
                return;
            var id = SquadId(save.Player);
            _match.AddNote(NameOf(id) + " defende.", "save", id);
            if (_lastShot != null && Time.unscaledTime - _lastShotAt < 4f)
                _lastShot.Outcome = "save";
        }

        void NoteWood(BallHitTheWoodWorkEvent _)
        {
            if (_match == null)
                return;
            _match.AddNote("A bola bate na trave.", "wood");
            if (_lastShot != null && Time.unscaledTime - _lastShotAt < 4f)
                _lastShot.Outcome = "woodwork";
        }

        public static void LiveFoul(FStudio.MatchEngine.Players.PlayerBase fouler, FStudio.MatchEngine.Players.PlayerBase victim)
        {
            var host = Object.FindAnyObjectByType<SimulatorMatchHost>();
            if (host == null || host._match == null || victim == null)
                return;
            var id = host.SquadId(victim);
            var who = host.NameOf(host.SquadId(fouler));
            host._match.AddNote("Falta de " + who + ".", "foul", id);
        }

        string SquadId(FStudio.MatchEngine.Players.PlayerBase player)
        {
            var match = MatchManager.Current;
            if (player == null || match == null || player.GameTeam == null || _match == null)
                return null;
            var home = player.GameTeam == match.GameTeam1;
            var squad = home ? _match.HomeOnField : _match.AwayOnField;
            var players = home ? match.GameTeam1.GamePlayers : match.GameTeam2.GamePlayers;
            if (players == null)
                return null;
            for (var i = 0; i < players.Length && i < squad.Count; i++)
            {
                if (players[i] == player)
                    return squad[i];
            }
            return null;
        }

        string NameOf(string id)
        {
            if (string.IsNullOrEmpty(id) || _match == null)
                return "O jogador";
            if (_match.Names.TryGetValue(id, out var name) && !string.IsNullOrEmpty(name))
                return name;
            return "O jogador";
        }

        public void Mirror(MatchSession session)
        {
            var match = MatchManager.Current;
            if (match == null || session == null)
                return;
            session.HomeGoals = match.homeTeamScore;
            session.AwayGoals = match.awayTeamScore;
            session.Minute = match.minutes;
            PitchOrders.SyncScore(session.HomeGoals, session.AwayGoals);
            var ball = Ball.Current;
            var holder = ball == null ? null : ball.HolderPlayer;
            if (holder != null && holder.GameTeam != null)
            {
                var home = holder.GameTeam == match.GameTeam1;
                session.HomeHasBall = home;
                if (home)
                    session.HomePossession += Time.deltaTime;
                else
                    session.AwayPossession += Time.deltaTime;
                var players = home ? match.GameTeam1.GamePlayers : match.GameTeam2.GamePlayers;
                if (players != null)
                {
                    for (var i = 0; i < players.Length; i++)
                    {
                        if (players[i] != holder)
                            continue;
                        session.BallHolder = i;
                        break;
                    }
                }
            }
            if (Time.unscaledTime >= _sampleAt)
            {
                _sampleAt = Time.unscaledTime + 12f;
                SamplePositions(session, match);
            }
        }

        void SamplePositions(MatchSession session, MatchManager match)
        {
            var home = Slots(match.GameTeam1);
            var away = Slots(match.GameTeam2);
            if (home == null || away == null || session.HomeOnField.Count < home.Length || session.AwayOnField.Count < away.Length)
                return;
            var homeIds = new string[home.Length];
            var awayIds = new string[away.Length];
            for (var i = 0; i < homeIds.Length; i++)
                homeIds[i] = session.HomeOnField[i];
            for (var i = 0; i < awayIds.Length; i++)
                awayIds[i] = session.AwayOnField[i];
            session.CapturePositions(session.Minute, home, away, homeIds, awayIds);
        }

        static Vector2[] Slots(GameTeam team)
        {
            if (team == null || team.GamePlayers == null)
                return null;
            var slots = new Vector2[team.GamePlayers.Length];
            for (var i = 0; i < slots.Length; i++)
            {
                var player = team.GamePlayers[i];
                slots[i] = player == null ? new Vector2(0.5f, 0.5f) : ToSlot(player.Position);
            }
            return slots;
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
            if (!Running || match == null)
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

        void ShowMinimap(bool show)
        {
            if (_map == null)
            {
                var canvas = GetComponentInParent<Canvas>();
                if (canvas == null)
                    return;
                var mapObject = new GameObject("MatchMinimap", typeof(RectTransform), typeof(CanvasRenderer), typeof(MatchMinimapGraphic));
                mapObject.transform.SetParent(canvas.transform, false);
                var rect = mapObject.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 16f);
                rect.sizeDelta = new Vector2(300f, 176f);
                _map = mapObject.GetComponent<MatchMinimapGraphic>();
                _map.raycastTarget = false;
            }

            if (_map.gameObject.activeSelf != show)
                _map.gameObject.SetActive(show);
            if (show)
                _map.SetVerticesDirty();
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
            var career = gameObject.scene;
            var view = MatchView();
            if (!show)
            {
                if (view != null && view.gameObject.scene != career)
                    view.enabled = false;
                var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                for (var i = 0; i < cameras.Length; i++)
                {
                    if (cameras[i] != null && cameras[i].gameObject.scene == career && cameras[i] != _cover)
                        cameras[i].enabled = true;
                }
                if (_cover != null)
                    _cover.enabled = false;
                return;
            }

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
                if (view.gameObject.scene != career)
                {
                    var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                    for (var i = 0; i < cameras.Length; i++)
                    {
                        if (cameras[i] != null && cameras[i].gameObject.scene == career)
                            cameras[i].enabled = false;
                    }
                }
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

        void KeepOneEventSystem()
        {
            var all = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            EventSystem keep = null;
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].gameObject.scene == gameObject.scene)
                {
                    keep = all[i];
                    break;
                }
            }
            if (keep == null && all.Length > 0)
                keep = all[0];
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i] != null)
                    all[i].enabled = all[i] == keep;
            }
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
            KeepOneEventSystem();
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
            if (_feed)
            {
                EventManager.UnSubscribe<PlayerShootEvent>(NoteShot);
                EventManager.UnSubscribe<PlayerChipShootEvent>(NoteChip);
                EventManager.UnSubscribe<GoalEvent>(NoteGoal);
                EventManager.UnSubscribe<PlayerPassEvent>(NotePass);
                EventManager.UnSubscribe<CornerEvent>(NoteCorner);
                EventManager.UnSubscribe<ThrowInEvent>(NoteThrow);
                EventManager.UnSubscribe<KeeperSavesTheBallEvent>(NoteSave);
                EventManager.UnSubscribe<BallHitTheWoodWorkEvent>(NoteWood);
            }
            Spectating = false;
            Running = false;
            PitchVisible = false;
            if (_map != null)
                _map.gameObject.SetActive(false);
            _pin = ViewPin.Auto;
            RestoreMenuPipeline();
            Time.timeScale = 1f;
        }
    }

    public sealed class MatchMinimapGraphic : Graphic
    {
        protected override void OnPopulateMesh(VertexHelper helper)
        {
            helper.Clear();
            var area = rectTransform.rect;
            AddRect(helper, area, new Color(0.02f, 0.08f, 0.05f, 0.82f));
            var match = MatchManager.Current;
            if (match == null)
                return;
            var size = match.SizeOfField;
            if (size.x < 1f || size.y < 1f)
                return;
            var flip = match.UserTeam != null && match.UserTeam == match.GameTeam2;
            var pad = 12f;
            Vector2 Map(float x, float z)
            {
                var nx = Mathf.Clamp01(x / size.x);
                if (flip)
                    nx = 1f - nx;
                var nz = Mathf.Clamp01(z / size.y);
                return new Vector2(
                    Mathf.Lerp(area.xMin + pad, area.xMax - pad, nx),
                    Mathf.Lerp(area.yMin + pad, area.yMax - pad, nz));
            }

            var line = new Color(1f, 1f, 1f, 0.55f);
            var corner = Map(0f, 0f);
            var far = Map(size.x, size.y);
            AddRect(helper, Rect.MinMaxRect(corner.x, corner.y, far.x, corner.y + 1.5f), line);
            AddRect(helper, Rect.MinMaxRect(corner.x, far.y - 1.5f, far.x, far.y), line);
            AddRect(helper, Rect.MinMaxRect(corner.x, corner.y, corner.x + 1.5f, far.y), line);
            AddRect(helper, Rect.MinMaxRect(far.x - 1.5f, corner.y, far.x, far.y), line);
            var mid = Map(size.x * 0.5f, 0f);
            AddRect(helper, Rect.MinMaxRect(mid.x - 0.8f, corner.y, mid.x + 0.8f, far.y), line);
            var centre = Map(size.x * 0.5f, size.y * 0.5f);
            AddDot(helper, centre, 10f, line);

            void dots(GameTeam team, Color color)
            {
                if (team == null || team.GamePlayers == null)
                    return;
                for (var i = 0; i < team.GamePlayers.Length; i++)
                {
                    var player = team.GamePlayers[i];
                    if (player == null)
                        continue;
                    var at = Map(player.Position.x, player.Position.z);
                    var mine = player.isInputControlled;
                    AddDot(helper, at, mine ? 7.5f : 4.6f, mine ? new Color(1f, 0.86f, 0.15f, 1f) : color);
                }
            }

            dots(match.GameTeam1, Color.white);
            dots(match.GameTeam2, new Color(0.25f, 0.62f, 1f, 1f));
            var ball = Ball.Current;
            if (ball != null)
                AddDot(helper, Map(ball.transform.position.x, ball.transform.position.z), 3.4f, new Color(1f, 0.95f, 0.4f, 1f));
        }

        static void AddRect(VertexHelper helper, Rect rect, Color color)
        {
            var i = helper.currentVertCount;
            helper.AddVert(new Vector3(rect.xMin, rect.yMin), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMin, rect.yMax), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax, rect.yMax), color, Vector2.zero);
            helper.AddVert(new Vector3(rect.xMax, rect.yMin), color, Vector2.zero);
            helper.AddTriangle(i, i + 1, i + 2);
            helper.AddTriangle(i, i + 2, i + 3);
        }

        static void AddDot(VertexHelper helper, Vector2 centre, float radius, Color color)
        {
            AddRect(helper, new Rect(centre.x - radius, centre.y - radius, radius * 2f, radius * 2f), color);
        }
    }
}
