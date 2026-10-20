using System;
using System.Collections.Generic;
using CoachMaster.App;
using CoachMaster.Career;
using CoachMaster.Data;
using CoachMaster.Domain;
using CoachMaster.MatchPlay;
using CoachMaster.Presentation;
using CoachMaster.UI;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace CoachMaster.UI.Scenes
{
    public sealed class MatchDayController : MonoBehaviour, IControlBackHandler
    {
        enum BoardTab
        {
            Fitness,
            Ratings,
            Stats,
            Plan
        }

        MatchSession _match;
        MatchEngine _engine;
        CareerState _career;
        bool _paused;
        float _speed = 0.68f;
        BoardTab _tab;
        bool _homeXi = true;
        bool _awayXi = true;
        int _subOff = 10;
        int _subOn;
        int _noteCount = -1;
        bool _snap = true;
        float _slow;

        Text _score;
        Text _clock;
        Text _ticker;
        RectTransform _pitch;
        RectTransform _ball;
        Image _ballImage;
        Image _ballShadow;
        Color _homeColor = Color.white;
        Color _awayColor = new Color(0.85f, 0.2f, 0.2f);
        Token[] _homeTokens;
        Token[] _awayTokens;
        readonly Vector2[] _tokenPoints = new Vector2[22];
        readonly Vector2[] _tokenOffsets = new Vector2[22];
        readonly Vector2[] _tokenRenderOffsets = new Vector2[22];
        SideColumn _homeCol;
        SideColumn _awayCol;
        GameObject _fitnessPage;
        GameObject _ratingsPage;
        GameObject _statsPage;
        GameObject _pause;
        GameObject _shape;
        RectTransform _shapeHost;
        Button[] _formationButtons;
        GameObject _halfTime;
        GameObject _fullTime;
        GameObject _footer;
            Text _pauseBody;
            Text _fullResult;
        Text _flash;
        Image _eventFace;
        Image _eventStrip;
        Text _eventWho;
        GameObject _eventCard;
        int _flashSeen = -1;
        float _flashLeft;
        string _heard;
        bool _halfWhistle;
        bool _fullWhistle;
        bool _matchFinalized;
        bool _analysisAtHalfTime;
        bool _analysisAtFullTime;
        bool _pauseAfterEvent;
        GameObject _tactics;
        GameObject _presetConfirm;
        int _pendingPresetIndex = -1;
        GameObject _subsMenu;
        RectTransform _subPitch;
        RectTransform _subBench;
        Text _subStatus;
        Button[] _pressButtons;
        Button[] _lineButtons;
        Button[] _ideaButtons;
        Button[] _presetButtons;
        Text _tacticNote;
        RectTransform _roleHost;
        RectTransform _tacticPitch;
        RectTransform _tacticOrders;
        string _tacticFocus;
        bool _watchEvent;
        Text _halfSummary;
        Text _managerSpeech;
        Button[] _teamTalkButtons;
        Button _continueSecondHalf;
        Button _halfTactics;
        Text _plan;
        RectTransform _locker;
        string[] _pressOptions;
        string[] _lineOptions;
        string[] _ideaOptions;
        string _queueOff;
        string _queueOn;
        bool _subFromPause;
        bool _tacticsFromHalfTime;
        RectTransform _timeline;
        Text _homePoss;
        Text _awayPoss;
        Text _homeShots;
        Text _awayShots;
        Text _homeChances;
        Text _awayChances;
        Text _homeRatings;
        Text _awayRatings;
        Button[] _tabs;
        Text _speedLabel;
        MatchAnalysisPanel _analysis;

        void Start()
        {
            SquadDragCard.Release();
            _match = GameRoot.Require().LiveMatch;
            if (_match == null)
            {
                GameRoot.Instance.Open(SceneNames.CareerHub);
                return;
            }

            _career = GameRoot.Instance.Session.Career;
            SettingsPrefs.MatchSpeedIndex = 1;
            SettingsPrefs.Commit();
            _speed = SettingsPrefs.MatchSpeed;
            _engine = new MatchEngine(_match,
                _career != null && _career.Coach != null ? _career.Coach.ClubId : null,
                _career == null ? 1f : SettingsPrefs.InjuryMultiplier(_career.InjuryFrequency),
                _career == null ? 1f : SettingsPrefs.RefereeMultiplier(_career.RefereeStrictness));
            GameAudio.Kickoff();
            GameAudio.StartCrowd();
            var design = transform.Find("Design");
            if (design != null)
            {
                BroadcastUi.Mount(this);
                BindMatch(design);
                Show(BoardTab.Fitness);
                return;
            }

            Build();
        }

        public void BakeDesign(CareerState career, MatchSession match)
        {
            _career = career;
            _match = match;
            BuildInto(BroadcastUi.CreateDesign(transform));
        }

        void Update()
        {
            if (_match == null)
                return;

            if (_match.Phase == MatchPhase.HalfTime)
            {
                if (!_halfWhistle)
                {
                    _halfWhistle = true;
                    GameAudio.Whistles(2);
                    FillLocker();
                }
                var changing = _subsMenu != null && _subsMenu.activeSelf;
                var reviewingTactics = _tactics != null && _tactics.activeSelf;
                _halfTime.SetActive(!changing && !reviewingTactics);
                if (_continueSecondHalf != null)
                    _continueSecondHalf.gameObject.SetActive(_match.TeamTalkApplied && !changing && !reviewingTactics);
                if (_teamTalkButtons != null)
                    for (var i = 0; i < _teamTalkButtons.Length; i++)
                        if (_teamTalkButtons[i] != null)
                            _teamTalkButtons[i].interactable = !_match.TeamTalkApplied;
                if (changing)
                    _subsMenu.transform.SetAsLastSibling();
                else if (reviewingTactics)
                    _tactics.transform.SetAsLastSibling();
                _footer.SetActive(false);
                if (_analysisAtHalfTime)
                {
                    _halfTime.SetActive(false);
                    if (_analysis != null)
                    {
                        _analysis.SetReturnAction(ReturnToHalfTime, true, "BACK TO TEAM TALK", "VOLTAR AO BALNEÁRIO");
                        _analysis.Refresh();
                    }
                    RefreshHud();
                    return;
                }
                RefreshHud();
                TryQueuedSub();
                return;
            }

            if (_match.Phase == MatchPhase.FullTime)
            {
                if (!_fullWhistle)
                {
                    _fullWhistle = true;
                    GameAudio.Whistles(3);
                    GameAudio.StopCrowd();
                }
                _fullTime.SetActive(!_analysisAtFullTime);
                _footer.SetActive(false);
                if (_analysisAtFullTime)
                {
                    if (_analysis != null)
                    {
                        _analysis.SetBackToResult(ReturnToFullTimeResult, true);
                        _analysis.Refresh();
                    }
                    RefreshHud();
                    return;
                }
                _fullResult.text = _match.Home.ShortName + "   " + _match.HomeGoals + "  -  " + _match.AwayGoals + "   " + _match.Away.ShortName;
                RefreshHud();
                return;
            }

            _halfTime.SetActive(false);
            _fullTime.SetActive(false);
            _footer.SetActive(true);
            var overlay = (_pause != null && _pause.activeSelf) || (_tactics != null && _tactics.activeSelf) || (_subsMenu != null && _subsMenu.activeSelf);
            if (_paused && !overlay)
            {
                _paused = false;
                SetFooter(true);
            }

            if (!_paused && !_watchEvent && !AudioSettingsOverlay.IsVisible)
                _engine.Tick(Time.deltaTime, _speed);
            TryQueuedSub();

            PlaceTokens();
            RefreshHud();
            if (!AudioSettingsOverlay.IsVisible)
                _slow -= Time.deltaTime;
            if (_slow <= 0f)
            {
                _slow = 0.3f;
                RefreshSides();
                RefreshRatings();
                RefreshStats();
                RefreshPlan();
            }
        }

        public bool HandleControlBack()
        {
            if (_presetConfirm != null) { ClosePresetConfirm(false); return true; }
            if (_subsMenu != null && _subsMenu.activeSelf) { CloseSubs(); return true; }
            if (_tactics != null && _tactics.activeSelf) { CloseTactics(); return true; }
            if (_shape != null && _shape.activeSelf)
            {
                _shape.SetActive(false);
                if (_tactics != null) _tactics.SetActive(true);
                return true;
            }
            if (_pause != null && _pause.activeSelf) { ResumeMatch(); return true; }
            if (_fullTime != null && _fullTime.activeSelf) { FinishMatch(); return true; }
            if (_analysisAtHalfTime) { ReturnToHalfTime(); return true; }
            if (_analysisAtFullTime) { ReturnToFullTimeResult(); return true; }
            if (_analysis != null && _analysis.IsVisible) { Show(BoardTab.Fitness); return true; }
            if (_match != null && _match.Phase != MatchPhase.HalfTime && _match.Phase != MatchPhase.FullTime)
            {
                OpenPause();
                return true;
            }
            return false;
        }

        string LeagueTitle(bool champions)
        {
            if (champions)
                return "Liga dos Campeões";
            if (_match.Fixture != null && _career != null && _career.World != null)
            {
                foreach (var league in _career.World.Leagues)
                {
                    if (league.Id == _match.Fixture.LeagueId)
                        return league.Name;
                }
            }

            return _career != null && _career.League != null ? _career.League.Name : "Liga";
        }

        void Build()
        {
            var root = BroadcastUi.Mount(this);
            BuildInto(root);
        }

        void BindMatch(Transform root)
        {
            var homeName = SceneBind.Get<Text>(root, "Scoreboard/Home/Name");
            if (homeName != null)
                homeName.text = _match.Home.ShortName;
            var awayName = SceneBind.Get<Text>(root, "Scoreboard/Away/Name");
            if (awayName != null)
                awayName.text = _match.Away.ShortName;
            var homeCrest = SceneBind.Get<Image>(root, "Scoreboard/Home/Crest");
            if (homeCrest != null)
                homeCrest.sprite = CrestStudio.For(_match.Home);
            var awayCrest = SceneBind.Get<Image>(root, "Scoreboard/Away/Crest");
            if (awayCrest != null)
                awayCrest.sprite = CrestStudio.For(_match.Away);
            LeagueBackdrop.Paint(root, LeagueBackdrop.ForFixture(_match.Fixture == null ? null : _match.Fixture.LeagueId, _career.Club.LeagueId));
            var leagueLabel = SceneBind.Get<Text>(root, "Wordmark/League");
            if (leagueLabel != null)
            {
                var champions = _match.Fixture != null && _match.Fixture.LeagueId != null && _match.Fixture.LeagueId.Contains("campeoes");
                leagueLabel.text = LeagueTitle(champions).ToUpperInvariant();
            }
            _score = SceneBind.Get<Text>(root, "Scoreboard/Score");
            _clock = SceneBind.Get<Text>(root, "Scoreboard/Clock");
            _plan = SceneBind.Get<Text>(root, "Plan");
            _ticker = SceneBind.Get<Text>(root, "Ticker");
            _flash = SceneBind.Get<Text>(root, "Flash");
            _eventCard = root.Find("EventCard").gameObject;
            _eventFace = SceneBind.Get<Image>(root, "EventCard/EventFace");
            _eventStrip = SceneBind.Get<Image>(root, "EventCard/Strip");
            _eventWho = SceneBind.Get<Text>(root, "EventCard/Who");
            _fitnessPage = root.Find("Fitness").gameObject;
            _homeCol = CaptureColumn(root.Find("Fitness/HomeXi"), true);
            _awayCol = CaptureColumn(root.Find("Fitness/AwayXi"), false);
            _pitch = root.Find("Fitness/PitchZone/Pitch") as RectTransform;
            _homeColor = BroadcastUi.Parse(_match.Home.JerseyPrimaryHex, Color.white);
            _awayColor = BroadcastUi.Parse(_match.Away.JerseyPrimaryHex, new Color(0.85f, 0.2f, 0.2f));
            _homeTokens = CaptureTokens(true, _homeColor);
            _awayTokens = CaptureTokens(false, _awayColor);
            var ball = root.Find("Fitness/PitchZone/Pitch/Ball");
            if (ball != null)
            {
                _ball = ball as RectTransform;
                _ballImage = ball.GetComponent<Image>();
            }

            var shadow = root.Find("Fitness/PitchZone/Pitch/BallShadow");
            if (shadow != null)
                _ballShadow = shadow.GetComponent<Image>();
            _ratingsPage = root.Find("Ratings").gameObject;
            _homeRatings = SceneBind.Get<Text>(root, "Ratings/HomeRatings");
            _awayRatings = SceneBind.Get<Text>(root, "Ratings/AwayRatings");
            _statsPage = root.Find("Stats").gameObject;
            _homePoss = SceneBind.Get<Text>(root, "Stats/HomePoss/Value");
            _homeShots = SceneBind.Get<Text>(root, "Stats/HomeShots/Value");
            _homeChances = SceneBind.Get<Text>(root, "Stats/HomeChances/Value");
            _awayPoss = SceneBind.Get<Text>(root, "Stats/AwayPoss/Value");
            _awayShots = SceneBind.Get<Text>(root, "Stats/AwayShots/Value");
            _awayChances = SceneBind.Get<Text>(root, "Stats/AwayChances/Value");
            _timeline = root.Find("Stats/TimelineCard/Timeline/Content") as RectTransform;
            _analysis = MatchAnalysisPanel.Attach(root, _match);
            if (_analysis != null) _analysis.SetDismissAction(DismissMatchAnalysis);
            _tabs = new Button[3];
            _tabs[0] = SceneBind.Wire(root.Find("Tabs/TabFit"), () => Show(BoardTab.Fitness));
            _tabs[1] = SceneBind.Wire(root.Find("Tabs/TabRate"), () => Show(BoardTab.Ratings));
            _tabs[2] = SceneBind.Wire(root.Find("Tabs/TabStats"), () => Show(BoardTab.Stats));
            if (_tabs[1] != null)
                _tabs[1].gameObject.SetActive(SettingsPrefs.LiveRatings);
            _footer = root.Find("Footer").gameObject;
            SceneBind.Wire(root.Find("Footer/Pause"), OpenPause);
            SceneBind.Wire(root.Find("Footer/QuickTactics"), OpenTactics);
            SceneBind.Wire(root.Find("Footer/QuickSub"), () => OpenSubs(false));
            var speed = SceneBind.Wire(root.Find("Footer/Speed"), CycleSpeed);
            _speedLabel = speed == null ? null : speed.GetComponentInChildren<Text>();
            if (_speedLabel != null)
                _speedLabel.text = MatchSpeedText();
            _pause = root.Find("PausePanel").gameObject;
            _pauseBody = SceneBind.Get<Text>(root, "PausePanel/Card/Tactics");
            SceneBind.Wire(root.Find("PausePanel/Dim"), ResumeMatch);
            SceneBind.Wire(root.Find("PausePanel/Card/Resume"), ResumeMatch);
            SceneBind.Wire(root.Find("PausePanel/Card/OpenTactics"), OpenTactics);
            SceneBind.Wire(root.Find("PausePanel/Card/OpenSubs"), () => OpenSubs(true));
            EnsurePopupClose(root.Find("PausePanel/Card"), "ClosePause", ResumeMatch);
            EnsureSkipMatchButton(root);
            _tactics = root.Find("TacticsPanel").gameObject;
            _tacticNote = SceneBind.Get<Text>(root, "TacticsPanel/Card/Note");
            _roleHost = root.Find("TacticsPanel/Card/RoleList") as RectTransform;
            EnsureLiveTactics();
            EnsurePopupClose(root.Find("TacticsPanel/Card"), "CloseTactics", CloseTactics);
            var tacticsDim = root.Find("TacticsPanel/Dim")?.GetComponent<Image>();
            if (tacticsDim != null)
            {
                var existingDimButton = tacticsDim.GetComponent<Button>();
                if (existingDimButton != null) SceneBind.Wire(existingDimButton.transform, CloseTactics);
                else BroadcastUi.Hit(tacticsDim, CloseTactics);
            }
            _presetButtons = new Button[TacticPresets.Names.Length];
            for (var i = 0; i < _presetButtons.Length; i++)
            {
                var index = i;
                _presetButtons[i] = SceneBind.Wire(root.Find("TacticsPanel/Card/Preset" + i), () => RequestPreset(index));
            }

            _shape = root.Find("Shape").gameObject;
            _shapeHost = root.Find("Shape/Card/Players") as RectTransform;
            EnsurePopupClose(root.Find("Shape/Card"), "CloseShape", () =>
            {
                _shape.SetActive(false);
                if (_tactics != null)
                    _tactics.SetActive(true);
            });
            SceneBind.Wire(root.Find("Shape/Dim"), () =>
            {
                _shape.SetActive(false);
                if (_tactics != null) _tactics.SetActive(true);
            });
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            _formationButtons = new Button[forms.Length];
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                _formationButtons[i] = SceneBind.Wire(root.Find("Shape/Card/Form" + i), () => PickFormation(formation));
            }

            _halfTime = root.Find("HalfTime").gameObject;
            _locker = root.Find("HalfTime/Card/Voices") as RectTransform;
            SceneBind.Wire(root.Find("HalfTime/Card/LeaveAssistant"), LeaveToAssistant);
            SceneBind.Wire(root.Find("HalfTime/Card/HalfSubs"), () => OpenSubs(true));
            EnsureHalfTalkUi(root.Find("HalfTime/Card"));
            EnsureHalfTimeAnalysisAction(root.Find("HalfTime/Card"));
            _fullTime = root.Find("FullTime").gameObject;
            _fullResult = SceneBind.Get<Text>(root, "FullTime/Card/Result");
            EnsurePopupClose(root.Find("FullTime/Card"), "CloseFullTime", FinishMatch);
            SceneBind.Wire(root.Find("FullTime/Card/Continue"), FinishMatch);
            EnsureFullTimeAnalysisAction(root.Find("FullTime/Card"));
            _subsMenu = root.Find("SubsMenu").gameObject;
            _subStatus = SceneBind.Get<Text>(root, "SubsMenu/Card/Status");
            _subPitch = root.Find("SubsMenu/Card/Pitch") as RectTransform;
            _subBench = root.Find("SubsMenu/Card/Bench") as RectTransform;
            var subCard = root.Find("SubsMenu/Card");
            var pitchBg = subCard == null ? null : subCard.Find("PitchBg") as RectTransform;
            if (pitchBg != null)
            {
                BroadcastUi.Place(pitchBg, 0.365f, 0.29f, 0.635f, 0.86f);
                for (var i = pitchBg.childCount - 1; i >= 0; i--)
                    Destroy(pitchBg.GetChild(i).gameObject);
                BroadcastUi.DrawVerticalHalfPitch(pitchBg, new Color(0.12f, 0.46f, 0.22f));
            }
            if (_subPitch != null)
                BroadcastUi.Place(_subPitch, 0.365f, 0.29f, 0.635f, 0.86f);
            if (_subBench != null)
                BroadcastUi.Place(_subBench, 0.04f, 0.05f, 0.96f, 0.25f);
            var benchTitle = subCard == null ? null : SceneBind.Get<Text>(subCard, "BenchTitle");
            if (benchTitle != null)
                BroadcastUi.Place(benchTitle.rectTransform, 0.04f, 0.25f, 0.3f, 0.31f);
            EnsurePopupClose(subCard, "CloseSubs", CloseSubs);
            SceneBind.Wire(root.Find("SubsMenu/Dim"), CloseSubs);
        }

        SideColumn CaptureColumn(Transform area, bool home)
        {
            var col = new SideColumn();
            col.Root = area.gameObject;
            col.Xi = SceneBind.Wire(area.Find(home ? "HomeXiTab" : "AwayXiTab"), () =>
            {
                if (home) _homeXi = true;
                else _awayXi = true;
                _slow = 0f;
            });
            col.Bench = SceneBind.Wire(area.Find(home ? "HomeBenchTab" : "AwayBenchTab"), () =>
            {
                if (home) _homeXi = false;
                else _awayXi = false;
                _slow = 0f;
            });
            col.Numbers = new Text[11];
            col.Names = new Text[11];
            col.Fills = new Image[11];
            col.Scores = new Text[11];
            col.Marks = new Text[11];
            col.Cards = new Image[11];
            col.Balls = new Image[11];
            col.GoalMarks = new Text[11];
            col.Boots = new Image[11];
            col.Pens = new Text[11];
            col.Rows = new GameObject[11];
            for (var i = 0; i < 11; i++)
            {
                var row = area.Find("Row" + i);
                col.Rows[i] = row.gameObject;
                col.Numbers[i] = row.Find("Num").GetComponent<Text>();
                col.Names[i] = row.Find("Name").GetComponent<Text>();
                col.Marks[i] = row.Find("Card").GetComponent<Text>();
                col.Cards[i] = row.Find("CardRect").GetComponent<Image>();
                col.Balls[i] = row.Find("Ball").GetComponent<Image>();
                col.GoalMarks[i] = row.Find("Ball/Goals").GetComponent<Text>();
                col.Boots[i] = row.Find("Boot").GetComponent<Image>();
                col.Pens[i] = row.Find("Pen").GetComponent<Text>();
                col.Fills[i] = row.Find("Bar/Fill").GetComponent<Image>();
                col.Scores[i] = row.Find("Score").GetComponent<Text>();
            }

            return col;
        }

        Token[] CaptureTokens(bool home, Color color)
        {
            var list = new Token[11];
            for (var i = 0; i < 11; i++)
            {
                var rect = _pitch.Find((home ? "H" : "A") + i) as RectTransform;
                var image = rect.GetComponent<Image>();
                image.color = color;
                var number = rect.Find("N").GetComponent<Text>();
                number.color = NumberInk(color);
                number.resizeTextForBestFit = true;
                number.resizeTextMinSize = 11;
                number.resizeTextMaxSize = 18;
                var injury = rect.Find("Injury") == null
                    ? BroadcastUi.Label(rect, "Injury", "+", 15, new Color(0.95f, 0.12f, 0.16f), TextAnchor.MiddleCenter, FontStyle.Bold)
                    : rect.Find("Injury").GetComponent<Text>();
                BroadcastUi.Place(injury.rectTransform, 0.62f, 0.62f, 1f, 1f);
                injury.gameObject.SetActive(false);
                list[i] = new Token { Rect = rect, Number = number, Injury = injury };
            }

            return list;
        }

        void BuildInto(Transform root)
        {
            var artId = LeagueBackdrop.ForFixture(_match.Fixture == null ? null : _match.Fixture.LeagueId, _career.Club.LeagueId);
            var champions = artId != null && artId.Contains("campeoes");
            if (LeagueBackdrop.First(artId) != null)
            {
                BroadcastUi.Backdrop(root);
                LeagueBackdrop.Paint(root, artId);
            }
            else if (champions)
                BroadcastUi.ChampionsNight(root);
            else
                BroadcastUi.Backdrop(root);
            BroadcastUi.Wordmark(root, LeagueTitle(champions));
            BuildScoreboard(root);
            BuildFitness(root);
            BuildRatings(root);
            BuildStats(root);
            BuildTabs(root);
            BuildFooter(root);
            BuildPause(root);
            BuildTactics(root);
            BuildShape(root);
            BuildSubs(root);
            BuildHalf(root);
            BuildFull(root);
            Show(BoardTab.Fitness);
        }

        void BuildScoreboard(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Scoreboard", 0.04f, 0.8f, 0.96f, 0.91f);
            var home = BroadcastUi.Zone(bar, "Home", 0.02f, 0.08f, 0.36f, 0.92f);
            TeamSide(home, _match.Home, true);
            _score = BroadcastUi.Label(bar, "Score", "0  -  0", 54, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_score.rectTransform, 0.36f, 0.28f, 0.64f, 0.92f);
            _clock = BroadcastUi.Label(bar, "Clock", "0:00", 22, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_clock.rectTransform, 0.36f, 0.02f, 0.64f, 0.34f);
            var away = BroadcastUi.Zone(bar, "Away", 0.64f, 0.08f, 0.98f, 0.92f);
            TeamSide(away, _match.Away, false);
            _plan = BroadcastUi.Label(root, "Plan", "", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_plan.rectTransform, 0.04f, 0.755f, 0.2f, 0.795f);
            _ticker = BroadcastUi.Label(root, "Ticker", "", 16, UiTheme.Muted, TextAnchor.MiddleCenter);
            BroadcastUi.Place(_ticker.rectTransform, 0.2f, 0.755f, 0.82f, 0.795f);
            _flash = BroadcastUi.Label(root, "Flash", "", 42, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_flash.rectTransform, 0.3f, 0.58f, 0.7f, 0.7f);
            _flash.gameObject.SetActive(false);
            _eventCard = BroadcastUi.Panel(root, "EventCard", new Color(0.08f, 0.03f, 0.16f, 0.97f), true).gameObject;
            BroadcastUi.Place(_eventCard.GetComponent<RectTransform>(), 0.22f, 0.32f, 0.78f, 0.68f);
            var faceGo = new GameObject("EventFace", typeof(RectTransform), typeof(Image));
            var faceRect = faceGo.GetComponent<RectTransform>();
            faceRect.SetParent(_eventCard.transform, false);
            faceRect.anchorMin = faceRect.anchorMax = new Vector2(0.12f, 0.56f);
            faceRect.sizeDelta = new Vector2(112, 112);
            _eventFace = faceGo.GetComponent<Image>();
            _eventFace.preserveAspect = true;
            _eventStrip = BroadcastUi.Solid(_eventCard.transform, "Strip", new Color(0.95f, 0.82f, 0.12f));
            BroadcastUi.Place(_eventStrip.rectTransform, 0.485f, 0.22f, 0.505f, 0.78f);
            _eventStrip.gameObject.SetActive(false);
            _eventWho = BroadcastUi.Label(_eventCard.transform, "Who", "", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(_eventWho.rectTransform, 0.23f, 0.48f, 0.51f, 0.72f);
            _eventCard.SetActive(false);
        }

        void EnsureSkipMatchButton(Transform root)
        {
            var buttonTransform = root.Find("PausePanel/Card/SimulateRest");
            if (buttonTransform == null)
            {
                var card = root.Find("PausePanel/Card");
                if (card == null)
                    return;
                var button = BroadcastUi.Button(card, "SimulateRest", "SIMULAR RESTO DO JOGO", SimulateRest,
                    new Color(0.12f, 0.38f, 0.3f, 1f), Color.white, 15);
                buttonTransform = button.transform;
            }

            SceneBind.Wire(buttonTransform, SimulateRest);
            BroadcastUi.Place(buttonTransform as RectTransform, 0.12f, 0.18f, 0.88f, 0.28f);
            var resume = root.Find("PausePanel/Card/Resume") as RectTransform;
            if (resume != null)
                BroadcastUi.Place(resume, 0.12f, 0.055f, 0.88f, 0.16f);
            var tactics = root.Find("PausePanel/Card/OpenTactics") as RectTransform;
            if (tactics != null)
                BroadcastUi.Place(tactics, 0.12f, 0.31f, 0.48f, 0.43f);
            var subs = root.Find("PausePanel/Card/OpenSubs") as RectTransform;
            if (subs != null)
                BroadcastUi.Place(subs, 0.52f, 0.31f, 0.88f, 0.43f);
        }

        void SimulateRest()
        {
            if (_engine == null || _match == null || _match.Finished)
                return;

            _paused = false;
            _watchEvent = false;
            if (_pause != null) _pause.SetActive(false);
            if (_tactics != null) _tactics.SetActive(false);
            if (_subsMenu != null) _subsMenu.SetActive(false);
            if (_eventCard != null) _eventCard.SetActive(false);
            if (_flash != null) _flash.gameObject.SetActive(false);
            SetFooter(false);

            var ticks = 0;
            while (_match.Phase != MatchPhase.FullTime && !_match.Finished && ticks++ < 5000)
            {
                if (_match.Phase == MatchPhase.HalfTime)
                {
                    _engine.StartSecondHalf();
                    continue;
                }

                _engine.Tick(0.05f, 1f);
                TryQueuedSub();
            }

            _flashSeen = _match.FlashSerial;
            _watchEvent = false;
            _flashLeft = 0f;
            if (_eventCard != null) _eventCard.SetActive(false);
            if (_match.Phase == MatchPhase.FullTime || _match.Finished)
            {
                _fullTime.SetActive(true);
                _footer.SetActive(false);
                _fullResult.text = _match.Home.ShortName + "   " + _match.HomeGoals + "  -  " + _match.AwayGoals + "   " + _match.Away.ShortName;
            }
            RefreshHud();
        }

        void TeamSide(RectTransform area, Club club, bool home)
        {
            WorldCatalog.PaintClub(club);
            var crestGo = new GameObject("Crest", typeof(RectTransform), typeof(Image));
            var crestRect = crestGo.GetComponent<RectTransform>();
            crestRect.SetParent(area, false);
            crestRect.anchorMin = crestRect.anchorMax = new Vector2(home ? 0.78f : 0.22f, 0.5f);
            crestRect.sizeDelta = new Vector2(46, 46);
            var crestImage = crestGo.GetComponent<Image>();
            crestImage.raycastTarget = false;
            crestImage.preserveAspect = true;
            crestImage.sprite = CrestStudio.For(club);

            var dot = BroadcastUi.Solid(area, "Dot", BroadcastUi.Parse(club.JerseyPrimaryHex, Color.white));
            dot.sprite = BroadcastUi.Circle;
            dot.rectTransform.anchorMin = dot.rectTransform.anchorMax = new Vector2(home ? 0.62f : 0.38f, 0.5f);
            dot.rectTransform.sizeDelta = new Vector2(16, 16);

            var name = BroadcastUi.Label(area, "Name", club.ShortName, 26, Color.white, home ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, home ? 0f : 0.46f, 0.1f, home ? 0.56f : 1f, 0.9f);
        }

        void BuildFitness(Transform root)
        {
            _fitnessPage = BroadcastUi.Zone(root, "Fitness", 0f, 0.12f, 1f, 0.75f).gameObject;
            var page = _fitnessPage.transform;
            _homeCol = BuildColumn(page, "HomeXi", 0.015f, 0.02f, 0.22f, 0.98f, true);
            var pitchZone = BroadcastUi.Zone(page, "PitchZone", 0.23f, 0.03f, 0.77f, 0.97f);
            _pitch = BroadcastUi.Zone(pitchZone, "Pitch", 0.04f, 0.04f, 0.96f, 0.96f);
            BroadcastUi.DrawPitch(_pitch, new Color(0.12f, 0.46f, 0.22f));
            WorldCatalog.PaintClub(_match.Home);
            WorldCatalog.PaintClub(_match.Away);
            _homeTokens = SpawnTokens(true, BroadcastUi.Parse(_match.Home.JerseyPrimaryHex, Color.white));
            _awayTokens = SpawnTokens(false, BroadcastUi.Parse(_match.Away.JerseyPrimaryHex, new Color(0.85f, 0.2f, 0.2f)));
            _homeColor = BroadcastUi.Parse(_match.Home.JerseyPrimaryHex, Color.white);
            _awayColor = BroadcastUi.Parse(_match.Away.JerseyPrimaryHex, new Color(0.85f, 0.2f, 0.2f));
            _ball = SpawnBall();
            _awayCol = BuildColumn(page, "AwayXi", 0.78f, 0.02f, 0.985f, 0.98f, false);
        }

        SideColumn BuildColumn(Transform parent, string name, float x0, float y0, float x1, float y1, bool home)
        {
            var area = BroadcastUi.Zone(parent, name, x0, y0, x1, y1);
            var col = new SideColumn();
            col.Root = area.gameObject;
            var xi = BroadcastUi.Button(area, home ? "HomeXiTab" : "AwayXiTab", "11 Inicial", () =>
            {
                if (home) _homeXi = true;
                else _awayXi = true;
                _slow = 0f;
            }, new Color(1f, 1f, 1f, 0.08f), Color.white, 14);
            BroadcastUi.Place(xi.GetComponent<RectTransform>(), 0.02f, 0.9f, 0.5f, 0.99f);
            var bench = BroadcastUi.Button(area, home ? "HomeBenchTab" : "AwayBenchTab", "Banco", () =>
            {
                if (home) _homeXi = false;
                else _awayXi = false;
                _slow = 0f;
            }, new Color(1f, 1f, 1f, 0.08f), UiTheme.Muted, 14);
            BroadcastUi.Place(bench.GetComponent<RectTransform>(), 0.52f, 0.9f, 0.98f, 0.99f);
            col.Xi = xi;
            col.Bench = bench;
            col.Numbers = new Text[11];
            col.Names = new Text[11];
            col.Fills = new Image[11];
            col.Scores = new Text[11];
            col.Marks = new Text[11];
            col.Cards = new Image[11];
            col.Balls = new Image[11];
            col.GoalMarks = new Text[11];
            col.Boots = new Image[11];
            col.Pens = new Text[11];
            col.Rows = new GameObject[11];
            for (var i = 0; i < 11; i++)
            {
                var rowTop = 0.88f - i * 0.078f;
                var row = BroadcastUi.Zone(area, "Row" + i, 0.02f, rowTop - 0.07f, 0.98f, rowTop);
                col.Rows[i] = row.gameObject;
                if (home)
                {
                    col.Numbers[i] = BroadcastUi.Label(row, "Num", "", 16, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(col.Numbers[i].rectTransform, 0f, 0f, 0.16f, 1f);
                    col.Names[i] = BroadcastUi.Label(row, "Name", "", 16, Color.white, TextAnchor.MiddleLeft);
                    BroadcastUi.Place(col.Names[i].rectTransform, 0.16f, 0f, 0.5f, 1f);
                    col.Marks[i] = BroadcastUi.Label(row, "Card", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    col.Marks[i].gameObject.SetActive(false);
                    col.Cards[i] = BroadcastUi.Solid(row, "CardRect", new Color(0.95f, 0.82f, 0.15f));
                    BroadcastUi.Place(col.Cards[i].rectTransform, 0.5f, 0.2f, 0.548f, 0.8f);
                    col.Cards[i].gameObject.SetActive(false);
                    col.Balls[i] = BroadcastUi.Solid(row, "Ball", Color.white);
                    col.Balls[i].sprite = BroadcastUi.Circle;
                    col.Balls[i].rectTransform.anchorMin = col.Balls[i].rectTransform.anchorMax = new Vector2(0.58f, 0.5f);
                    col.Balls[i].rectTransform.sizeDelta = new Vector2(18, 18);
                    col.Balls[i].gameObject.SetActive(false);
                    col.GoalMarks[i] = BroadcastUi.Label(col.Balls[i].rectTransform, "Goals", "", 12, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(col.GoalMarks[i].rectTransform);
                    col.Boots[i] = BroadcastUi.Solid(row, "Boot", new Color(1f, 1f, 1f, 0f));
                    var assist = BroadcastUi.Label(col.Boots[i].rectTransform, "A", "A", 13, new Color(0.95f, 0.7f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(assist.rectTransform);
                    col.Boots[i].rectTransform.anchorMin = col.Boots[i].rectTransform.anchorMax = new Vector2(0.66f, 0.5f);
                    col.Boots[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    col.Boots[i].gameObject.SetActive(false);
                    col.Pens[i] = BroadcastUi.Label(row, "Pen", "", 14, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(col.Pens[i].rectTransform, 0.7f, 0f, 0.76f, 1f);
                    col.Fills[i] = Bar(row, 0.74f);
                    col.Scores[i] = BroadcastUi.Label(row, "Score", "", 16, UiTheme.Score, TextAnchor.MiddleRight, FontStyle.Bold);
                    BroadcastUi.Place(col.Scores[i].rectTransform, 0.72f, 0f, 0.98f, 1f);
                    col.Scores[i].gameObject.SetActive(false);
                }
                else
                {
                    col.Fills[i] = Bar(row, 0.02f);
                    col.Scores[i] = BroadcastUi.Label(row, "Score", "", 16, UiTheme.Score, TextAnchor.MiddleLeft, FontStyle.Bold);
                    BroadcastUi.Place(col.Scores[i].rectTransform, 0.02f, 0f, 0.24f, 1f);
                    col.Scores[i].gameObject.SetActive(false);
                    col.Marks[i] = BroadcastUi.Label(row, "Card", "", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                    col.Marks[i].gameObject.SetActive(false);
                    col.Pens[i] = BroadcastUi.Label(row, "Pen", "", 14, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Place(col.Pens[i].rectTransform, 0.08f, 0f, 0.16f, 1f);
                    col.Boots[i] = BroadcastUi.Solid(row, "Boot", new Color(1f, 1f, 1f, 0f));
                    var assistAway = BroadcastUi.Label(col.Boots[i].rectTransform, "A", "A", 13, new Color(0.95f, 0.7f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(assistAway.rectTransform);
                    col.Boots[i].rectTransform.anchorMin = col.Boots[i].rectTransform.anchorMax = new Vector2(0.2f, 0.5f);
                    col.Boots[i].rectTransform.sizeDelta = new Vector2(16, 16);
                    BroadcastUi.Place(col.Boots[i].rectTransform, 0.16f, 0.22f, 0.24f, 0.78f);
                    col.Boots[i].gameObject.SetActive(false);
                    col.Balls[i] = BroadcastUi.Solid(row, "Ball", Color.white);
                    col.Balls[i].sprite = BroadcastUi.Circle;
                    col.Balls[i].rectTransform.anchorMin = col.Balls[i].rectTransform.anchorMax = new Vector2(0.28f, 0.5f);
                    col.Balls[i].rectTransform.sizeDelta = new Vector2(18, 18);
                    col.Balls[i].gameObject.SetActive(false);
                    col.GoalMarks[i] = BroadcastUi.Label(col.Balls[i].rectTransform, "Goals", "", 12, new Color(0.08f, 0.08f, 0.1f), TextAnchor.MiddleCenter, FontStyle.Bold);
                    BroadcastUi.Stretch(col.GoalMarks[i].rectTransform);
                    col.Cards[i] = BroadcastUi.Solid(row, "CardRect", new Color(0.95f, 0.82f, 0.15f));
                    BroadcastUi.Place(col.Cards[i].rectTransform, 0.34f, 0.2f, 0.388f, 0.8f);
                    col.Cards[i].gameObject.SetActive(false);
                    col.Names[i] = BroadcastUi.Label(row, "Name", "", 16, Color.white, TextAnchor.MiddleRight);
                    BroadcastUi.Place(col.Names[i].rectTransform, 0.4f, 0f, 0.84f, 1f);
                    col.Numbers[i] = BroadcastUi.Label(row, "Num", "", 16, Color.white, TextAnchor.MiddleRight, FontStyle.Bold);
                    BroadcastUi.Place(col.Numbers[i].rectTransform, 0.84f, 0f, 1f, 1f);
                }
            }

            return col;
        }

        Image Bar(Transform row, float x)
        {
            var bg = BroadcastUi.Solid(row, "Bar", new Color(1f, 1f, 1f, 0.16f));
            BroadcastUi.Place(bg.rectTransform, x, 0.38f, x + 0.22f, 0.62f);
            var fill = BroadcastUi.Solid(bg.transform, "Fill", UiTheme.Fitness);
            BroadcastUi.Stretch(fill.rectTransform);
            fill.rectTransform.anchorMax = new Vector2(0.8f, 1f);
            return fill;
        }

        Token[] SpawnTokens(bool home, Color color)
        {
            var list = new Token[11];
            var ink = NumberInk(color);
            for (var i = 0; i < 11; i++)
            {
                var go = new GameObject((home ? "H" : "A") + i, typeof(RectTransform), typeof(Image));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(_pitch, false);
                rect.sizeDelta = new Vector2(30, 30);
                var image = go.GetComponent<Image>();
                image.sprite = BroadcastUi.Circle;
                image.color = color;
                image.raycastTarget = false;
                var num = BroadcastUi.Label(rect, "N", "", 18, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                num.resizeTextForBestFit = true;
                num.resizeTextMinSize = 11;
                num.resizeTextMaxSize = 18;
                BroadcastUi.Stretch(num.rectTransform);
                var injury = BroadcastUi.Label(rect, "Injury", "+", 15, new Color(0.95f, 0.12f, 0.16f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(injury.rectTransform, 0.62f, 0.62f, 1f, 1f);
                injury.gameObject.SetActive(false);
                list[i] = new Token { Rect = rect, Number = num, Injury = injury };
            }

            return list;
        }

        RectTransform SpawnBall()
        {
            var go = new GameObject("Ball", typeof(RectTransform), typeof(Image));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(_pitch, false);
            rect.sizeDelta = new Vector2(14, 14);
            var image = go.GetComponent<Image>();
            image.sprite = BroadcastUi.Circle;
            image.color = Color.white;
            image.raycastTarget = false;
            _ballImage = image;
            var shadowGo = new GameObject("BallShadow", typeof(RectTransform), typeof(Image));
            var shadow = shadowGo.GetComponent<RectTransform>();
            shadow.SetParent(_pitch, false);
            shadow.SetSiblingIndex(rect.GetSiblingIndex());
            shadow.sizeDelta = new Vector2(12, 8);
            var shadowImage = shadowGo.GetComponent<Image>();
            shadowImage.sprite = BroadcastUi.Circle;
            shadowImage.color = new Color(0f, 0f, 0f, 0.35f);
            shadowImage.raycastTarget = false;
            _ballShadow = shadowImage;
            return rect;
        }

        void BuildRatings(Transform root)
        {
            _ratingsPage = BroadcastUi.Panel(root, "Ratings", new Color(0.08f, 0.03f, 0.16f, 0.35f)).gameObject;
            BroadcastUi.Place(_ratingsPage.GetComponent<RectTransform>(), 0.05f, 0.14f, 0.95f, 0.76f);
            var page = _ratingsPage.transform;
            var leftTitle = BroadcastUi.Label(page, "HomeTitle", _match.Home.ShortName.ToUpperInvariant(), 18, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(leftTitle.rectTransform, 0.04f, 0.9f, 0.46f, 0.98f);
            var rightTitle = BroadcastUi.Label(page, "AwayTitle", _match.Away.ShortName.ToUpperInvariant(), 18, UiTheme.Accent, TextAnchor.MiddleRight, FontStyle.Bold);
            BroadcastUi.Place(rightTitle.rectTransform, 0.54f, 0.9f, 0.96f, 0.98f);
            _homeRatings = BroadcastUi.Label(page, "HomeRatings", "", 18, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(_homeRatings.rectTransform, 0.04f, 0.04f, 0.48f, 0.88f);
            _awayRatings = BroadcastUi.Label(page, "AwayRatings", "", 18, Color.white, TextAnchor.UpperRight);
            BroadcastUi.Place(_awayRatings.rectTransform, 0.52f, 0.04f, 0.96f, 0.88f);
        }

        void BuildStats(Transform root)
        {
            _statsPage = BroadcastUi.Zone(root, "Stats", 0.03f, 0.13f, 0.97f, 0.77f).gameObject;
            var page = _statsPage.transform;
            _homePoss = StatBlock(page, "HomePoss", 0.02f, 0.62f, 0.24f, 0.96f, "Posse de bola (%)");
            _homeShots = StatBlock(page, "HomeShots", 0.02f, 0.32f, 0.24f, 0.6f, "Remates");
            _homeChances = StatBlock(page, "HomeChances", 0.02f, 0.02f, 0.24f, 0.3f, "Oportunidades");
            _awayPoss = StatBlock(page, "AwayPoss", 0.76f, 0.62f, 0.98f, 0.96f, "Posse de bola (%)");
            _awayShots = StatBlock(page, "AwayShots", 0.76f, 0.32f, 0.98f, 0.6f, "Remates");
            _awayChances = StatBlock(page, "AwayChances", 0.76f, 0.02f, 0.98f, 0.3f, "Oportunidades");
            var card = BroadcastUi.Panel(page, "TimelineCard", new Color(0.12f, 0.05f, 0.22f, 0.55f));
            card.gameObject.SetActive(false);
            var scroll = BroadcastUi.Scroll(card.transform, "Timeline", out _timeline);
            BroadcastUi.Stretch(scroll.GetComponent<RectTransform>());
            scroll.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
            scroll.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);
            _analysis = MatchAnalysisPanel.Attach(root, _match);
            if (_analysis != null) _analysis.SetDismissAction(DismissMatchAnalysis);
        }

        Text StatBlock(Transform parent, string name, float x0, float y0, float x1, float y1, string caption)
        {
            var zone = BroadcastUi.Zone(parent, name, x0, y0, x1, y1);
            var cap = BroadcastUi.Label(zone, "Cap", caption, 16, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(cap.rectTransform, 0f, 0.62f, 1f, 0.95f);
            var value = BroadcastUi.Label(zone, "Value", "0", 64, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(value.rectTransform, 0f, 0.05f, 1f, 0.68f);
            return value;
        }

        void BuildTabs(Transform root)
        {
            var bar = BroadcastUi.Zone(root, "Tabs", 0.12f, 0.058f, 0.88f, 0.115f);
            _tabs = new Button[3];
            _tabs[0] = TabButton(bar, "TabFit", "C. física", 0.04f, 0.32f, BoardTab.Fitness);
            _tabs[1] = TabButton(bar, "TabRate", "Pontuações", 0.36f, 0.64f, BoardTab.Ratings);
            _tabs[2] = TabButton(bar, "TabStats", "Estatísticas", 0.68f, 0.96f, BoardTab.Stats);
        }

        Button TabButton(Transform parent, string name, string label, float x0, float x1, BoardTab tab)
        {
            var button = BroadcastUi.Button(parent, name, label, () => Show(tab), new Color(1f, 1f, 1f, 0f), UiTheme.Muted, 18);
            BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, 0f, x1, 1f);
            return button;
        }

        void BuildFooter(Transform root)
        {
            _footer = BroadcastUi.Zone(root, "Footer", 0f, 0f, 1f, 0.055f).gameObject;
            var pause = BroadcastUi.Button(_footer.transform, "Pause", "Pausa", OpenPause, new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(pause.GetComponent<RectTransform>(), 0.02f, 0.08f, 0.12f, 0.92f);
            var tactics = BroadcastUi.Button(_footer.transform, "QuickTactics", "Táticas rápidas", OpenTactics, new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(tactics.GetComponent<RectTransform>(), 0.14f, 0.08f, 0.32f, 0.92f);
            var sub = BroadcastUi.Button(_footer.transform, "QuickSub", "Substituição", () => OpenSubs(false), new Color(1f, 1f, 1f, 0f), Color.white, 16);
            BroadcastUi.Place(sub.GetComponent<RectTransform>(), 0.34f, 0.08f, 0.5f, 0.92f);
            var speed = BroadcastUi.Button(_footer.transform, "Speed", MatchSpeedText(), CycleSpeed, new Color(1f, 1f, 1f, 0f), UiTheme.Accent, 16);
            BroadcastUi.Place(speed.GetComponent<RectTransform>(), 0.52f, 0.08f, 0.66f, 0.92f);
            _speedLabel = speed.GetComponentInChildren<Text>();
            var hint = BroadcastUi.Label(_footer.transform, "ControlHint", LocalizationManager.Translate("M · DEFINIÇÕES"), 14, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(hint.rectTransform, 0.66f, 0f, 0.98f, 1f);
        }

        static Button EnsurePopupClose(Transform card, string name, Action close)
        {
            if (card == null) return null;
            var existing = card.Find(name);
            var button = existing == null || existing.GetComponent<Button>() == null
                ? BroadcastUi.Button(card, name, "×", close, new Color(1f, 1f, 1f, 0.12f), Color.white, 24)
                : SceneBind.Wire(existing, close);
            if (button == null) return null;
            button.gameObject.SetActive(true);
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = "×";
                label.fontSize = 24;
                label.fontStyle = FontStyle.Bold;
            }
            BroadcastUi.Place(button.GetComponent<RectTransform>(), 0.925f, 0.895f, 0.975f, 0.975f);
            return button;
        }

        void BuildPause(Transform root)
        {
            _pause = Overlay(root, "PausePanel", 0.12f, 0.14f, 0.88f, 0.86f, out var card);
            var dim = _pause.transform.Find("Dim");
            if (dim != null)
            {
                var shade = dim.GetComponent<Image>();
                var dimButton = BroadcastUi.Hit(shade, ResumeMatch);
                dimButton.transition = Selectable.Transition.None;
                shade.color = new Color(0.03f, 0.01f, 0.08f, 0.72f);
            }
            var title = BroadcastUi.Label(card, "Title", "JOGO EM PAUSA", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.78f, 0.92f, 0.95f);
            _pauseBody = BroadcastUi.Label(card, "Tactics", "", 16, Color.white, TextAnchor.UpperLeft);
            BroadcastUi.Place(_pauseBody.rectTransform, 0.1f, 0.48f, 0.9f, 0.74f);
            var resume = BroadcastUi.Button(card, "Resume", "RETOMAR", ResumeMatch, UiTheme.Accent, UiTheme.ButtonText, 18);
            BroadcastUi.Place(resume.GetComponent<RectTransform>(), 0.12f, 0.055f, 0.88f, 0.16f);
            var tactics = BroadcastUi.Button(card, "OpenTactics", "TÁTICAS", OpenTactics, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(tactics.GetComponent<RectTransform>(), 0.12f, 0.31f, 0.48f, 0.43f);
            var sub = BroadcastUi.Button(card, "OpenSubs", "SUBSTITUIÇÕES", () => OpenSubs(true), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 16);
            BroadcastUi.Place(sub.GetComponent<RectTransform>(), 0.52f, 0.31f, 0.88f, 0.43f);
            var simulate = BroadcastUi.Button(card, "SimulateRest", "SIMULAR RESTO DO JOGO", SimulateRest,
                new Color(0.12f, 0.38f, 0.3f, 1f), Color.white, 15);
            BroadcastUi.Place(simulate.GetComponent<RectTransform>(), 0.12f, 0.18f, 0.88f, 0.28f);
            EnsurePopupClose(card, "ClosePause", ResumeMatch);
            _pause.SetActive(false);
        }

        void BuildTactics(Transform root)
        {
            _tactics = Overlay(root, "TacticsPanel", 0.06f, 0.06f, 0.94f, 0.94f, out var card);
            var dim = _tactics.transform.Find("Dim")?.GetComponent<Image>();
            if (dim != null) BroadcastUi.Hit(dim, CloseTactics);
            var title = BroadcastUi.Label(card, "Title", "TÁTICAS RÁPIDAS", 26, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.7f, 0.98f);
            EnsurePopupClose(card, "CloseTactics", CloseTactics);
            var side = BroadcastUi.Label(card, "Side", "PLANOS", 13, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(side.rectTransform, 0.04f, 0.84f, 0.32f, 0.9f);
            _presetButtons = new Button[TacticPresets.Names.Length];
            for (var i = 0; i < TacticPresets.Names.Length; i++)
            {
                var index = i;
                var y1 = 0.8f - i * 0.15f;
                _presetButtons[i] = BroadcastUi.Button(card, "Preset" + i, TacticPresets.Names[i], () =>
                    RequestPreset(index), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 15);
                BroadcastUi.Place(_presetButtons[i].GetComponent<RectTransform>(), 0.04f, y1 - 0.12f, 0.32f, y1);
            }

            _tacticNote = BroadcastUi.Label(card, "Note", "", 16, Color.white, TextAnchor.UpperLeft);
            _tacticNote.verticalOverflow = VerticalWrapMode.Truncate;
            BroadcastUi.Place(_tacticNote.rectTransform, 0.34f, 0.86f, 0.97f, 0.98f);
            var grass = BroadcastUi.Panel(card, "TacticGrass", new Color(0.05f, 0.16f, 0.08f, 1f));
            BroadcastUi.Place(grass.rectTransform, 0.35f, 0.12f, 0.64f, 0.84f);
            BroadcastUi.DrawVerticalHalfPitch(grass.rectTransform, new Color(0.12f, 0.46f, 0.22f));
            _tacticPitch = BroadcastUi.Zone(card, "TacticPitch", 0.35f, 0.12f, 0.64f, 0.84f);
            _tacticOrders = BroadcastUi.Zone(card, "TacticOrders", 0.66f, 0.03f, 0.97f, 0.84f);
            _tactics.SetActive(false);
        }

        void EnsureLiveTactics()
        {
            if (_tactics == null)
                return;
            var card = _tactics.transform.Find("Card");
            if (card == null)
                return;
            _tacticPitch = card.Find("TacticPitch") as RectTransform;
            _tacticOrders = card.Find("TacticOrders") as RectTransform;
            var old = card.Find("RoleList");
            if (old != null)
                old.gameObject.SetActive(false);
            var roles = card.Find("Roles");
            if (roles != null)
                roles.gameObject.SetActive(false);
            var grass = card.Find("TacticGrass") as RectTransform;
            if (grass == null)
            {
                grass = BroadcastUi.Panel(card, "TacticGrass", new Color(0.05f, 0.16f, 0.08f, 1f)).rectTransform;
            }
            BroadcastUi.Place(grass, 0.35f, 0.12f, 0.64f, 0.84f);
            if (grass.Find("HalfwayCircle") == null || grass.Find("HalfwayLine") == null)
            {
                for (var i = grass.childCount - 1; i >= 0; i--)
                    Destroy(grass.GetChild(i).gameObject);
                BroadcastUi.DrawVerticalHalfPitch(grass, new Color(0.12f, 0.46f, 0.22f));
            }

            if (_tacticPitch == null)
                _tacticPitch = BroadcastUi.Zone(card, "TacticPitch", 0.35f, 0.12f, 0.64f, 0.84f);
            if (_tacticOrders == null)
                _tacticOrders = BroadcastUi.Zone(card, "TacticOrders", 0.66f, 0.03f, 0.97f, 0.84f);
            BroadcastUi.Place(_tacticPitch, 0.35f, 0.12f, 0.64f, 0.84f);
            BroadcastUi.Place(_tacticOrders, 0.66f, 0.03f, 0.97f, 0.84f);
            if (_tacticNote != null)
                BroadcastUi.Place(_tacticNote.rectTransform, 0.34f, 0.86f, 0.97f, 0.98f);
        }

        void DrawRoles()
        {
            EnsureLiveTactics();
            var tactics = UserTactics();
            if (_tacticNote != null && _match != null)
                _tacticNote.text = TacticPresets.IsCustomized(tactics)
                    ? (LocalizationManager.Current == GameLanguage.English
                        ? "Custom team instructions. Individual player duties are kept when you choose a preset."
                        : "Instruções de equipa personalizadas. As instruções individuais mantêm-se ao escolher um plano.")
                    : TacticPresets.Describe(TacticPresets.IndexOf(tactics));
            if (_tacticPitch == null || _match == null)
                return;
            for (var i = _tacticPitch.childCount - 1; i >= 0; i--)
                Destroy(_tacticPitch.GetChild(i).gameObject);
            var home = UserHome();
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var club = home ? _match.Home : _match.Away;
            var lines = StartingShape.Lines(tactics == null ? null : tactics.Formation);
            var cursor = 0;
            var rows = lines.Length;
            Player focus = null;
            for (var row = 0; row < rows; row++)
            {
                var count = lines[row];
                var y = rows == 1 ? 0.5f : 0.16f + row * (0.68f / (rows - 1));
                for (var col = 0; col < count && cursor < field.Count; col++)
                {
                    var player = PlayerOf(field[cursor]);
                    cursor++;
                    if (player == null)
                        continue;
                    if (player.Id == _tacticFocus)
                        focus = player;
                    var spot = TacticSpot(_tacticPitch, player, club, player.Id == _tacticFocus);
                    spot.anchorMin = spot.anchorMax = new Vector2((count - col) / (count + 1f), y);
                    spot.sizeDelta = new Vector2(84, 72);
                }
            }

            DrawTacticOrders(focus);
        }

        RectTransform TacticSpot(Transform parent, Player player, Club club, bool selected)
        {
            var card = BroadcastUi.Panel(parent, "T" + player.Id, selected ? new Color(0.16f, 0.42f, 0.78f, 0.96f) : new Color(0.1f, 0.04f, 0.18f, 0.94f), true);
            if (card.GetComponent<RectMask2D>() == null)
                card.gameObject.AddComponent<RectMask2D>();
            var who = player;
            BroadcastUi.Hit(card, () =>
            {
                _tacticFocus = who.Id;
                DrawRoles();
            });
            var shirt = club == null ? UiTheme.Accent : BroadcastUi.Parse(club.JerseyPrimaryHex, UiTheme.Accent);
            var number = BroadcastUi.Label(card.transform, "No", player.SquadNumber.ToString(), 42, new Color(shirt.r, shirt.g, shirt.b, 0.22f), TextAnchor.MiddleCenter, FontStyle.Bold);
            number.raycastTarget = false;
            BroadcastUi.Place(number.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
            var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
            var face = faceGo.GetComponent<Image>();
            face.sprite = FaceBook.For(player);
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.rectTransform.SetParent(card.transform, false);
            BroadcastUi.Place(face.rectTransform, 0.22f, 0.48f, 0.78f, 0.96f);
            var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(), 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.raycastTarget = false;
            BroadcastUi.Place(name.rectTransform, 0.02f, 0.28f, 0.98f, 0.5f);
            var duty = string.IsNullOrEmpty(player.Duty) ? "Equilibrar" : player.Duty;
            var meta = BroadcastUi.Label(card.transform, "Meta", duty, 10, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            meta.raycastTarget = false;
            BroadcastUi.Place(meta.rectTransform, 0.02f, 0.04f, 0.98f, 0.28f);
            return card.rectTransform;
        }

        void DrawTacticOrders(Player focus)
        {
            if (_tacticOrders == null)
                return;
            for (var i = _tacticOrders.childCount - 1; i >= 0; i--)
                Destroy(_tacticOrders.GetChild(i).gameObject);
            if (focus == null)
            {
                var hint = BroadcastUi.Label(_tacticOrders, "Hint", "Toca num jogador do onze. As instruções aparecem aqui.", 15, Color.white, TextAnchor.MiddleLeft);
                BroadcastUi.Place(hint.rectTransform, 0.02f, 0.15f, 0.98f, 0.85f);
                return;
            }

            var current = string.IsNullOrEmpty(focus.Duty) ? "Equilibrar" : focus.Duty;
            var head = BroadcastUi.Label(_tacticOrders, "Head", BroadcastUi.ShortName(focus.FullName).ToUpperInvariant() + "  ·  " + current, 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(head.rectTransform, 0.02f, 0.82f, 0.98f, 0.98f);
            var fit = PlayerTacticalFit.Evaluate(focus, UserTactics(), current);
            var strongest = LocalizationManager.Translate(fit.StrongestAttribute ?? "—");
            var development = LocalizationManager.Translate(fit.DevelopmentAttribute ?? "—");
            var english = LocalizationManager.Current == GameLanguage.English;
            var fitSummary = english
                ? "ROLE FIT " + fit.RoleGrade + " · SYSTEM FIT " + fit.TacticalGrade + "    Strength: " + strongest + " " + fit.StrongestValue + " · Develop: " + development + " " + fit.DevelopmentValue
                : "ADEQUAÇÃO " + fit.RoleGrade + " · SISTEMA " + fit.TacticalGrade + "    Ponto forte: " + strongest + " " + fit.StrongestValue + " · A desenvolver: " + development + " " + fit.DevelopmentValue;
            var fitLine = BroadcastUi.Label(_tacticOrders, "PlayerFit", fitSummary, 10, Color.white, TextAnchor.MiddleLeft);
            fitLine.resizeTextForBestFit = true;
            fitLine.resizeTextMinSize = 8;
            fitLine.resizeTextMaxSize = 10;
            BroadcastUi.Place(fitLine.rectTransform, 0.02f, 0.64f, 0.98f, 0.80f);
            var options = IndividualOrders.ForPosition(focus.Position);
            for (var i = 0; i < options.Length; i++)
            {
                var duty = options[i];
                var on = duty == current;
                var col = i % 2;
                var row = i / 2;
                var x0 = 0.01f + col * 0.5f;
                var y1 = 0.62f - row * 0.2f;
                var who = focus;
                var chosen = duty;
                var button = BroadcastUi.Button(_tacticOrders, "Duty" + i, chosen, () =>
                {
                    who.Duty = chosen;
                    _tacticFocus = who.Id;
                    DrawRoles();
                }, on ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f), on ? UiTheme.ButtonText : Color.white, 13);
                BroadcastUi.Place(button.GetComponent<RectTransform>(), x0, y1 - 0.18f, x0 + 0.48f, y1);
            }
        }

        void OpenTactics()
        {
            if (_match.Phase == MatchPhase.FullTime)
                return;
            _tacticsFromHalfTime = _match.Phase == MatchPhase.HalfTime;
            if (_tacticsFromHalfTime)
                _halfTime.SetActive(false);
            else
            {
                _paused = true;
                _pause.SetActive(false);
            }
            _tactics.SetActive(true);
            _tactics.transform.SetAsLastSibling();
            SetFooter(false);
            PaintChoices();
            DrawRoles();
            RefreshPlan();
        }

        void CloseTactics()
        {
            if (_tactics != null)
                _tactics.SetActive(false);
            if (_tacticsFromHalfTime)
            {
                _tacticsFromHalfTime = false;
                if (_halfTime != null)
                {
                    _halfTime.SetActive(true);
                    _halfTime.transform.SetAsLastSibling();
                }
                return;
            }
            ResumeMatch();
        }

        void OpenPause()
        {
            if (_match.Phase == MatchPhase.HalfTime || _match.Phase == MatchPhase.FullTime)
                return;
            _paused = true;
            if (_tactics != null)
                _tactics.SetActive(false);
            if (_subsMenu != null)
                _subsMenu.SetActive(false);
            _pause.SetActive(true);
            _pause.transform.SetAsLastSibling();
            SetFooter(false);
            RefreshPlan();
        }

        void ResumeMatch()
        {
            _paused = false;
            if (_pause != null)
                _pause.SetActive(false);
            if (_tactics != null)
                _tactics.SetActive(false);
            if (_subsMenu != null)
                _subsMenu.SetActive(false);
            SetFooter(true);
        }

        void SetFooter(bool on)
        {
            if (_footer == null)
                return;
            foreach (var button in _footer.GetComponentsInChildren<Button>(true))
                button.interactable = on;
        }

        void TogglePause()
        {
            if (_paused)
                ResumeMatch();
            else
                OpenPause();
        }

        Button[] ChoiceRow(Transform card, string caption, float y, string[] options, Action<string> pick, bool fullLabel = false)
        {
            var buttons = new Button[options.Length];
            var label = BroadcastUi.Label(card, caption, caption.ToUpperInvariant(), 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(label.rectTransform, 0.08f, y + 0.12f, 0.4f, 0.2f);
            var span = options.Length <= 1 ? 0.84f : 0.84f / options.Length;
            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                var x0 = 0.08f + i * span;
                var text = fullLabel ? TacticLabel(option) : ShortChoice(option);
                buttons[i] = BroadcastUi.Button(card, caption + i, text, () => pick(option), new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, options.Length > 3 ? 11 : fullLabel ? 12 : 15);
                BroadcastUi.Place(buttons[i].GetComponent<RectTransform>(), x0, y, x0 + span - 0.012f, y + 0.1f);
            }

            return buttons;
        }

        static string ShortIdea(string option)
        {
            if (option.Contains("Tiki")) return "Tiki";
            if (option.Contains("Posse")) return "Posse";
            if (option.Contains("Cruz")) return "Cruzar";
            if (option.Contains("direto")) return "Direto";
            if (option.Contains("longas")) return "Longas";
            if (option.Contains("Remate")) return "Longe";
            if (option.Contains("Contra")) return "Contra";
            return "Equil.";
        }

        static string TacticLabel(string option)
        {
            if (option.Contains("muito alta"))
                return "Muito\nalta";
            if (option.Contains("muito baixa"))
                return "Muito\nbaixa";
            if (option.Contains("Pressão"))
                return option.Replace("Pressão ", "Pressão\n");
            if (option.Contains("alta"))
                return "Alta";
            if (option.Contains("baixa"))
                return "Baixa";
            if (option.Contains("média"))
                return "Média";
            return option;
        }

        static string ShortChoice(string option)
        {
            if (option.Contains("alta")) return "ALTA";
            if (option.Contains("baixa")) return "BAIXA";
            return "MÉDIA";
        }

        void BuildShape(Transform root)
        {
            _shape = Overlay(root, "Shape", 0.08f, 0.06f, 0.92f, 0.94f, out var card);
            var dim = _shape.transform.Find("Dim")?.GetComponent<Image>();
            if (dim != null) BroadcastUi.Hit(dim, () =>
            {
                _shape.SetActive(false);
                if (_tactics != null) _tactics.SetActive(true);
            });
            var title = BroadcastUi.Label(card, "Title", "FORMAÇÃO", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.5f, 0.98f);
            var close = BroadcastUi.Button(card, "CloseShape", "×", () =>
            {
                _shape.SetActive(false);
                if (_tactics != null)
                    _tactics.SetActive(true);
            }, new Color(1f, 1f, 1f, 0.12f), Color.white, 16);
            BroadcastUi.Place(close.GetComponent<RectTransform>(), 0.925f, 0.9f, 0.975f, 0.98f);
            var grass = BroadcastUi.Zone(card, "Grass", 0.38f, 0.2f, 0.62f, 0.88f);
            BroadcastUi.DrawVerticalHalfPitch(grass, new Color(0.13f, 0.48f, 0.22f));
            _shapeHost = BroadcastUi.Zone(card, "Players", 0.38f, 0.2f, 0.62f, 0.88f);
            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            _formationButtons = new Button[forms.Length];
            for (var i = 0; i < forms.Length; i++)
            {
                var formation = forms[i];
                var x0 = 0.04f + i * 0.19f;
                _formationButtons[i] = BroadcastUi.Button(card, "Form" + i, formation, () => PickFormation(formation), new Color(1f, 1f, 1f, 0.1f), Color.white, 16);
                BroadcastUi.Place(_formationButtons[i].GetComponent<RectTransform>(), x0, 0.05f, x0 + 0.17f, 0.16f);
            }

            _shape.SetActive(false);
        }

        void OpenShape()
        {
            _pause.SetActive(false);
            _shape.SetActive(true);
            DrawShape();
        }

        void PickFormation(string formation)
        {
            var ours = UserTactics();
            ours.Formation = formation;
            if (UserHome())
                Reseat(_match.HomeOnField, _match.HomeBench, formation);
            else
                Reseat(_match.AwayOnField, _match.AwayBench, formation);
            TouchSummary();
            DrawShape();
            _slow = 0f;
        }

        void Reseat(List<string> onField, List<string> bench, string formation)
        {
            var players = new List<Player>();
            foreach (var id in onField)
            {
                var player = PlayerOf(id);
                if (player != null)
                    players.Add(player);
            }

            foreach (var id in bench)
            {
                var player = PlayerOf(id);
                if (player != null)
                    players.Add(player);
            }

            var eleven = new List<Player>();
            var rest = new List<Player>();
            StartingShape.Split(players, formation, eleven, rest);
            onField.Clear();
            bench.Clear();
            foreach (var player in eleven)
                onField.Add(player.Id);
            foreach (var player in rest)
                bench.Add(player.Id);
        }

        void DrawShape()
        {
            var grass = _shape == null ? null : _shape.transform.Find("Card/Grass") as RectTransform;
            if (grass != null)
            {
                BroadcastUi.Place(grass, 0.38f, 0.2f, 0.62f, 0.88f);
                for (var i = grass.childCount - 1; i >= 0; i--)
                    Destroy(grass.GetChild(i).gameObject);
                BroadcastUi.DrawVerticalHalfPitch(grass, new Color(0.13f, 0.48f, 0.22f));
            }
            if (_shapeHost == null || _match == null)
                return;
            BroadcastUi.Place(_shapeHost, 0.38f, 0.2f, 0.62f, 0.88f);
            for (var i = _shapeHost.childCount - 1; i >= 0; i--)
                Destroy(_shapeHost.GetChild(i).gameObject);
            var field = UserHome() ? _match.HomeOnField : _match.AwayOnField;
            var shape = UserTactics().Formation;
            for (var i = 0; i < field.Count && i < 11; i++)
            {
                var slot = MatchEngine.Slot(true, i, shape);
                var player = PlayerOf(field[i]);
                var anchor = slot;
                var faceGo = new GameObject("Face" + i, typeof(RectTransform), typeof(Image));
                var faceRect = faceGo.GetComponent<RectTransform>();
                faceRect.SetParent(_shapeHost, false);
                faceRect.anchorMin = faceRect.anchorMax = anchor;
                faceRect.sizeDelta = new Vector2(36, 36);
                var face = faceGo.GetComponent<Image>();
                face.sprite = player == null ? null : FaceBook.For(player);
                face.preserveAspect = true;
                face.raycastTarget = false;
                var label = player == null ? (i + 1).ToString() : player.SquadNumber.ToString();
                var text = BroadcastUi.Label(_shapeHost, "S" + i, label, 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor + new Vector2(0f, -0.07f);
                text.rectTransform.sizeDelta = new Vector2(36, 18);
            }

            var forms = new[] { "4-3-3", "4-2-3-1", "4-4-2", "3-4-3", "5-4-1" };
            for (var i = 0; i < _formationButtons.Length; i++)
            {
                var on = UserTactics().Formation == forms[i];
                _formationButtons[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(1f, 1f, 1f, 0.1f);
                _formationButtons[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }
        }

        void TouchSummary()
        {
            var t = UserTactics();
            var matchingPreset = TacticPresets.MatchingPresetIndex(t);
            t.Preset = matchingPreset >= 0 ? TacticPresets.Names[matchingPreset] : TacticPresets.CustomName;
            t.Summary = t.Formation + " · " + t.Pressing + " · " + t.BuildUp;
            PaintChoices();
            RefreshPlan();
        }

        void RequestPreset(int index)
        {
            var tactics = UserTactics();
            if (tactics == null || index < 0 || index >= TacticPresets.Names.Length)
                return;
            if (!TacticPresets.IsCustomized(tactics) || TacticPresets.MatchesPreset(tactics, index))
            {
                ApplyPreset(index);
                return;
            }

            if (_presetConfirm != null)
                Destroy(_presetConfirm);
            _pendingPresetIndex = index;
            _presetConfirm = new GameObject("ConfirmCustomTactic", typeof(RectTransform));
            _presetConfirm.transform.SetParent(_tactics.transform, false);
            var root = _presetConfirm.GetComponent<RectTransform>();
            BroadcastUi.Stretch(root);
            var dim = BroadcastUi.Solid(root, "Dim", new Color(0.01f, 0.01f, 0.05f, 0.82f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            BroadcastUi.Hit(dim, () => ClosePresetConfirm(false));
            var card = BroadcastUi.Panel(root, "Card", new Color(0.12f, 0.05f, 0.24f, 1f), true);
            BroadcastUi.Place(card.rectTransform, 0.27f, 0.31f, 0.73f, 0.69f);
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = BroadcastUi.Label(card.transform, "Title",
                english ? "REPLACE CUSTOM TACTICS?" : "SUBSTITUIR TÁTICAS PERSONALIZADAS?",
                19, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.06f, 0.68f, 0.94f, 0.94f);
            var presetName = LocalizationManager.Translate(TacticPresets.Names[index]);
            var message = BroadcastUi.Label(card.transform, "Message",
                english
                    ? "Applying “" + presetName + "” will replace your custom team instructions. Individual player duties will stay unchanged."
                    : "Aplicar “" + presetName + "” substitui as instruções personalizadas da equipa. As instruções individuais dos jogadores mantêm-se.",
                15, Color.white, TextAnchor.MiddleCenter);
            message.resizeTextForBestFit = true;
            message.resizeTextMinSize = 12;
            message.resizeTextMaxSize = 15;
            BroadcastUi.Place(message.rectTransform, 0.08f, 0.32f, 0.92f, 0.68f);
            var apply = BroadcastUi.Button(card.transform, "ApplyPreset",
                english ? "APPLY PRESET" : "APLICAR PLANO", () => ClosePresetConfirm(true),
                UiTheme.Accent, UiTheme.ButtonText, 14);
            BroadcastUi.Place(apply.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.48f, 0.28f);
            var keep = BroadcastUi.Button(card.transform, "KeepCustom",
                english ? "KEEP CUSTOM" : "MANTER PERSONALIZADA", () => ClosePresetConfirm(false),
                new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 13);
            BroadcastUi.Place(keep.GetComponent<RectTransform>(), 0.52f, 0.08f, 0.94f, 0.28f);
            EnsurePopupClose(card.transform, "ClosePreset", () => ClosePresetConfirm(false));
            _presetConfirm.transform.SetAsLastSibling();
        }

        void ClosePresetConfirm(bool apply)
        {
            var index = _pendingPresetIndex;
            _pendingPresetIndex = -1;
            if (_presetConfirm != null)
                Destroy(_presetConfirm);
            _presetConfirm = null;
            if (apply && index >= 0)
                ApplyPreset(index);
        }

        void ApplyPreset(int index)
        {
            TacticPresets.Apply(UserTactics(), index);
            TouchSummary();
            DrawRoles();
        }

        void PaintChoices()
        {
            var ours = UserTactics();
            if (_presetButtons == null)
                return;
            var active = TacticPresets.Active(ours);
            for (var i = 0; i < _presetButtons.Length; i++)
            {
                var on = TacticPresets.Names[i] == active;
                _presetButtons[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                _presetButtons[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }
        }

        static void PaintOption(Button[] buttons, string[] options, string current)
        {
            if (buttons == null)
                return;
            for (var i = 0; i < buttons.Length; i++)
            {
                var on = options != null && i < options.Length && options[i] == current;
                buttons[i].GetComponent<Image>().color = on ? UiTheme.Accent : new Color(0.22f, 0.12f, 0.38f, 1f);
                buttons[i].GetComponentInChildren<Text>().color = on ? UiTheme.ButtonText : Color.white;
            }
        }

        void BuildHalf(Transform root)
        {
            _halfTime = Overlay(root, "HalfTime", 0.03f, 0.03f, 0.97f, 0.97f, out var card);
            var english = LocalizationManager.Current == GameLanguage.English;
            var background = BroadcastUi.Solid(card, "Locker", new Color(0.1f, 0.05f, 0.16f, 1f), false);
            BroadcastUi.Stretch(background.rectTransform);
            background.preserveAspect = false;
            background.type = Image.Type.Simple;
            var room = Resources.LoadAll<Sprite>("Balneario");
            if (room != null && room.Length > 0 && room[0] != null)
            {
                background.sprite = room[0];
                background.color = Color.white;
            }

            var shade = BroadcastUi.Solid(card, "Shade", new Color(0.03f, 0.01f, 0.08f, 0.38f), false);
            BroadcastUi.Stretch(shade.rectTransform);
            var title = BroadcastUi.Label(card, "Title", english ? "DRESSING ROOM" : "BALNEÁRIO", 28, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.05f, 0.88f, 0.7f, 0.97f);
            _locker = BroadcastUi.Zone(card, "Voices", 0.03f, 0.12f, 0.97f, 0.88f);
            var leave = BroadcastUi.Button(card, "LeaveAssistant", english ? "LET ASSISTANT HANDLE IT" : "DEIXAR AO ADJUNTO", LeaveToAssistant, new Color(1f, 1f, 1f, 0.14f), Color.white, 14);
            BroadcastUi.Place(leave.GetComponent<RectTransform>(), 0.03f, 0.015f, 0.32f, 0.07f);
            var analysis = BroadcastUi.Button(card, "HalfTimeAnalysis", LocalizationManager.Current == GameLanguage.English ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO", OpenHalfTimeAnalysis, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
            BroadcastUi.Place(analysis.GetComponent<RectTransform>(), 0.355f, 0.015f, 0.645f, 0.07f);
            var changes = BroadcastUi.Button(card, "HalfSubs", english ? "SUBSTITUTIONS" : "SUBSTITUIÇÕES", () => OpenSubs(true), UiTheme.Accent, UiTheme.ButtonText, 16);
            BroadcastUi.Place(changes.GetComponent<RectTransform>(), 0.68f, 0.015f, 0.97f, 0.07f);
            EnsureHalfTalkUi(card);
            _halfTime.SetActive(false);
        }

        void EnsureHalfTalkUi(Transform card)
        {
            if (card == null)
                return;
            var english = LocalizationManager.Current == GameLanguage.English;
            var title = card.Find("Title")?.GetComponent<Text>();
            if (title != null)
                title.text = english ? "DRESSING ROOM" : "BALNEÁRIO";
            _locker = card.Find("Voices") as RectTransform;
            if (_locker != null)
                BroadcastUi.Place(_locker, 0.03f, 0.43f, 0.97f, 0.76f);
            var summary = card.Find("FirstHalfSummary");
            if (summary == null)
            {
                _halfSummary = BroadcastUi.Label(card, "FirstHalfSummary", "", 14, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
                summary = _halfSummary.transform;
            }
            else
                _halfSummary = summary.GetComponent<Text>();
            if (_halfSummary != null)
                BroadcastUi.Place(_halfSummary.rectTransform, 0.05f, 0.79f, 0.95f, 0.87f);

            var managerSpeech = card.Find("ManagerSpeech");
            if (managerSpeech == null)
                _managerSpeech = BroadcastUi.Label(card, "ManagerSpeech", english ? "Choose how to address the team." : "Escolhe como falar com a equipa.", 16, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            else
                _managerSpeech = managerSpeech.GetComponent<Text>();
            if (_managerSpeech != null)
            {
                if (_match == null || !_match.TeamTalkApplied)
                    _managerSpeech.text = english ? "Choose how to address the team." : "Escolhe como falar com a equipa.";
                BroadcastUi.Place(_managerSpeech.rectTransform, 0.06f, 0.31f, 0.94f, 0.41f);
                _managerSpeech.resizeTextForBestFit = true;
                _managerSpeech.resizeTextMinSize = 12;
                _managerSpeech.resizeTextMaxSize = 16;
            }

            var labels = english
                ? new[] { "MOTIVATE", "DEMAND", "CALM", "REPRIMAND", "TACTICAL" }
                : new[] { "MOTIVAR", "EXIGIR", "ACALMAR", "REPREENDER", "TÁTICA" };
            _teamTalkButtons = new Button[labels.Length];
            for (var i = 0; i < labels.Length; i++)
            {
                var index = i;
                var node = card.Find("Talk" + i);
                if (node == null)
                {
                    var button = BroadcastUi.Button(card, "Talk" + i, labels[i], () => ChooseTeamTalk(index),
                        i == 4 ? UiTheme.Accent : new Color(0.12f, 0.08f, 0.2f, 0.92f), Color.white, 14);
                    node = button.transform;
                }
                _teamTalkButtons[i] = SceneBind.Wire(node, () => ChooseTeamTalk(index));
                if (_teamTalkButtons[i] != null)
                {
                    var buttonLabel = _teamTalkButtons[i].GetComponentInChildren<Text>();
                    if (buttonLabel != null)
                        buttonLabel.text = labels[i];
                    BroadcastUi.Place(_teamTalkButtons[i].GetComponent<RectTransform>(), 0.025f + i * 0.19f, 0.19f,
                        0.205f + i * 0.19f, 0.29f);
                    var text = _teamTalkButtons[i].GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.resizeTextForBestFit = true;
                        text.resizeTextMinSize = 10;
                        text.resizeTextMaxSize = 14;
                    }
                }
            }

            var leave = card.Find("LeaveAssistant") as RectTransform;
            if (leave != null)
            {
                BroadcastUi.Place(leave, 0.02f, 0.045f, 0.20f, 0.13f);
                var leaveLabel = leave.GetComponentInChildren<Text>();
                if (leaveLabel != null) leaveLabel.text = english ? "LET ASSISTANT HANDLE IT" : "DEIXAR AO ADJUNTO";
            }
            var tactics = card.Find("HalfTactics");
            if (tactics == null)
            {
                _halfTactics = BroadcastUi.Button(card, "HalfTactics", "TÁTICAS", OpenTactics,
                    new Color(1f, 1f, 1f, 0.14f), Color.white, 13);
                tactics = _halfTactics.transform;
            }
            else
                _halfTactics = SceneBind.Wire(tactics, OpenTactics);
            if (_halfTactics != null)
            {
                BroadcastUi.Place(_halfTactics.GetComponent<RectTransform>(), 0.21f, 0.045f, 0.39f, 0.13f);
                var tacticsLabel = _halfTactics.GetComponentInChildren<Text>();
                if (tacticsLabel != null) tacticsLabel.text = english ? "TACTICS" : "TÁTICAS";
            }
            var subs = card.Find("HalfSubs") as RectTransform;
            if (subs != null)
            {
                BroadcastUi.Place(subs, 0.60f, 0.045f, 0.78f, 0.13f);
                var subsLabel = subs.GetComponentInChildren<Text>();
                if (subsLabel != null) subsLabel.text = english ? "SUBSTITUTIONS" : "SUBSTITUIÇÕES";
            }
            var proceed = card.Find("ContinueSecondHalf");
            if (proceed == null)
            {
                _continueSecondHalf = BroadcastUi.Button(card, "ContinueSecondHalf", "COMEÇAR 2.ª PARTE", ContinueSecondHalf,
                    UiTheme.Accent, UiTheme.ButtonText, 14);
                proceed = _continueSecondHalf.transform;
            }
            else
                _continueSecondHalf = SceneBind.Wire(proceed, ContinueSecondHalf);
            if (_continueSecondHalf != null)
            {
                BroadcastUi.Place(_continueSecondHalf.GetComponent<RectTransform>(), 0.80f, 0.045f, 0.98f, 0.13f);
                var continueLabel = _continueSecondHalf.GetComponentInChildren<Text>();
                if (continueLabel != null) continueLabel.text = english ? "START SECOND HALF" : "COMEÇAR 2.ª PARTE";
                _continueSecondHalf.gameObject.SetActive(false);
            }
        }

        void EnsureHalfTimeAnalysisAction(Transform card)
        {
            if (card == null)
                return;
            var analysis = card.Find("HalfTimeAnalysis");
            if (analysis == null)
            {
                var created = BroadcastUi.Button(card, "HalfTimeAnalysis", LocalizationManager.Current == GameLanguage.English ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO", () => { }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 12);
                analysis = created.transform;
            }
            SceneBind.Wire(analysis, OpenHalfTimeAnalysis);
            BroadcastUi.Place(analysis as RectTransform, 0.40f, 0.045f, 0.59f, 0.13f);
            var analysisLabel = analysis.GetComponentInChildren<Text>();
            if (analysisLabel != null)
                analysisLabel.text = LocalizationManager.Current == GameLanguage.English ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO";
            var leave = card.Find("LeaveAssistant");
            if (leave != null)
                BroadcastUi.Place(leave as RectTransform, 0.02f, 0.045f, 0.20f, 0.13f);
            var subs = card.Find("HalfSubs");
            if (subs != null)
                BroadcastUi.Place(subs as RectTransform, 0.60f, 0.045f, 0.78f, 0.13f);
        }

        void EnsureFullTimeAnalysisAction(Transform card)
        {
            if (card == null)
                return;
            var analysis = card.Find("Analysis");
            if (analysis == null)
            {
                var created = BroadcastUi.Button(card, "Analysis", LocalizationManager.Current == GameLanguage.English ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO", () => { }, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
                analysis = created.transform;
            }
            SceneBind.Wire(analysis, OpenFullTimeAnalysis);
            BroadcastUi.Place(analysis as RectTransform, 0.28f, 0.22f, 0.72f, 0.34f);
            var next = card.Find("Continue");
            if (next != null)
                BroadcastUi.Place(next as RectTransform, 0.28f, 0.08f, 0.72f, 0.2f);
        }

        void FillLocker()
        {
            if (_locker == null || _match == null)
                return;
            for (var i = _locker.childCount - 1; i >= 0; i--)
                Destroy(_locker.GetChild(i).gameObject);
            var field = UserHome() ? _match.HomeOnField : _match.AwayOnField;
            var ours = UserHome() ? _match.HomeGoals : _match.AwayGoals;
            var theirs = UserHome() ? _match.AwayGoals : _match.HomeGoals;
            var voices = LockerVoices(field);
            if (_halfSummary != null)
                _halfSummary.text = FirstHalfSummary();
            for (var slot = 0; slot < voices.Length; slot++)
            {
                var player = voices[slot];
                var kind = slot == 0 ? 0 : player.IsCaptain && player != voices[0] ? 1 : 2;
                var speech = LockerComment(player, slot, ours, theirs);
                var x0 = 0.02f + slot * 0.33f;
                var hit = BroadcastUi.Panel(_locker, "Voice" + slot, new Color(0.05f, 0.02f, 0.1f, 0.28f), true);
                BroadcastUi.Place(hit.rectTransform, x0, 0.02f, x0 + 0.31f, 0.98f);
                hit.raycastTarget = false;
                var face = FaceImage(player);
                face.rectTransform.SetParent(hit.transform, false);
                face.raycastTarget = false;
                BroadcastUi.Place(face.rectTransform, 0.18f, 0.48f, 0.82f, 0.96f);
                var english = LocalizationManager.Current == GameLanguage.English;
                var tag = kind == 0 ? (english ? "STAR" : "ÁS") : kind == 1 ? (english ? "CAPTAIN" : "CAPITÃO") : (english ? "SQUAD" : "PLANTEL");
                var who = BroadcastUi.Label(hit.transform, "Who", tag + "  ·  " + BroadcastUi.ShortName(player.FullName), 14, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
                who.raycastTarget = false;
                BroadcastUi.Place(who.rectTransform, 0.04f, 0.34f, 0.96f, 0.46f);
                var line = BroadcastUi.Label(hit.transform, "Say", speech, 15, Color.white, TextAnchor.UpperCenter);
                line.raycastTarget = false;
                line.verticalOverflow = VerticalWrapMode.Truncate;
                BroadcastUi.Place(line.rectTransform, 0.04f, 0.04f, 0.96f, 0.34f);
            }
        }

        string FirstHalfSummary()
        {
            var home = UserHome();
            var ours = home ? _match.HomeGoals : _match.AwayGoals;
            var theirs = home ? _match.AwayGoals : _match.HomeGoals;
            var ourShots = home ? _match.HomeShots : _match.AwayShots;
            var theirShots = home ? _match.AwayShots : _match.HomeShots;
            var ourOnTarget = home ? _match.HomeChances : _match.AwayChances;
            var theirOnTarget = home ? _match.AwayChances : _match.HomeChances;
            var totalPossession = _match.HomePossession + _match.AwayPossession;
            var ourPossession = home ? _match.HomePossession : _match.AwayPossession;
            var possession = totalPossession > 0.01f ? Mathf.RoundToInt(ourPossession / totalPossession * 100f) : 50;
            if (LocalizationManager.Current == GameLanguage.English)
                return "HALF-TIME  " + ours + "–" + theirs + "     POSSESSION  " + possession + "%     SHOTS  " + ourShots + "–" + theirShots
                    + "     ON TARGET  " + ourOnTarget + "–" + theirOnTarget;
            return "INTERVALO  " + ours + "–" + theirs + "     POSSE  " + possession + "%     REMATES  " + ourShots + "–" + theirShots
                + "     À BALIZA  " + ourOnTarget + "–" + theirOnTarget;
        }

        string LockerComment(Player player, int slot, int ours, int theirs)
        {
            if (player == null)
                return LocalizationManager.Current == GameLanguage.English
                    ? "We need to find more space after the break."
                    : "Temos de encontrar mais espaço depois do intervalo.";
            var goals = _match.GoalsScored.TryGetValue(player.Id, out var scored) ? scored : 0;
            if (LocalizationManager.Current == GameLanguage.English)
            {
                var name = BroadcastUi.ShortName(player.FullName);
                if (goals > 0) return name + ": Keep putting the ball into the box; I’m getting there.";
                if (player.Fatigue >= 78) return name + ": My legs are heavy. I’ll manage the runs better.";
                if (_match.RatingOf(player.Id) < 6.1f) return name + ": I’ve given the ball away too often. I’ll keep it simple.";
                if (player.IsCaptain) return name + ": We’ve got 45 minutes. Stay compact and play together.";
                if (ours < theirs && player.Personality == PersonalityTrait.Leader) return name + ": We can still turn this around. No panic.";
                if (player.Morale < 40) return name + ": I could use a word from you before we go back out.";
                return slot % 2 == 0
                    ? name + ": If the space opens up behind them, play the ball in."
                    : name + ": Let’s pick the right moment to speed things up.";
            }
            if (goals > 0)
                return BroadcastUi.ShortName(player.FullName) + ": Continuem a meter a bola na área; estou a chegar lá.";
            if (player.Fatigue >= 78)
                return BroadcastUi.ShortName(player.FullName) + ": As pernas já pesam. Vamos gerir melhor as corridas.";
            if (_match.RatingOf(player.Id) < 6.1f)
                return BroadcastUi.ShortName(player.FullName) + ": Perdi demasiadas bolas. Na segunda parte vou simplificar.";
            if (player.IsCaptain)
                return BroadcastUi.ShortName(player.FullName) + ": Ainda há 45 minutos. Vamos fechar as linhas e jogar juntos.";
            if (ours < theirs && player.Personality == PersonalityTrait.Leader)
                return BroadcastUi.ShortName(player.FullName) + ": O resultado ainda dá para virar. Sem entrar em pânico.";
            if (player.Morale < 40)
                return BroadcastUi.ShortName(player.FullName) + ": Preciso de uma palavra tua antes de voltarmos.";
            return slot % 2 == 0
                ? BroadcastUi.ShortName(player.FullName) + ": Se aparecer espaço nas costas, metemos a bola lá."
                : BroadcastUi.ShortName(player.FullName) + ": Vamos escolher melhor o momento para acelerar.";
        }

        Player[] LockerVoices(List<string> field)
        {
            var players = new List<Player>();
            for (var i = 0; i < field.Count; i++)
            {
                var player = PlayerOf(field[i]);
                if (player != null && player.Position != PlayerPosition.Goalkeeper)
                    players.Add(player);
            }

            if (players.Count == 0)
                return new Player[0];
            players.Sort((a, b) => AceRank(b).CompareTo(AceRank(a)));
            var ace = players[0];
            Player captain = null;
            foreach (var player in players)
            {
                if (player.IsCaptain)
                    captain = player;
            }

            if (captain == null || captain == ace)
            {
                captain = players.Count > 1 ? players[1] : ace;
                foreach (var player in players)
                {
                    if (player != ace && player.IsViceCaptain)
                        captain = player;
                }
            }

            Player loose = null;
            var start = Mathf.Abs((ace.FullName ?? "").Length + (int)_match.Minute) % players.Count;
            for (var n = 0; n < players.Count; n++)
            {
                var candidate = players[(start + n) % players.Count];
                if (candidate != ace && candidate != captain)
                {
                    loose = candidate;
                    break;
                }
            }

            var voices = new List<Player> { ace };
            if (captain != null && captain != ace)
                voices.Add(captain);
            if (loose != null && loose != ace && loose != captain)
                voices.Add(loose);
            return voices.ToArray();
        }

        static int AceRank(Player player)
        {
            if (player == null || player.Attributes == null)
                return 0;
            return player.Attributes.Finishing + player.Attributes.Passing + player.Attributes.Pace + player.Attributes.Composure + PlayerAttributes.Overall(player);
        }

        HalfIdea IdeaFor(Player player, int slot, int ours, int theirs)
        {
            var who = BroadcastUi.ShortName(player.FullName);
            var idea = new HalfIdea { PlayerId = player.Id, Preset = -1 };
            var english = LocalizationManager.Current == GameLanguage.English;
            if (slot == 0)
            {
                idea.Speech = english
                    ? (ours < theirs ? who + ": Find me between the lines. When it comes, I’ll shoot." : who + ": I’ll stay in the box. Find me facing goal and I’ll finish.")
                    : ours < theirs
                        ? who + ": Deixem-me entre linhas. Quando a bola chegar, eu remato."
                        : who + ": Eu fico na área. Se me acharem de frente, eu finalizo.";
                idea.Done = english
                    ? who + " wants the ball in a shooting position. We’ll look for the finish in the second half."
                    : who + " pede a bola para finalizar. A segunda parte vai procurar o remate.";
                idea.Duty = player.Position == PlayerPosition.LeftWinger || player.Position == PlayerPosition.RightWinger
                    ? "Cortar para dentro ofensivo"
                    : "Ficar na área";
                return idea;
            }

            if (slot == 1)
            {
                if (ours <= theirs)
                {
                    idea.Speech = english ? who + ": Let’s push the line up. I won’t give their midfielder time to think."
                        : who + ": Subimos o bloco. Não deixo o médio deles pensar.";
                    idea.Done = english ? who + " wants the team to push up and press higher."
                        : who + " puxa a linha para cima e aumenta a pressão.";
                    idea.Preset = 2;
                }
                else
                {
                    idea.Speech = english ? who + ": Close the middle and break only when there’s space."
                        : who + ": Fechamos o meio e só saímos quando houver espaço.";
                    idea.Done = english ? who + " wants a deeper block and counter-attacks."
                        : who + " pede o bloco mais baixo e saída em contra-ataque.";
                    idea.Preset = 3;
                }

                return idea;
            }

            if (player.Position == PlayerPosition.LeftWinger || player.Position == PlayerPosition.RightWinger)
            {
                idea.Speech = english ? who + ": I’ll cut inside and only make the run when I’m free."
                    : who + ": Eu corto para dentro equilibrado e só apareço se estiver desmarcado.";
                idea.Duty = "Cortar para dentro equilibrado";
            }
            else if (player.Position == PlayerPosition.LeftBack || player.Position == PlayerPosition.RightBack)
            {
                idea.Speech = english ? who + ": I’ll stay back and only move forward when the flank is open."
                    : who + ": Eu fico defensivo e só subo quando a ala estiver livre.";
                idea.Duty = "Defensivo";
            }
            else
            {
                idea.Speech = english ? who + ": I’ll make shorter runs so I can receive facing forward."
                    : who + ": Eu faço só a desmarcação curta, para receber de frente.";
                idea.Duty = "Só desmarcação";
            }

            idea.Done = english ? who + " changes role: " + LocalizationManager.Translate(idea.Duty) + "."
                : who + " muda a função: " + idea.Duty + ".";
            return idea;
        }

        void ChooseTeamTalk(int index)
        {
            var approaches = new[] { "motivational", "demanding", "calm", "angry", "tactical" };
            if (index < 0 || index >= approaches.Length)
                return;
            var detail = ManagerSpeech(approaches[index]);
            if (approaches[index] == "tactical")
                detail = ApplyHalfTimeTacticalAdjustment() + "\n" + detail;
            Talk(approaches[index], detail);
        }

        string ManagerSpeech(string approach, string extra = null)
        {
            var home = UserHome();
            var ours = home ? _match.HomeGoals : _match.AwayGoals;
            var theirs = home ? _match.AwayGoals : _match.HomeGoals;
            var ourShots = home ? _match.HomeShots : _match.AwayShots;
            var theirShots = home ? _match.AwayShots : _match.HomeShots;
            var ourChances = home ? _match.HomeChances : _match.AwayChances;
            var theirChances = home ? _match.AwayChances : _match.HomeChances;
            var totalPossession = _match.HomePossession + _match.AwayPossession;
            var ourPossession = home ? _match.HomePossession : _match.AwayPossession;
            var possession = totalPossession > 0.01f ? Mathf.RoundToInt(ourPossession / totalPossession * 100f) : 50;
            var tired = 0;
            var poor = 0;
            var lowMorale = 0;
            var leaders = 0;
            var sensitive = 0;
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            for (var i = 0; i < field.Count; i++)
            {
                var player = PlayerOf(field[i]);
                if (player == null) continue;
                if (player.Fatigue >= 78) tired++;
                if (_match.RatingOf(player.Id) < 6f) poor++;
                if (player.Morale < 42) lowMorale++;
                if (player.Personality == PersonalityTrait.Leader) leaders++;
                if (player.Personality == PersonalityTrait.Insecure || player.Personality == PersonalityTrait.Temperamental) sensitive++;
            }

            if (LocalizationManager.Current == GameLanguage.English)
                return ManagerSpeechEnglish(approach, extra, ours, theirs, ourShots, theirShots, ourChances, theirChances,
                    possession, tired, poor, lowMorale, leaders, sensitive);

            string situation = ours < theirs
                ? "Estamos a perder " + ours + "–" + theirs + "."
                : ours > theirs ? "Estamos na frente " + ours + "–" + theirs + "."
                : "Está empatado " + ours + "–" + theirs + ".";
            var evidence = ourShots > theirShots
                ? "Já criámos " + ourShots + " remates e temos de escolher melhor a finalização."
                : ourShots < theirShots ? "Eles remataram mais (" + theirShots + "–" + ourShots + "); precisamos de defender melhor a entrada da área."
                : "Os remates estão equilibrados (" + ourShots + "–" + theirShots + ").";
            if (ourChances < theirChances)
                evidence += " Eles também acertaram mais vezes na baliza.";
            else if (ourChances > theirChances)
                evidence += " Estamos a acertar na baliza, continuem a procurar esse espaço.";
            evidence += " Temos " + possession + "% de posse.";

            string speech;
            switch (approach)
            {
                case "motivational":
                    speech = situation + " " + (ours < theirs && ourShots > 0
                        ? "As oportunidades apareceram; mantenham a coragem e tratem melhor o último toque."
                        : "Confiem no que estamos a fazer e ajudem o colega ao lado.");
                    break;
                case "demanding":
                    speech = situation + " " + (poor > 2
                        ? "Quero mais concentração e responsabilidade nas próximas decisões."
                        : "A intensidade está lá; agora executem cada lance com rigor.");
                    break;
                case "angry":
                    speech = situation + " " + (theirChances > ourChances
                        ? "Estamos a deixar demasiados remates limpos. Fechem a zona central e falem uns com os outros."
                        : "Não ofereçam mais bolas fáceis. Quero reação imediata depois de cada perda.");
                    break;
                case "tactical":
                    speech = situation + " " + evidence;
                    break;
                default:
                    speech = situation + " " + (ours < theirs
                        ? "Respirem. Não precisamos de resolver tudo num só lance; vamos construir com cabeça."
                        : "Mantenham a calma e a distância entre setores. O jogo ainda pede boas decisões.");
                    break;
            }
            if (tired >= 3)
                speech += " Vejo várias pernas pesadas; gerimos melhor as corridas e as substituições.";
            if (lowMorale >= 2)
                speech += " Alguns estão com a confiança em baixo; falem uns com os outros e apoiem o colega.";
            if (leaders > 0 && ours < theirs)
                speech += " Os líderes do grupo têm de puxar pela equipa agora.";
            if (sensitive > 0 && approach == "angry")
                speech += " Sei que alguns sentem mais a crítica; quero uma resposta com trabalho, não com receio.";
            if (!string.IsNullOrEmpty(extra))
                speech += " " + extra;
            return speech;
        }

        string ManagerSpeechEnglish(string approach, string extra, int ours, int theirs, int ourShots, int theirShots,
            int ourChances, int theirChances, int possession, int tired, int poor, int lowMorale, int leaders, int sensitive)
        {
            var situation = ours < theirs ? "We’re behind " + ours + "–" + theirs + "."
                : ours > theirs ? "We’re ahead " + ours + "–" + theirs + "."
                : "It’s level at " + ours + "–" + theirs + ".";
            var evidence = ourShots > theirShots
                ? "We’ve created " + ourShots + " shots; now we need to choose our finishes better."
                : ourShots < theirShots ? "They’ve had more shots (" + theirShots + "–" + ourShots + "); defend the edge of the box better."
                : "The shot count is even (" + ourShots + "–" + theirShots + ").";
            if (ourChances < theirChances) evidence += " They’ve hit the target more often.";
            else if (ourChances > theirChances) evidence += " We’re hitting the target; keep finding that space.";
            evidence += " We have " + possession + "% possession.";

            string speech;
            switch (approach)
            {
                case "motivational":
                    speech = situation + " " + (ours < theirs && ourShots > 0
                        ? "We’ve made chances; stay brave and take more care with the final touch."
                        : "Trust what we’re doing and support the teammate beside you.");
                    break;
                case "demanding":
                    speech = situation + " " + (poor > 2
                        ? "I want more focus and responsibility in your next decisions."
                        : "The intensity is there. Now execute every moment with discipline.");
                    break;
                case "angry":
                    speech = situation + " " + (theirChances > ourChances
                        ? "We’re allowing too many clear shots. Close the middle and communicate."
                        : "Stop giving the ball away cheaply. React immediately when we lose it.");
                    break;
                case "tactical":
                    speech = situation + " " + evidence;
                    break;
                default:
                    speech = situation + " " + (ours < theirs
                        ? "Take a breath. We don’t need to solve everything in one move; build with patience."
                        : "Stay calm and keep the distance between the lines. Good decisions still matter.");
                    break;
            }
            if (tired >= 3) speech += " Several players look tired; manage the runs and substitutions.";
            if (lowMorale >= 2) speech += " A few of you look low on confidence; talk to each other and back your teammates.";
            if (leaders > 0 && ours < theirs) speech += " I need the leaders in this group to lift the team now.";
            if (sensitive > 0 && approach == "angry") speech += " I know some of you take criticism harder; respond with effort, not fear.";
            if (!string.IsNullOrEmpty(extra)) speech += " " + extra;
            return speech;
        }

        string ApplyHalfTimeTacticalAdjustment()
        {
            var home = UserHome();
            var tactics = UserTactics();
            var ours = home ? _match.HomeGoals : _match.AwayGoals;
            var theirs = home ? _match.AwayGoals : _match.HomeGoals;
            var ourShots = home ? _match.HomeShots : _match.AwayShots;
            var theirShots = home ? _match.AwayShots : _match.HomeShots;
            var ourPossession = home ? _match.HomePossession : _match.AwayPossession;
            var theirPossession = home ? _match.AwayPossession : _match.HomePossession;
            if (ours < theirs && ourShots < theirShots)
            {
                TacticPresets.Apply(tactics, 2);
                return LocalizationManager.Current == GameLanguage.English
                    ? "You’ve set the team to press higher and win the ball further from your goal."
                    : "Ajustas para pressionar mais alto e recuperar a bola longe da tua baliza.";
            }
            if (ours > theirs && theirPossession > ourPossession)
            {
                TacticPresets.Apply(tactics, 3);
                return LocalizationManager.Current == GameLanguage.English
                    ? "You’ve dropped the defensive block and switched to counter-attacks."
                    : "Ajustas para baixar o bloco e sair em contra-ataque.";
            }
            if (ourPossession > theirPossession && ourShots <= theirShots)
            {
                TacticPresets.Apply(tactics, 1);
                return LocalizationManager.Current == GameLanguage.English
                    ? "You’ve increased the width to create crossing options and reach the box."
                    : "Ajustas a largura para dar mais opções de cruzamento e chegar à área.";
            }
            TacticPresets.Apply(tactics, 4);
            return LocalizationManager.Current == GameLanguage.English
                ? "You’ve asked the team to keep the ball and wait for a clearer opening."
                : "Ajustas para manter a bola e esperar por uma oportunidade mais clara.";
        }

        static Image FaceImage(Player player)
        {
            var go = new GameObject("Face", typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            image.sprite = FaceBook.For(player);
            image.preserveAspect = true;
            image.raycastTarget = false;
            return image;
        }

        void LeaveToAssistant()
        {
            var home = UserHome();
            var ours = home ? _match.HomeGoals : _match.AwayGoals;
            var theirs = home ? _match.AwayGoals : _match.HomeGoals;
            var ourShots = home ? _match.HomeShots : _match.AwayShots;
            var theirShots = home ? _match.AwayShots : _match.HomeShots;
            if (ours < theirs && ourShots < theirShots)
            {
                TacticPresets.Apply(UserTactics(), 2);
                Talk("demanding", (LocalizationManager.Current == GameLanguage.English
                    ? "The assistant wants more pressure after a quiet first half. "
                    : "O adjunto pede mais pressão depois de uma primeira parte em que criámos pouco. ") + ManagerSpeech("demanding"));
            }
            else if (ours > theirs && theirShots > ourShots)
            {
                TacticPresets.Apply(UserTactics(), 3);
                Talk("tactical", (LocalizationManager.Current == GameLanguage.English
                    ? "The assistant recommends protecting the lead and counter-attacking. "
                    : "O adjunto recomenda proteger a vantagem e sair em contra-ataque. ") + ManagerSpeech("tactical"));
            }
            else
                Talk("motivational", (LocalizationManager.Current == GameLanguage.English
                    ? "The assistant asks the squad to stay confident and united. "
                    : "O adjunto pede confiança e união para a segunda parte. ") + ManagerSpeech("motivational"));
        }

        void BuildFull(Transform root)
        {
            _fullTime = Modal(root, "FullTime", out var card);
            var title = BroadcastUi.Label(card, "Title", "FIM DE JOGO", 28, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.08f, 0.72f, 0.92f, 0.9f);
            _fullResult = BroadcastUi.Label(card, "Result", "", 32, UiTheme.Score, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(_fullResult.rectTransform, 0.08f, 0.4f, 0.92f, 0.7f);
            var analysis = BroadcastUi.Button(card, "Analysis", LocalizationManager.Current == GameLanguage.English ? "MATCH ANALYSIS" : "ANÁLISE DO JOGO", OpenFullTimeAnalysis, new Color(0.22f, 0.12f, 0.38f, 1f), Color.white, 14);
            BroadcastUi.Place(analysis.GetComponent<RectTransform>(), 0.28f, 0.22f, 0.72f, 0.34f);
            var next = BroadcastUi.Button(card, "Continue", "CONTINUAR", FinishMatch, UiTheme.Accent, UiTheme.ButtonText, 20);
            BroadcastUi.Place(next.GetComponent<RectTransform>(), 0.28f, 0.08f, 0.72f, 0.2f);
            EnsurePopupClose(card, "CloseFullTime", FinishMatch);
            _fullTime.SetActive(false);
        }

        void OpenFullTimeAnalysis()
        {
            _analysisAtFullTime = true;
            _fullTime.SetActive(false);
            Show(BoardTab.Stats);
            if (_analysis != null)
            {
                _analysis.SetFinalMode(true);
                _analysis.SetBackToResult(ReturnToFullTimeResult, true);
                _analysis.Refresh();
            }
            RefreshStats();
        }

        void ReturnToFullTimeResult()
        {
            _analysisAtFullTime = false;
            if (_analysis != null)
            {
                _analysis.SetFinalMode(false);
                _analysis.SetBackToResult(null, false);
            }
            Show(BoardTab.Fitness);
            _fullTime.SetActive(true);
        }

        void OpenHalfTimeAnalysis()
        {
            _analysisAtHalfTime = true;
            _halfTime.SetActive(false);
            Show(BoardTab.Stats);
            if (_analysis != null)
            {
                _analysis.SetFinalMode(false);
                _analysis.SetReturnAction(ReturnToHalfTime, true, "BACK TO TEAM TALK", "VOLTAR AO BALNEÁRIO");
                _analysis.Refresh();
            }
            RefreshStats();
        }

        void ReturnToHalfTime()
        {
            _analysisAtHalfTime = false;
            if (_analysis != null)
            {
                _analysis.SetReturnAction(null, false, "", "");
                _analysis.SetVisible(false);
            }
            Show(BoardTab.Fitness);
            _halfTime.SetActive(true);
        }

        void DismissMatchAnalysis()
        {
            if (_analysisAtFullTime) ReturnToFullTimeResult();
            else if (_analysisAtHalfTime) ReturnToHalfTime();
            else Show(BoardTab.Fitness);
        }

        GameObject Overlay(Transform root, string name, float x0, float y0, float x1, float y1, out Transform card)
        {
            var modal = BroadcastUi.Zone(root, name, 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var panel = BroadcastUi.Panel(modal, "Card", new Color(0.16f, 0.06f, 0.32f, 0.96f), true);
            BroadcastUi.Place(panel.rectTransform, x0, y0, x1, y1);
            card = panel.transform;
            return modal.gameObject;
        }

        GameObject Modal(Transform root, string name, out Transform card)
        {
            var modal = BroadcastUi.Zone(root, name, 0f, 0f, 1f, 1f);
            var dim = BroadcastUi.Solid(modal, "Dim", new Color(0.03f, 0.01f, 0.08f, 0.72f), true);
            BroadcastUi.Stretch(dim.rectTransform);
            var panel = BroadcastUi.Panel(modal, "Card", new Color(0.16f, 0.06f, 0.32f, 0.96f), true);
            BroadcastUi.Place(panel.rectTransform, 0.27f, 0.24f, 0.73f, 0.76f);
            card = panel.transform;
            return modal.gameObject;
        }

        void Show(BoardTab tab)
        {
            if (tab == BoardTab.Ratings && !SettingsPrefs.LiveRatings)
                tab = BoardTab.Fitness;
            _tab = tab;
            _fitnessPage.SetActive(!_analysisAtFullTime);
            _ratingsPage.SetActive(tab == BoardTab.Ratings && !_analysisAtFullTime);
            _statsPage.SetActive(tab == BoardTab.Stats && !_analysisAtFullTime);
            if (_analysis != null)
            {
                _analysis.SetFinalMode(_analysisAtFullTime);
                _analysis.SetVisible(_analysisAtFullTime || tab == BoardTab.Stats);
            }
            if (_homeCol != null && _homeCol.Root != null)
                _homeCol.Root.SetActive(tab != BoardTab.Stats);
            if (_awayCol != null && _awayCol.Root != null)
                _awayCol.Root.SetActive(tab != BoardTab.Stats);
            for (var i = 0; i < _tabs.Length; i++)
            {
                if (_tabs[i] == null)
                    continue;
                _tabs[i].gameObject.SetActive(!_analysisAtFullTime);
                var label = _tabs[i].GetComponentInChildren<Text>();
                var on = (int)tab == i;
                if (label != null)
                {
                    label.color = on ? Color.white : UiTheme.Muted;
                    label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
                }
            }
            if (_score != null) _score.gameObject.SetActive(!_analysisAtFullTime);
            if (_clock != null) _clock.gameObject.SetActive(!_analysisAtFullTime);
            if (_ticker != null) _ticker.gameObject.SetActive(!_analysisAtFullTime);

            _slow = 0f;
        }

        void CycleSpeed()
        {
            SettingsPrefs.MatchSpeedIndex = (SettingsPrefs.MatchSpeedIndex + 1) % 4;
            _speed = SettingsPrefs.MatchSpeed;
            SettingsPrefs.Commit();
            if (_speedLabel != null)
                _speedLabel.text = MatchSpeedText();
        }

        static string MatchSpeedText()
        {
            var prefix = LocalizationManager.Current == GameLanguage.English ? "Speed " : "Ritmo ";
            return prefix + SettingsPrefs.MatchSpeedLabel;
        }

        public static void RefreshSettings()
        {
            var controller = FindFirstObjectByType<MatchDayController>();
            if (controller == null)
                return;
            if (controller._tabs != null && controller._tabs.Length > 1 && controller._tabs[1] != null)
                controller._tabs[1].gameObject.SetActive(SettingsPrefs.LiveRatings);
            if (!SettingsPrefs.LiveRatings && controller._tab == BoardTab.Ratings)
                controller.Show(BoardTab.Fitness);
            controller._speed = SettingsPrefs.MatchSpeed;
            if (controller._speedLabel != null)
                controller._speedLabel.text = MatchSpeedText();
        }

        void LateUpdate()
        {
            if (_match == null || !SquadDragCard.ConsumeSwap(out var incoming, out var fieldIndex, out var fromField))
                return;
            ApplyDragSub(incoming, fieldIndex, fromField);
        }

        void OpenSubs(bool fromPause)
        {
            var bench = UserHome() ? _match.HomeBench : _match.AwayBench;
            if (bench.Count == 0)
                return;
            _subFromPause = fromPause;
            _paused = true;
            if (_pause != null)
                _pause.SetActive(false);
            if (_tactics != null)
                _tactics.SetActive(false);
            SetFooter(false);
            DrawSubPitch();
            _subsMenu.SetActive(true);
            _subsMenu.transform.SetAsLastSibling();
        }

        void BuildSubs(Transform root)
        {
            _subsMenu = Overlay(root, "SubsMenu", 0.04f, 0.05f, 0.96f, 0.95f, out var card);
            var dim = _subsMenu.transform.Find("Dim")?.GetComponent<Image>();
            if (dim != null) BroadcastUi.Hit(dim, CloseSubs);
            var title = BroadcastUi.Label(card, "Title", "SUBSTITUIÇÕES", 24, Color.white, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(title.rectTransform, 0.04f, 0.9f, 0.46f, 0.98f);
            _subStatus = BroadcastUi.Label(card, "Status", "", 15, UiTheme.Muted, TextAnchor.MiddleRight);
            BroadcastUi.Place(_subStatus.rectTransform, 0.4f, 0.9f, 0.86f, 0.98f);
            var pitch = BroadcastUi.Panel(card, "PitchBg", new Color(0.05f, 0.16f, 0.08f, 1f));
            BroadcastUi.Place(pitch.rectTransform, 0.365f, 0.29f, 0.635f, 0.86f);
            BroadcastUi.DrawVerticalHalfPitch(pitch.rectTransform, new Color(0.12f, 0.46f, 0.22f));
            _subPitch = BroadcastUi.Zone(card, "Pitch", 0.365f, 0.29f, 0.635f, 0.86f);
            var benchTitle = BroadcastUi.Label(card, "BenchTitle", "SUPLENTES", 14, UiTheme.Accent, TextAnchor.MiddleLeft, FontStyle.Bold);
            BroadcastUi.Place(benchTitle.rectTransform, 0.04f, 0.25f, 0.3f, 0.31f);
            _subBench = BroadcastUi.Zone(card, "Bench", 0.04f, 0.05f, 0.96f, 0.25f);
            EnsurePopupClose(card, "CloseSubs", CloseSubs);
            _subsMenu.SetActive(false);
        }

        void DrawSubPitch()
        {
            if (_subPitch == null)
                return;
            for (var i = _subPitch.childCount - 1; i >= 0; i--)
                Destroy(_subPitch.GetChild(i).gameObject);
            for (var i = _subBench.childCount - 1; i >= 0; i--)
                Destroy(_subBench.GetChild(i).gameObject);
            var home = UserHome();
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var bench = home ? _match.HomeBench : _match.AwayBench;
            var left = home ? _match.HomeSubsLeft : _match.AwaySubsLeft;
            var club = home ? _match.Home : _match.Away;
            if (_subStatus != null)
                _subStatus.text = left > 0 ? "Arrasta o suplente para o lugar. Restam " + left + "." : "Sem substituições.";
            var formation = UserTactics().Formation;
            for (var index = 0; index < field.Count && index < 11; index++)
            {
                var player = PlayerOf(field[index]);
                if (player != null)
                {
                    var card = SubCard(_subPitch, player, club, false, index);
                    card.anchorMin = card.anchorMax = MatchEngine.Slot(true, index, formation);
                    card.sizeDelta = new Vector2(76, 64);
                }
            }

            var shown = Mathf.Min(bench.Count, 9);
            for (var i = 0; i < shown; i++)
            {
                var player = PlayerOf(bench[i]);
                if (player == null)
                    continue;
                var card = SubCard(_subBench, player, club, true, i);
                card.anchorMin = card.anchorMax = new Vector2((i + 0.5f) / shown, 0.5f);
                card.sizeDelta = new Vector2(118, 104);
            }
        }

        RectTransform SubCard(Transform parent, Player player, Club club, bool bench, int index)
        {
            var card = BroadcastUi.Panel(parent, "S" + player.Id, new Color(0.1f, 0.04f, 0.18f, 0.94f), true);
            if (card.GetComponent<RectMask2D>() == null)
                card.gameObject.AddComponent<RectMask2D>();
            var drag = card.gameObject.AddComponent<SquadDragCard>();
            drag.Player = player;
            drag.Bench = bench;
            drag.FieldIndex = index;
            var injured = player.Injury != null && player.Injury.IsInjured;
            var sentOff = _match.SentOffPlayers.Contains(player.Id);
            if ((bench && injured) || sentOff)
                drag.enabled = false;
            var shirt = BroadcastUi.Parse(club.JerseyPrimaryHex, UiTheme.Accent);
            var number = BroadcastUi.Label(card.transform, "No", player.SquadNumber.ToString(), 64, new Color(shirt.r, shirt.g, shirt.b, 0.22f), TextAnchor.MiddleCenter, FontStyle.Bold);
            number.resizeTextForBestFit = true;
            number.resizeTextMinSize = 10;
            number.resizeTextMaxSize = 120;
            BroadcastUi.Place(number.rectTransform, 0.04f, 0.04f, 0.96f, 0.96f);
            var faceGo = new GameObject("Face", typeof(RectTransform), typeof(Image));
            var face = faceGo.GetComponent<Image>();
            face.sprite = FaceBook.For(player);
            face.preserveAspect = true;
            face.raycastTarget = false;
            face.rectTransform.SetParent(card.transform, false);
            BroadcastUi.Place(face.rectTransform, 0.18f, 0.46f, 0.82f, 0.96f);
            var name = BroadcastUi.Label(card.transform, "Name", BroadcastUi.ShortName(player.FullName).ToUpperInvariant(), 12, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(name.rectTransform, 0.04f, 0.28f, 0.96f, 0.48f);
            var meta = BroadcastUi.Label(card.transform, "Meta", PresentationLabels.Position(player.Position), 11, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(meta.rectTransform, 0.04f, 0.14f, 0.96f, 0.3f);
            var form = BroadcastUi.Label(card.transform, "Form", "OVR " + PlayerAttributes.Overall(player) + "   Cans. " + player.Fatigue, 11, UiTheme.Accent, TextAnchor.MiddleCenter, FontStyle.Bold);
            BroadcastUi.Place(form.rectTransform, 0.02f, 0.01f, 0.98f, 0.16f);
            if (injured)
            {
                var cross = BroadcastUi.Label(card.transform, "Injury", "+ LESIONADO", 14, new Color(1f, 0.18f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(cross.rectTransform, 0.06f, 0.77f, 0.94f, 0.98f);
            }
            if (sentOff)
            {
                var red = BroadcastUi.Label(card.transform, "SentOff", "EXPULSO", 14, new Color(1f, 0.18f, 0.2f), TextAnchor.MiddleCenter, FontStyle.Bold);
                BroadcastUi.Place(red.rectTransform, 0.06f, 0.77f, 0.94f, 0.98f);
            }
            return card.rectTransform;
        }

        void ApplyDragSub(Player incoming, int fieldIndex, int fromField)
        {
            if (incoming == null)
                return;
            if (incoming.Injury != null && incoming.Injury.IsInjured)
            {
                _match.LastEvent = incoming.FullName + " está lesionado e não pode entrar.";
                return;
            }
            var home = UserHome();
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var bench = home ? _match.HomeBench : _match.AwayBench;
            if (fieldIndex < 0 || fieldIndex >= field.Count)
                return;
            if (_match.SentOffPlayers.Contains(field[fieldIndex]))
            {
                _match.LastEvent = "O jogador expulso não pode ser substituído.";
                return;
            }
            if (fromField >= 0)
            {
                if (fromField >= field.Count || fromField == fieldIndex)
                    return;
                var swap = field[fromField];
                field[fromField] = field[fieldIndex];
                field[fieldIndex] = swap;
                _match.LastEvent = "Troca de posições em campo.";
                DrawSubPitch();
                RefreshSides();
                return;
            }

            var benchIndex = bench.IndexOf(incoming.Id);
            if (benchIndex < 0)
                return;
            var left = home ? _match.HomeSubsLeft : _match.AwaySubsLeft;
            if (left <= 0)
            {
                _match.LastEvent = "Já não há substituições.";
                return;
            }

            _queueOff = field[fieldIndex];
            _queueOn = incoming.Id;
            var stopped = _match.Phase == MatchPhase.HalfTime || _engine.DeadBall || (_subsMenu != null && _subsMenu.activeSelf);
            if (stopped)
            {
                TryQueuedSub();
                DrawSubPitch();
                RefreshSides();
                return;
            }

            _match.LastEvent = "Substituição pronta. Entra na próxima paragem.";
            DrawSubPitch();
        }

        void CloseSubs()
        {
            _subsMenu.SetActive(false);
            if (_match != null && _match.Phase == MatchPhase.HalfTime)
            {
                _halfTime.SetActive(true);
                _halfTime.transform.SetAsLastSibling();
                return;
            }
            if (_subFromPause)
            {
                _pause.SetActive(true);
                _pause.transform.SetAsLastSibling();
            }
            else
                ResumeMatch();
        }

        void TryQueuedSub()
        {
            if (string.IsNullOrEmpty(_queueOff) || string.IsNullOrEmpty(_queueOn))
                return;
            var stopped = _engine.DeadBall || _match.Phase == MatchPhase.HalfTime || (_subsMenu != null && _subsMenu.activeSelf);
            if (!stopped)
                return;
            var home = UserHome();
            var field = home ? _match.HomeOnField : _match.AwayOnField;
            var bench = home ? _match.HomeBench : _match.AwayBench;
            var off = field.IndexOf(_queueOff);
            var on = bench.IndexOf(_queueOn);
            _queueOff = null;
            _queueOn = null;
            if (off < 0 || on < 0)
                return;
            _engine.TrySub(home, off, on);
            if (_subsMenu != null && _subsMenu.activeSelf)
                DrawSubPitch();
            _slow = 0f;
            RefreshPlan();
        }

        void Talk(string approach, string detail = null)
        {
            var trust = _career == null ? null : _career.ManagerTrust;
            if (!_engine.ApplyTeamTalk(approach, UserHome(), trust, detail))
                return;
            if (_managerSpeech != null)
                _managerSpeech.text = string.IsNullOrEmpty(detail) ? ManagerSpeech(approach) : detail;
            if (_teamTalkButtons != null)
                for (var i = 0; i < _teamTalkButtons.Length; i++)
                    if (_teamTalkButtons[i] != null)
                        _teamTalkButtons[i].interactable = false;
            if (_continueSecondHalf != null)
                _continueSecondHalf.gameObject.SetActive(true);
        }

        void ContinueSecondHalf()
        {
            if (_match == null || !_match.TeamTalkApplied || _match.Phase != MatchPhase.HalfTime)
                return;
            _halfTime.SetActive(false);
            _engine.StartSecondHalf();
            _paused = false;
            _footer.SetActive(true);
        }

        void FinishMatch()
        {
            if (_matchFinalized || _match == null || GameRoot.Instance == null || GameRoot.Instance.Session.Career == null)
                return;
            _matchFinalized = true;
            var career = GameRoot.Instance.Session.Career;
            RecoverBench(_match.HomeBench);
            RecoverBench(_match.AwayBench);
            var played = SeasonSchedule.Find(career, _match.Fixture);
            played.HomeGoals = _match.HomeGoals;
            played.AwayGoals = _match.AwayGoals;
            SeasonSchedule.ApplyResult(career, played);
            SeasonSchedule.SimulateOthers(career, played.Date, played);
            var userHome = _match.Home.Id == career.Coach.ClubId;
            string motm = null;
            string motmPlayerId = null;
            var best = -1f;
            foreach (var pair in _match.Ratings)
            {
                if (pair.Value <= best || !_match.People.ContainsKey(pair.Key))
                    continue;
                best = pair.Value;
                motm = _match.People[pair.Key].FullName;
                motmPlayerId = pair.Key;
            }
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "red" || note.Kind == "goal" || note.Kind == "injury")
                    Newsroom.Publish(career, "A Gazeta do Relvado", note.Minute + "'  " + note.Text, "Aconteceu aos " + note.Minute + " minutos.\n\n" + note.Text);
            }
            CareerMoments.AfterMatch(career, _match.HomeGoals, _match.AwayGoals, userHome, _match.Home.ShortName, _match.Away.ShortName, motm, best < 0 ? null : best.ToString("0.0"));
            CareerMemories.RecordMatch(career, _match, userHome, motmPlayerId);
            CareerRelationships.ApplySelectionConsequences(career, _match, userHome);
            SeasonSchedule.RebuildTable(career);
            SeasonBook.CreditLive(career, _match);
            SeasonBook.ReconcileCurrentSeason(career);
            CareerStore.Save(career);
            GameRoot.Instance.LiveMatch = null;
            GameRoot.Instance.Open(SceneNames.CareerHub);
        }

        void RefreshHud()
        {
            _score.text = _match.HomeGoals + "  -  " + _match.AwayGoals;
            if (_plan != null)
                _plan.text = TacticPresets.Active(UserTactics());
            _clock.text = Clock(_match.Minute);
            _ticker.text = _match.LastEvent;
            if (_heard != _match.LastEvent)
            {
                _heard = _match.LastEvent;
                if (_heard != null && (_heard.Contains("Lançamento") || _heard.Contains("Lancamento")))
                    GameAudio.ThrowIn();
            }
            if (_match.FlashSerial != _flashSeen)
            {
                _flashSeen = _match.FlashSerial;
                var person = PlayerOf(_match.FlashPlayerId);
                var text = _match.Flash ?? "";
                var eventText = text.ToUpperInvariant();
                var isGoal = eventText.Contains("GOLO") || eventText.Contains("GOAL");
                var isRed = eventText.Contains("VERMELHO") || eventText.Contains("RED CARD");
                var isInjury = eventText.Contains("LES") || eventText.Contains("INJUR");
                var isPenalty = eventText.Contains("PENÁLTI") || eventText.Contains("PENALTI") || eventText.Contains("PENALTY");
                var isKeyEvent = isGoal || isRed || isInjury || isPenalty;
                var isExtended = isKeyEvent || eventText.Contains("AMARELO") || eventText.Contains("YELLOW CARD")
                    || eventText.Contains("FALTA") || eventText.Contains("FOUL") || eventText.Contains("CANTO") || eventText.Contains("CORNER");
                var highlightMode = SettingsPrefs.MatchHighlights;
                var important = highlightMode == 0 || (highlightMode == 1 && isExtended) || (highlightMode == 2 && isKeyEvent) || (highlightMode == 3 && isGoal);
                _watchEvent = important;
                _flashLeft = isGoal ? 2.65f : _watchEvent ? 3.4f : 2.4f;
                GameAudio.ForFlash(_match.Flash);
                _flash.transform.SetParent(_eventCard.transform, false);
                _eventCard.SetActive(important);
                _pauseAfterEvent = (isGoal && SettingsPrefs.PauseOnGoal)
                    || (isRed && SettingsPrefs.PauseOnRedCard)
                    || (isInjury && SettingsPrefs.PauseOnInjury);
                if (important)
                {
                    _eventCard.transform.SetAsLastSibling();
                    PositionEventPopup(person != null, text);
                    _eventFace.gameObject.SetActive(person != null);
                    if (person != null)
                    {
                        _eventFace.sprite = FaceBook.For(person);
                        _eventWho.text = person.SquadNumber + "   " + person.FullName;
                    }
                    else
                        _eventWho.text = "";
                    var card = text.Contains("VERMELHO") || text.Contains("AMARELO");
                    _eventStrip.gameObject.SetActive(card);
                    _eventStrip.color = text.Contains("VERMELHO") ? new Color(0.82f, 0.1f, 0.12f) : new Color(0.95f, 0.82f, 0.12f);
                    _flash.text = text;
                    _flash.gameObject.SetActive(true);
                    PlaceEventText(person != null);
                }
            }

            if (_flashLeft > 0f && !AudioSettingsOverlay.IsVisible)
            {
                _flashLeft -= Time.deltaTime;
                if (_flashLeft <= 0f)
                {
                    _watchEvent = false;
                    _flash.gameObject.SetActive(false);
                    if (_eventCard != null)
                        _eventCard.SetActive(false);
                    if (_eventStrip != null)
                        _eventStrip.gameObject.SetActive(false);
                    if (_pauseAfterEvent && _match.Phase != MatchPhase.HalfTime && _match.Phase != MatchPhase.FullTime)
                    {
                        _pauseAfterEvent = false;
                        OpenPause();
                    }
                }
            }
        }

        void PositionEventPopup(bool hasPlayer, string text)
        {
            if (_eventCard == null)
                return;
            var compact = !string.IsNullOrEmpty(text) && (text.Length <= 38 || (text.Contains("GOLO") && text.Length < 72));
            var left = compact ? 0.31f : 0.22f;
            var bottom = compact ? 0.36f : 0.32f;
            var right = compact ? 0.69f : 0.78f;
            var top = compact ? 0.64f : 0.68f;
            BroadcastUi.Place(_eventCard.GetComponent<RectTransform>(), left, bottom, right, top);
            if (_eventFace != null)
            {
                _eventFace.rectTransform.anchorMin = _eventFace.rectTransform.anchorMax = new Vector2(0.13f, 0.5f);
                _eventFace.rectTransform.anchoredPosition = Vector2.zero;
                var faceSize = compact ? 92f : 122f;
                _eventFace.rectTransform.sizeDelta = new Vector2(hasPlayer ? faceSize : 0f, hasPlayer ? faceSize : 0f);
            }
            if (_eventWho != null)
                BroadcastUi.Place(_eventWho.rectTransform, hasPlayer ? 0.25f : 0.05f, 0.55f, 0.96f, 0.9f);
            if (_eventStrip != null)
                BroadcastUi.Place(_eventStrip.rectTransform, 0.06f, 0.18f, 0.08f, 0.82f);
            PlaceEventText(hasPlayer);
        }

        void PlaceEventText(bool hasPlayer)
        {
            if (_flash == null)
                return;
            BroadcastUi.Place(_flash.rectTransform, hasPlayer ? 0.25f : 0.06f, 0.13f, 0.95f, hasPlayer ? 0.53f : 0.88f);
        }

        void PlaceTokens()
        {
            for (var i = 0; i < 11; i++)
            {
                var homeOn = i < _match.HomeOnField.Count;
                var awayOn = i < _match.AwayOnField.Count;
                _homeTokens[i].Rect.gameObject.SetActive(homeOn);
                _awayTokens[i].Rect.gameObject.SetActive(awayOn);
                if (homeOn)
                {
                    var slot = _engine.MotionOf(true, i);
                    Move(_homeTokens[i], ToPitch(slot), _match.HomeHasBall && i == _match.BallHolder, Jumping(true, i));
                    _homeTokens[i].Number.text = Shirt(_match.HomeOnField, i);
                    _homeTokens[i].Rect.GetComponent<Image>().color = _homeColor;
                    _homeTokens[i].Injury.gameObject.SetActive(Injured(_match.HomeOnField[i]));
                    Dive(_homeTokens[i], _engine.KeeperDive(true), i == 0);
                }

                if (awayOn)
                {
                    var slot = _engine.MotionOf(false, i);
                    Move(_awayTokens[i], ToPitch(slot), !_match.HomeHasBall && i == _match.BallHolder, Jumping(false, i));
                    _awayTokens[i].Number.text = Shirt(_match.AwayOnField, i);
                    _awayTokens[i].Rect.GetComponent<Image>().color = _awayColor;
                    _awayTokens[i].Injury.gameObject.SetActive(Injured(_match.AwayOnField[i]));
                    Dive(_awayTokens[i], _engine.KeeperDive(false), i == 0);
                }
            }

            SeparateTokenPositions();

            var ball = ToPitch(_engine.ShownBall());
            _ball.anchorMin = ball;
            _ball.anchorMax = ball;
            var lift = Mathf.Clamp01(_engine.BallHeight);
            var visualSize = Mathf.Lerp(14f, 28f, lift);
            _ball.sizeDelta = new Vector2(visualSize, visualSize);
            _ball.localScale = Vector3.one;
            _ball.anchoredPosition = Vector2.zero;
            if (_ballShadow != null)
            {
                _ballShadow.rectTransform.anchorMin = ball;
                _ballShadow.rectTransform.anchorMax = ball;
                _ballShadow.rectTransform.anchoredPosition = Vector2.zero;
                var shade = Mathf.Lerp(1f, 0.45f, lift);
                _ballShadow.rectTransform.localScale = new Vector3(shade, shade * 0.72f, 1f);
                _ballShadow.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.38f, 0.16f, lift));
            }
            if (_ballImage != null)
                _ballImage.color = Color.white;
            _snap = false;
        }

        static Vector2 ToPitch(Vector2 slot)
        {
            return new Vector2(slot.y, slot.x);
        }

        static Color NumberInk(Color jerseyColor)
        {
            return jerseyColor.grayscale >= 0.5f ? new Color(0.08f, 0.08f, 0.1f, 1f) : Color.white;
        }

        Token TokenAt(int index)
        {
            return index < 11 ? _homeTokens[index] : _awayTokens[index - 11];
        }

        void SeparateTokenPositions()
        {
            if (_pitch == null || _homeTokens == null || _awayTokens == null)
                return;
            var pitchSize = _pitch.rect.size;
            if (pitchSize.x < 1f || pitchSize.y < 1f)
                return;

            for (var i = 0; i < 22; i++)
            {
                var token = TokenAt(i);
                _tokenOffsets[i] = Vector2.zero;
                var active = token != null && token.Rect != null && token.Rect.gameObject.activeInHierarchy;
                if (!active)
                {
                    _tokenPoints[i] = Vector2.zero;
                    _tokenRenderOffsets[i] = Vector2.zero;
                    continue;
                }
                _tokenPoints[i] = Vector2.Scale(token.Rect.anchorMin, pitchSize) + token.Rect.anchoredPosition;
            }

            for (var pass = 0; pass < 5; pass++)
            {
                for (var i = 0; i < 22; i++)
                {
                    var first = TokenAt(i);
                    if (first == null || first.Rect == null || !first.Rect.gameObject.activeInHierarchy)
                        continue;
                    for (var j = i + 1; j < 22; j++)
                    {
                        var second = TokenAt(j);
                        if (second == null || second.Rect == null || !second.Rect.gameObject.activeInHierarchy)
                            continue;

                        var delta = _tokenPoints[j] + _tokenOffsets[j] - _tokenPoints[i] - _tokenOffsets[i];
                        var distance = delta.magnitude;
                        var firstRadius = Mathf.Max(first.Rect.sizeDelta.x, first.Rect.sizeDelta.y) * first.Rect.localScale.x * 0.5f;
                        var secondRadius = Mathf.Max(second.Rect.sizeDelta.x, second.Rect.sizeDelta.y) * second.Rect.localScale.x * 0.5f;
                        var minimumDistance = firstRadius + secondRadius + 2f;
                        if (distance >= minimumDistance)
                            continue;

                        var direction = distance > 0.01f
                            ? delta / distance
                            : new Vector2((i + j) % 2 == 0 ? 1f : -1f, i % 2 == 0 ? 1f : -1f).normalized;
                        var push = (minimumDistance - distance) * 0.5f;
                        _tokenOffsets[i] -= direction * push;
                        _tokenOffsets[j] += direction * push;
                    }
                }
            }

            for (var i = 0; i < 22; i++)
            {
                var token = TokenAt(i);
                if (token == null || token.Rect == null || !token.Rect.gameObject.activeInHierarchy)
                    continue;
                var targetOffset = Vector2.ClampMagnitude(_tokenOffsets[i], 28f);
                if (_snap)
                    _tokenRenderOffsets[i] = targetOffset;
                else
                    _tokenRenderOffsets[i] = Vector2.Lerp(_tokenRenderOffsets[i], targetOffset,
                        1f - Mathf.Exp(-12f * Time.unscaledDeltaTime));
                token.Rect.anchoredPosition += _tokenRenderOffsets[i];
            }
        }

        bool Jumping(bool home, int index)
        {
            if (_engine == null || _engine.ActionKind != "cross" || index <= 0)
                return false;
            if (home != _match.HomeHasBall)
                return false;
            if (index == _match.BallHolder)
                return true;
            var spot = _engine.MotionOf(home, index);
            var depth = home ? spot.y : 1f - spot.y;
            return depth > 0.7f && Mathf.Abs(spot.x - 0.5f) < 0.22f;
        }

        void Move(Token token, Vector2 anchor, bool hasBall, bool jumping)
        {
            var current = token.Rect.anchorMin;
            var rate = _engine != null && _engine.DeadBall ? 5f : 6f;
            if (_engine != null)
                rate *= Mathf.Clamp(_engine.MovementSpeedScale, 0.5f, 4f);
            var next = _snap ? anchor : Vector2.Lerp(current, anchor, 1f - Mathf.Exp(-Time.deltaTime * rate));
            token.Rect.anchorMin = next;
            token.Rect.anchorMax = next;
            var lift = _engine == null ? 0f : Mathf.Clamp01(_engine.BallHeight);
            var swell = jumping ? lift * lift : 0f;
            token.Rect.anchoredPosition = new Vector2(0f, swell * 22f);
            var scale = hasBall ? 1.28f : 1f;
            token.Rect.localScale = Vector3.one * (scale * (1f + swell * 0.5f));
        }

        static void Dive(Token token, float dive, bool keeper)
        {
            if (!keeper || Mathf.Abs(dive) < 0.08f)
            {
                token.Rect.localRotation = Quaternion.identity;
                return;
            }

            token.Rect.localRotation = Quaternion.Euler(0f, 0f, dive * -18f);
        }

        void RefreshSides()
        {
            Fill(_homeCol, _homeXi ? _match.HomeOnField : _match.HomeBench);
            Fill(_awayCol, _awayXi ? _match.AwayOnField : _match.AwayBench);
            PaintTab(_homeCol.Xi, _homeXi);
            PaintTab(_homeCol.Bench, !_homeXi);
            PaintTab(_awayCol.Xi, _awayXi);
            PaintTab(_awayCol.Bench, !_awayXi);
        }

        void Fill(SideColumn col, List<string> ids)
        {
            for (var i = 0; i < 11; i++)
            {
                var on = ids != null && i < ids.Count;
                col.Rows[i].SetActive(on);
                if (!on)
                    continue;
                var player = PlayerOf(ids[i]);
                var number = player == null ? (i + 1).ToString() : player.SquadNumber.ToString();
                var name = player == null ? NameAt(ids, i) : BroadcastUi.ShortName(player.FullName);
                if (player != null && player.IsCaptain)
                    name += "  C";
                col.Numbers[i].text = number;
                col.Names[i].text = name;
                var injured = player != null && player.Injury != null && player.Injury.IsInjured;
                col.Marks[i].text = injured ? "+" : "";
                col.Marks[i].color = new Color(0.95f, 0.12f, 0.16f);
                col.Marks[i].gameObject.SetActive(injured);
                var booking = 0;
                if (player != null)
                    _match.Bookings.TryGetValue(player.Id, out booking);
                col.Cards[i].gameObject.SetActive(booking > 0);
                col.Cards[i].color = booking == 2 ? new Color(0.85f, 0.12f, 0.14f) : new Color(0.95f, 0.82f, 0.12f);
                var id = player == null ? null : player.Id;
                var goals = Stat(_match.GoalsScored, id);
                col.Balls[i].gameObject.SetActive(goals > 0);
                if (col.GoalMarks[i] != null)
                    col.GoalMarks[i].text = goals > 1 ? goals.ToString() : "";
                col.Boots[i].gameObject.SetActive(Stat(_match.Assists, id) > 0);
                col.Pens[i].text = Stat(_match.PenaltyGoals, id) > 0 ? "P" : "";
                var ratings = _tab == BoardTab.Ratings;
                col.Fills[i].transform.parent.gameObject.SetActive(!ratings);
                col.Scores[i].gameObject.SetActive(ratings);
                if (ratings)
                    col.Scores[i].text = _match.RatingOf(ids[i]).ToString("0.0");
                else
                {
                    var tired = player == null ? 30f : player.Fatigue;
                    BroadcastUi.SetMeter(col.Fills[i], 1f - tired / 100f);
                }
            }
        }

        void RefreshRatings()
        {
            if (_tab != BoardTab.Ratings)
                return;
            _homeRatings.text = RatingBlock(_match.HomeOnField, _match.HomeBench);
            _awayRatings.text = RatingBlock(_match.AwayOnField, _match.AwayBench);
        }

        string RatingBlock(List<string> field, List<string> bench)
        {
            var lines = new List<string>();
            AppendRatings(lines, field);
            lines.Add("");
            lines.Add("Banco");
            AppendRatings(lines, bench);
            return string.Join("\n", lines);
        }

        void AppendRatings(List<string> lines, List<string> ids)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                var player = PlayerOf(ids[i]);
                var number = player == null ? (i + 1).ToString() : player.SquadNumber.ToString();
                var name = player == null ? NameAt(ids, i) : BroadcastUi.ShortName(player.FullName);
                lines.Add(number.PadLeft(2) + "   " + name.PadRight(16) + "   " + _match.RatingOf(ids[i]).ToString("0.0"));
            }
        }

        void RefreshStats()
        {
            var total = _match.HomePossession + _match.AwayPossession;
            var home = total <= 0.01f ? 50 : Mathf.RoundToInt(_match.HomePossession / total * 100f);
            _homePoss.text = home.ToString();
            _awayPoss.text = (100 - home).ToString();
            _homeShots.text = _match.HomeShots.ToString();
            _awayShots.text = _match.AwayShots.ToString();
            _homeChances.text = _match.HomeChances.ToString();
            _awayChances.text = _match.AwayChances.ToString();
            if (_analysis != null)
            {
                _analysis.SetMatch(_match);
                _analysis.Refresh();
            }
            if (_noteCount == _match.Notes.Count && _timeline.childCount > 0)
                return;
            _noteCount = _match.Notes.Count;
            for (var i = _timeline.childCount - 1; i >= 0; i--)
                Destroy(_timeline.GetChild(i).gameObject);
            var first = NotesBefore(45);
            if (first.Count == 0)
                TimelineLine("SEM EVENTOS", true);
            else
                foreach (var note in first)
                    TimelineLine(note.Minute + "'    " + note.Text, note.Kind == "goal");
            if (_match.Phase != MatchPhase.FirstHalf)
            {
                TimelineLine("45:00", false);
                var second = NotesFrom(45);
                if (second.Count == 0)
                    TimelineLine("SEM EVENTOS", true);
                else
                    foreach (var note in second)
                        TimelineLine(note.Minute + "'    " + note.Text, note.Kind == "goal");
            }
        }

        List<MatchNote> NotesBefore(int minute)
        {
            var list = new List<MatchNote>();
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "half" || note.Kind == "full")
                    continue;
                if (note.Minute < minute)
                    list.Add(note);
            }

            return list;
        }

        List<MatchNote> NotesFrom(int minute)
        {
            var list = new List<MatchNote>();
            foreach (var note in _match.Notes)
            {
                if (note.Kind == "half" || note.Kind == "full")
                    continue;
                if (note.Minute >= minute)
                    list.Add(note);
            }

            return list;
        }

        void TimelineLine(string value, bool strong)
        {
            var text = BroadcastUi.Label(_timeline, "Line", value, strong ? 20 : 16, strong ? Color.white : UiTheme.Muted, TextAnchor.MiddleCenter, strong ? FontStyle.Bold : FontStyle.Normal);
            BroadcastUi.Preferred(text, strong ? 36 : 28);
        }

        bool UserHome()
        {
            return _career != null && _match != null && _match.Home != null && _match.Home.Id == _career.Coach.ClubId;
        }

        TacticalIdentity UserTactics()
        {
            return UserHome() ? _match.HomeTactics : _match.AwayTactics;
        }

        void RefreshPlan()
        {
            var side = UserHome();
            var english = LocalizationManager.Current == GameLanguage.English;
            var off = NameAt(side ? _match.HomeOnField : _match.AwayOnField, _subOff);
            var bench = side ? _match.HomeBench : _match.AwayBench;
            var on = bench.Count == 0 ? "—" : NameAt(bench, _subOn % bench.Count);
            var tactics = UserTactics();
            var body = english
                ? "Plan  " + LocalizationManager.Translate(TacticPresets.Active(tactics)) + "\n" +
                    "Formation  " + tactics.Formation + "\n\n" +
                    LocalizationManager.Translate(tactics.Pressing) + "\n" +
                    LocalizationManager.Translate(tactics.BuildUp) + "\n" +
                    LocalizationManager.Translate(tactics.DefensiveLine) + "\n\n" +
                    "Substitutions remaining: " + (side ? _match.HomeSubsLeft : _match.AwaySubsLeft) + "\n" +
                    "Next change: " + off + " off, " + on + " on\n\n" +
                    "Opponent  " + (side ? _match.Away.ShortName : _match.Home.ShortName) + "  ·  " + (side ? _match.AwayTactics.Formation : _match.HomeTactics.Formation)
                : "Plano  " + TacticPresets.Active(tactics) + "\n" +
                    "Formação  " + tactics.Formation + "\n\n" +
                    tactics.Pressing + "\n" +
                    tactics.BuildUp + "\n" +
                    tactics.DefensiveLine + "\n\n" +
                    "Substituições restantes: " + (side ? _match.HomeSubsLeft : _match.AwaySubsLeft) + "\n" +
                    "Próxima troca: sai " + off + ", entra " + on + "\n\n" +
                    "Adversário  " + (side ? _match.Away.ShortName : _match.Home.ShortName) + "  ·  " + (side ? _match.AwayTactics.Formation : _match.HomeTactics.Formation);
            if (_pauseBody != null)
                _pauseBody.text = body;
        }

        static void PaintTab(Button button, bool on)
        {
            var label = button.GetComponentInChildren<Text>();
            label.color = on ? Color.white : UiTheme.Muted;
            label.fontStyle = on ? FontStyle.Bold : FontStyle.Normal;
        }

        bool Injured(string id)
        {
            return !string.IsNullOrEmpty(id) && _match.People.TryGetValue(id, out var player)
                && player != null && player.Injury != null && player.Injury.IsInjured;
        }

        static int Stat(Dictionary<string, int> map, string id)
        {
            if (map == null || string.IsNullOrEmpty(id))
                return 0;
            return map.TryGetValue(id, out var value) ? value : 0;
        }

        static Sprite _boot;
        static Sprite BootSprite()
        {
            if (_boot != null)
                return _boot;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            var clear = new Color(0, 0, 0, 0);
            var ink = Color.white;
            for (var y = 0; y < 16; y++)
            {
                for (var x = 0; x < 16; x++)
                {
                    var sole = y < 4 && x > 1 && x < 15;
                    var upper = y >= 4 && y < 11 && x > 3 && x < 12;
                    var ankle = y >= 8 && y < 14 && x > 5 && x < 11;
                    tex.SetPixel(x, y, sole || upper || ankle ? ink : clear);
                }
            }
            tex.Apply();
            _boot = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
            return _boot;
        }

        Player PlayerOf(string id)
        {
            if (_career == null || string.IsNullOrEmpty(id))
                return null;
            return _career.World.Players.TryGetValue(id, out var player) ? player : null;
        }

        void RecoverBench(List<string> ids)
        {
            if (ids == null)
                return;
            foreach (var id in ids)
            {
                if (!_match.People.TryGetValue(id, out var player))
                    continue;
                player.Fatigue = Mathf.Max(5, player.Fatigue - 14);
            }
        }

        string Shirt(List<string> ids, int index)
        {
            var player = PlayerOf(ids[index]);
            return player == null ? (index + 1).ToString() : player.SquadNumber.ToString();
        }

        string NameAt(List<string> ids, int index)
        {
            if (ids == null || ids.Count == 0)
                return "—";
            index = Mathf.Clamp(index, 0, ids.Count - 1);
            return _match.Names.TryGetValue(ids[index], out var name) ? BroadcastUi.ShortName(name) : "Jogador";
        }

        static string Clock(float minute)
        {
            if (minute >= 90f)
            {
                var extra = Mathf.FloorToInt(minute - 90f);
                return extra > 0 ? "90:00  +" + extra : "90:00";
            }

            var whole = Mathf.FloorToInt(minute);
            var seconds = Mathf.FloorToInt((minute - whole) * 60f);
            return whole + ":" + seconds.ToString("00");
        }

        sealed class HalfIdea
        {
            public string Speech;
            public string Done;
            public int Preset = -1;
            public string Duty;
            public string PlayerId;
        }

        sealed class Token
        {
            public RectTransform Rect;
            public Text Number;
            public Text Injury;
        }

        sealed class SideColumn
        {
            public GameObject Root;
            public Text[] Numbers;
            public Text[] Names;
            public Text[] Scores;
            public Image[] Cards;
            public Image[] Balls;
            public Text[] GoalMarks;
            public Image[] Boots;
            public Text[] Pens;
            public Text[] Marks;
            public Image[] Fills;
            public GameObject[] Rows;
            public Button Xi;
            public Button Bench;
        }
    }
}
