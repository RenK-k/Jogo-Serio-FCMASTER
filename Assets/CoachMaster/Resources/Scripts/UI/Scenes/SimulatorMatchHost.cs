using System.Collections;
using CoachMaster.MatchPlay;
using FStudio.Data;
using FStudio.Events;
using FStudio.Graphics.Cameras;
using FStudio.MatchEngine;
using FStudio.MatchEngine.Balls;
using FStudio.MatchEngine.Enums;
using FStudio.MatchEngine.Events;
using FStudio.MatchEngine.Tactics;
using FStudio.UI;
using FStudio.UI.Events;
using Shared.Responses;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace CoachMaster.UI.Scenes
{
    public sealed class SimulatorMatchHost : MonoBehaviour
    {
        public static bool Running { get; private set; }
        public static bool Spectating { get; private set; }
        MatchSession _match;
        bool _hooked;
        bool _shots;
        bool _attack;
        float _dangerUntil;
        Camera _cover;

        public void Begin(MatchSession match)
        {
            _match = match;
            Running = true;
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
            _attack = false;
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(true);
            HideMatchScreens();
            SetMatchCamera(false, gameObject.scene, Cover());
            Restore<EventSystem>(gameObject.scene);
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

        public void SetAttack(bool attack)
        {
            if (_attack == attack)
                return;
            _attack = attack;
            var design = transform.Find("Design");
            if (design != null)
                design.gameObject.SetActive(!attack);
            SetMatchCamera(attack, gameObject.scene, Cover());
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

        static void SetMatchCamera(bool show, Scene career, Camera cover)
        {
            if (cover != null)
                cover.enabled = !show;
            var cameras = Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < cameras.Length; i++)
            {
                var camera = cameras[i];
                if (camera == null || camera == cover || camera.gameObject.scene == career)
                    continue;
                var main = MainCamera.Current != null && camera == MainCamera.Current.Camera;
                camera.enabled = show && main;
            }
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
            Restore<EventSystem>(career);
            var day = GetComponent<MatchDayController>();
            if (day != null)
                day.ResumeSimulation();
        }

        IEnumerator Open()
        {
            SimulatorCareer.Prepare(_match);
            if (!gameObject.GetComponent<ModernStadiumDress>())
                gameObject.AddComponent<ModernStadiumDress>();
            var loaded = SceneManager.GetSceneByName("_StartingScene");
            if (!loaded.isLoaded)
            {
                var load = SceneManager.LoadSceneAsync("_StartingScene", LoadSceneMode.Additive);
                if (load == null)
                    yield break;
                while (!load.isDone)
                    yield return null;
            }

            for (var i = 0; i < 30 && UILoader.Current == null; i++)
                yield return null;
            if (UILoader.Current == null || _match == null)
                yield break;
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
            var request = new MatchCreateRequest(SimulatorCareer.BuildTeam(_match, true), SimulatorCareer.BuildTeam(_match, false));
            request.aiLevel = AILevel.Legendary;
            request.userTeam = MatchCreateRequest.UserTeam.None;
            request.dayTime = DayTimes.Afternoon;
            var match = MatchEngineLoader.CreateMatch(request);
            while (!match.IsCompleted)
                yield return null;
            if (match.IsFaulted)
            {
                Debug.LogException(match.Exception);
                ShowCareerMatch();
            }
            PreferSimulator();
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
            Restore<EventSystem>(career);
        }

        static void Restore<T>(Scene career) where T : Behaviour
        {
            var all = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < all.Length; i++)
                all[i].enabled = all[i].gameObject.scene == career;
        }

        void Complete(MatchCompleteEvent _)
        {
            Spectating = false;
            _attack = false;
            if (_match == null || MatchManager.Current == null)
                return;
            _match.HomeGoals = MatchManager.Current.homeTeamScore;
            _match.AwayGoals = MatchManager.Current.awayTeamScore;
            _match.Minute = 90f;
            _match.Finished = true;
            _match.Phase = MatchPhase.FullTime;
            Running = false;
            SetMatchCamera(false, gameObject.scene, Cover());
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
        }
    }
}
